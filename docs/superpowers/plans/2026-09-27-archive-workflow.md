# ArchiveWorkflow Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Aktif arşiv kaydını koruyarak arşiv kaydı silme ve son silineni geri alma akışlarını `ArchiveWorkflow` üzerinden yürütmek.

**Architecture:** `DogruTemin.Core.Services.ArchiveWorkflow`, `StorageService` kullanarak silme ve geri alma kullanım senaryolarını orkestre eder ve WPF'den bağımsız sonuç durumu döndürür. WPF; seçim, ön kontrol sonucunun sunumu, onay penceresi, liste yenileme ve geri yüklenen kaydı seçme sorumluluklarını korur. `StorageService` ve dosya davranışı değişmez.

**Tech Stack:** .NET 8, C#, WPF, xUnit.

**Spec:** `docs/superpowers/specs/2026-09-27-archive-workflow-design.md`

## Global Constraints

- Aktif kayıt silinemez.
- Silme onayı ve mesaj metni korunur.
- Silinen kayıt, mevcut güvenli kopyalama ve 30 kayıtlık saklama davranışını sürdürür.
- Geri yükleme en son silinen kaydı seçer; RecordId çakışmasında StorageService'in yeni kimlik üretmesi değişmez.
- Filtre/sıralama ve arşiv ekranı projeksiyonu değişmez.
- StorageService'in mevcut hata sözleşmesi ve kullanıcıya gösterilen mevcut hata metinleri korunur.
- Workflow WPF türlerine, `MessageBox`'a veya görsel duruma bağımlı olmaz.
- `StorageService` bu ilk dilimde değiştirilmez.

## Review Focus

- Aktif RecordId doğrudan `Delete` çağrısına verilirse sonuç engellenmeli ve arşiv dosyası kalmalı. (Task 1 testi.)
- Aktif RecordId boş veya farklıysa geçerli kayıt silinebilmeli; silinen kopya kurtarılabilir olmalı. (Task 1 testleri.)
- Geri alınacak kayıt yoksa null veri ve mevcut StorageService uyarısı başarısız sonuçta korunmalı. (Task 1 testi.)
- Geri yükleme için seçilen silinen dosya bozuksa hata sonucu dönmeli ve bozuk dosya yerinde kalmalı. (Task 1 testi.)
- Birden fazla silinen kayıt varsa en son silinen seçilmeli; RecordId arşivde yeniden kullanılmışsa geri yüklenen kayıt yeni RecordId almalı. (Task 1 testleri.)

---

### Task 1: ArchiveWorkflow ve sonuç sözleşmesi

**Files:**
- Create: `DogruTemin.Tests/ArchiveWorkflowTests.cs`
- Create: `DogruTemin.Core/Services/ArchiveWorkflow.cs`

**Interfaces:**
- Consumes: `StorageService(string rootFolder)`, `DeleteRecord(string)`, `RestoreMostRecentlyDeleted(out string?)`.
- Produces: `ArchiveWorkflow(StorageService storage)`; static `CanDelete(string recordId, string? activeRecordId)`, instance `Delete(string recordId, string? activeRecordId)` and `RestoreMostRecentlyDeleted()`; `ArchiveWorkflowStatus` values `Ready`, `Completed`, `Blocked`, `Failed`; `ArchiveWorkflowResult` properties `Status`, `RestoredRecord`, `Warning`.
- `CanDelete` is side-effect free. It returns `Blocked` only when the selected ID equals the non-empty active ID; otherwise `Ready`.
- `Delete` repeats the active-record guard, delegates to `StorageService.DeleteRecord`, and maps its bool to `Completed` or `Failed`.
- `RestoreMostRecentlyDeleted` maps a returned record to `Completed` and null to `Failed`, preserving the storage warning.

- [ ] **Step 1: Add failing workflow tests**

Create a disposable test fixture with a unique temporary root and a real `StorageService`. Add these tests:

```csharp
ConstructorRejectsNullStorage
CanDeleteBlocksActiveRecordWithoutChangingArchiveFile
CanDeleteAllowsRecordWhenNoActiveRecordId
CanDeleteAllowsInactiveRecord
DeleteBlocksActiveRecordEvenWhenCalledDirectly
DeleteNonActiveRecordCreatesRecoverableDeletedCopy
RestoreMostRecentlyDeletedWhenEmptyReturnsFailureAndStorageWarning
RestoreMostRecentlyDeletedWhenMultipleRecordsRestoresNewest
RestoreMostRecentlyDeletedWhenRecordIdWasReusedCreatesNewId
RestoreMostRecentlyDeletedWhenLatestFileIsCorruptReturnsFailureAndKeepsFile
```

Assert statuses and persisted state, not implementation call counts. For corrupt/latest-file cases, set file timestamps explicitly so selection is deterministic.

- [ ] **Step 2: Run the new tests and confirm the expected failure**

Run: `dotnet test .\DogruTemin.Tests\DogruTemin.Tests.csproj -p:NuGetAudit=false --filter FullyQualifiedName~ArchiveWorkflowTests`

Expected: compile/test failure because `ArchiveWorkflow`, `ArchiveWorkflowStatus`, and `ArchiveWorkflowResult` do not yet exist.

- [ ] **Step 3: Implement the workflow contract**

In `DogruTemin.Core/Services/ArchiveWorkflow.cs`, add a sealed workflow with a null-checked `StorageService` constructor and the three methods from the Interfaces block. Keep all WPF messages out of Core. Use small factory methods on `ArchiveWorkflowResult` for `Ready`, `Completed`, `Blocked`, and `Failed`; include `RestoredRecord` only for successful restore and `Warning` when StorageService returns one.

- [ ] **Step 4: Run focused tests and then the full suite**

Run: `dotnet test .\DogruTemin.Tests\DogruTemin.Tests.csproj -p:NuGetAudit=false --filter FullyQualifiedName~ArchiveWorkflowTests`

Expected: all ten ArchiveWorkflow tests pass.

Run: `dotnet test .\DogruTemin.Tests\DogruTemin.Tests.csproj -p:NuGetAudit=false`

Expected: all tests pass; no existing test is removed or weakened.

- [ ] **Step 5: Commit the Core workflow**

```powershell
git add DogruTemin.Tests/ArchiveWorkflowTests.cs DogruTemin.Core/Services/ArchiveWorkflow.cs
git commit -m "feat: add archive delete and restore workflow"
```

### Task 2: WPF silme ve geri alma akışlarını bağla

**Files:**
- Modify: `DogruTemin.Wpf/MainWindow.Core.cs`
- Modify: `DogruTemin.Wpf/MainWindow.Startup.cs`
- Modify: `DogruTemin.Wpf/MainWindow.Archive.cs`

**Interfaces:**
- Consumes: Task 1 `ArchiveWorkflow`, `ArchiveWorkflowStatus`, and `ArchiveWorkflowResult`.
- Produces: MainWindow-owned workflow initialized with the existing `_storage`; delete and restore event handlers use the workflow result while retaining current user-facing behavior.

- [ ] **Step 1: Add and initialize the workflow field**

Declare a readonly `ArchiveWorkflow` field beside `_procurementWorkflow` in `MainWindow.Core.cs`; initialize it from the existing `_storage` in `MainWindow.Startup.cs`. Do not create another `StorageService`.

- [ ] **Step 2: Route archive deletion through the workflow**

In `DeleteArchive_Click`, preserve the no-selection message. Replace the duplicated active-record comparison with `ArchiveWorkflow.CanDelete(row.RecordId, _recordId)` and preserve the current active-record message before showing confirmation. After confirmation, call `_archiveWorkflow.Delete(row.RecordId, _recordId)`; if it returns `Blocked`, show the same active-record message, and if it returns `Failed`, show the existing generic deletion error. Preserve success refresh behavior and confirmation text.

- [ ] **Step 3: Route deleted-record recovery through the workflow**

In `RestoreDeletedArchive_Click`, call `RestoreMostRecentlyDeleted()`. On `Failed`, show `result.Warning` or the existing fallback message. On `Completed`, use `result.RestoredRecord`, refresh the archive list, select the restored record, and preserve the current success notification.

- [ ] **Step 4: Build WPF and run all tests**

Run: `dotnet build .\DogruTemin.Wpf\DogruTemin.Wpf.csproj -p:NuGetAudit=false`

Expected: build succeeds with zero errors.

Run: `dotnet test .\DogruTemin.Tests\DogruTemin.Tests.csproj -p:NuGetAudit=false`

Expected: all tests pass.

- [ ] **Step 5: Manually check the Windows archive flow**

Open the WPF application and confirm: active record deletion is refused before the confirmation dialog; another record can be deleted after confirmation; the deleted record can be restored and selected; existing messages, list filtering, and sorting remain usable.

- [ ] **Step 6: Commit the WPF integration**

```powershell
git add DogruTemin.Wpf/MainWindow.Core.cs DogruTemin.Wpf/MainWindow.Startup.cs DogruTemin.Wpf/MainWindow.Archive.cs
git commit -m "refactor: use ArchiveWorkflow for delete and restore"
```
