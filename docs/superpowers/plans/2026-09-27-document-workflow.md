# DocumentWorkflow Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Move deterministic preview/export document preparation from `MainWindow` into a WPF-layer `DocumentWorkflow` while preserving existing behavior.

**Architecture:** Add an immutable-result-style workflow API in `DogruTemin.Wpf.Services` that coordinates existing readiness, validation, calculation, and document-generation services. Keep selection, confirmation dialogs, tie-winner UI state, PDF destination, preview window, printing, and localized messages in `MainWindow`.

**Tech Stack:** .NET 8, WPF `FixedDocument`, xUnit, existing `DocumentReadinessService`, `ValidationService`, `ProcurementEngine`, and `OfficialDocumentService`.

**Spec:** `docs/superpowers/specs/2026-09-27-document-workflow-design.md`

## Global Constraints

- Keep `OfficialDocumentService`, its `FixedDocument` output, and all document layouts unchanged.
- Keep `ProcurementEngine` calculation and tie-break semantics unchanged.
- Keep document type selection and supported-type checks in the UI for this first increment.
- Do not change PDF path defaults, output file naming, print behavior, or home-screen readiness presentation.
- Do not fold archive, backup/recovery, or ViewModel work into this change.
- Preserve validation messages and severity values.
- Preview continues to allow incomplete information; export continues to enforce existing core readiness rules.

## Review Focus

- Preview generation with incomplete fields must still produce the same document type and must not prompt for export-only warnings.
- Export with core readiness gaps must stop before confirmation, tie selection, or generation; pin with a blocked-result test.
- Export advisories and date warnings must be returned before tie selection and retain current text/order; pin with a confirmation-required test.
- A tie prompts only for market research and inspection/acceptance documents, and a valid existing preferred winner suppresses the prompt; pin both cases in tests.
- A tie must not prompt for preparation documents; pin with a preparation-document tie test.
- Unexpected document-generation exceptions must become a failed workflow result that the UI maps to the existing localized message; use an invalid enum value in a direct workflow test to exercise the existing generator exception path without adding a production test seam. The UI continues to reject unsupported selections before calling the workflow.

---

### Task 1: Add the document preparation workflow and result types

**Files:**
- Create: `DogruTemin.Wpf/Services/DocumentWorkflow.cs`
- Create: `DogruTemin.Wpf/Services/DocumentWorkflowResult.cs`
- Test: `DogruTemin.Tests/DocumentWorkflowTests.cs`

**Interfaces:**
- Consumes: `ProcurementData`, `IReadOnlyCollection<OutputDocumentType>`, existing validation/readiness/engine/document services.
- Produces:
  - `DocumentPreparationMode`: `Preview`, `Export`.
  - `DocumentPreparationStatus`: `Ready`, `Blocked`, `ConfirmationRequired`, `WinnerSelectionRequired`, `Failed`.
  - `DocumentWorkflowResult` with `Status`, nullable `FixedDocument`, `CoreIssues`, `ConfirmationMessages`, `TiedWinnerFirmIds`, nullable `TiedMarketTotal`, and nullable `Error`.
  - Static `DocumentWorkflow.Prepare(ProcurementData data, IReadOnlyCollection<OutputDocumentType> types, DocumentPreparationMode mode, bool advisoriesConfirmed = false)`; the workflow has no mutable state or injected dependencies.

- [x] **Step 1: Add all workflow behavior tests before production code**

Test names and assertions:
- `PrepareExportWithCoreReadinessIssuesReturnsBlockedWithoutDocument`: result is `Blocked`, `CoreIssues` is nonempty, and `Document` is null.
- `PreparePreviewWithIncompleteDataReturnsReadyDocument`: result is `Ready`, has one generated page for one supported type, and `CoreIssues`/`ConfirmationMessages` do not block preview.
- `PrepareExportWithAdvisoriesReturnsConfirmationRequiredBeforeTieSelection`: status is `ConfirmationRequired` and tie IDs are not exposed until `advisoriesConfirmed` is true.
- `PrepareExportWithUnresolvedMarketTieReturnsWinnerSelectionRequired`: status and tied IDs match `ProcurementEngine.Calculate`.
- `PrepareExportWithTieForPreparationDocumentDoesNotRequestWinner`: an `ApproximateRequest` result does not return `WinnerSelectionRequired`.
- `PrepareExportWithPreferredWinnerBuildsDocumentWithoutTiePrompt`: status is `Ready` and winner selection is not requested.
- `PrepareExportWithUnsupportedEnumValueReturnsFailed`: status is `Failed`, `Document` is null, and `Exception` is present; production UI still filters unsupported types.
- In each result-path test, assert the input `ProcurementData.PreferredWinnerFirmId` remains unchanged.

- [x] **Step 2: Run the focused test to confirm the workflow API is missing**

Run: `dotnet test .\DogruTemin.Tests\DogruTemin.Tests.csproj -p:NuGetAudit=false --filter FullyQualifiedName~DocumentWorkflowTests`
Expected: FAIL to compile because `DocumentWorkflow` and result types do not exist.

- [x] **Step 3: Implement result types and `DocumentWorkflow.Prepare`**

For `Preview`, call `OfficialDocumentService.BuildDocument` directly. For `Export`, evaluate `DocumentReadinessService.GetExportCoreIssues` first; return `Blocked` when any exist. Then collect distinct nonblank messages from `ValidationService.ValidateDocument` for each selected type plus `ValidationService.DateLogicWarnings`. Return `ConfirmationRequired` when those messages exist and `advisoriesConfirmed` is false. Next, calculate with `ProcurementEngine`; for `MarketResearchReport` or `InspectionAcceptanceReport`, return `WinnerSelectionRequired` with tied IDs if there is a tie and no chosen winner. Otherwise build and return the document. Catch generation exceptions and return `Failed` with the exception. Do not mutate `data`.

- [x] **Step 4: Run all focused workflow tests after implementation**

Run: `dotnet test .\DogruTemin.Tests\DogruTemin.Tests.csproj -p:NuGetAudit=false --filter FullyQualifiedName~DocumentWorkflowTests`
Expected: PASS all `DocumentWorkflowTests` on Windows; WPF document generation tests use an STA thread pattern consistent with `ApprovalDocumentLayoutTests`.

- [x] **Step 5: Commit the workflow unit**

```powershell
git add DogruTemin.Wpf/Services/DocumentWorkflow.cs DogruTemin.Wpf/Services/DocumentWorkflowResult.cs DogruTemin.Tests/DocumentWorkflowTests.cs
git commit -m "feat: add document preparation workflow"
```

### Task 2: Route preview and export handlers through the workflow

**Files:**
- Modify: `DogruTemin.Wpf/Services/DocumentWorkflowResult.cs`
- Modify: `DogruTemin.Wpf/Services/DocumentWorkflow.cs`
- Modify: `DogruTemin.Tests/DocumentWorkflowTests.cs`
- Modify: `DogruTemin.Wpf/MainWindow.Documents.cs`

**Interfaces:**
- Consumes: `DocumentWorkflow`, `DocumentWorkflowResult`, and `DocumentPreparationMode` from Task 1.
- Produces: `MainWindow` calls the stateless `DocumentWorkflow.Prepare` API; its preview/export handlers retain existing user-facing behavior while delegating deterministic preparation.

- [x] **Step 1: Carry the tie amount through the workflow result**

Add nullable `TiedMarketTotal` to `DocumentWorkflowResult`. When returning `WinnerSelectionRequired`, set it to `CalculationResult.MarketResultTotal`. Extend `PrepareExportWithUnresolvedMarketTieReturnsWinnerSelectionRequired` to assert the result total equals the calculated minimum and watch the test fail before implementation.

- [x] **Step 2: Replace preview preparation with a workflow call**

Change `BuildSelectedOfficialDocumentForPreview` to keep `GetSelectedImplementedDocumentTypes` and `BuildDataFromUi` in the UI, call `Prepare(..., Preview)`, return its document on `Ready`, and route `Failed` through the existing preview error text.

- [x] **Step 3: Replace export preparation with workflow result handling**

Change `BuildSelectedOfficialDocumentForExport` to call `Prepare(..., Export)`. Preserve the current order: core-block message; advisory/date confirmation; tie-winner chooser using `TiedWinnerFirmIds` and `TiedMarketTotal`; rebuild the UI snapshot after a winner is selected, call `Prepare(..., Export, advisoriesConfirmed: true)` again; return the ready document. Map `Failed` to the existing export/print localized catch messages. Keep PDF dialog/path, PDF writing, preview window, print dialog, notifications, and exception display in their current handlers.

- [x] **Step 4: Check the patch for behavior and formatting changes**

Run: `git diff --check`
Expected: no whitespace errors. Review the diff to confirm no strings, PDF defaults, or print behavior changed.

- [x] **Step 5: Commit the UI integration**

```powershell
git add DogruTemin.Wpf/Services/DocumentWorkflowResult.cs DogruTemin.Wpf/Services/DocumentWorkflow.cs DogruTemin.Tests/DocumentWorkflowTests.cs DogruTemin.Wpf/MainWindow.Documents.cs
git commit -m "refactor: use DocumentWorkflow for preview and export"
```

### Task 3: Verify the integrated change

**Files:**
- Verify: `DogruTemin.Wpf/MainWindow.Documents.cs`
- Verify: `DogruTemin.Wpf/Services/DocumentWorkflow.cs`
- Verify: `DogruTemin.Tests/DocumentWorkflowTests.cs`, existing document layout and PDF tests

**Interfaces:**
- Consumes: the completed workflow and UI integration from Tasks 1–2.
- Produces: a clean local verification result ready for the existing Windows CI / PR flow.

- [x] **Step 1: Run the full test project**

Run: `dotnet test .\DogruTemin.Tests\DogruTemin.Tests.csproj -p:NuGetAudit=false`
Expected: all tests pass, including workflow, layout, PDF, and existing regression tests.

- [x] **Step 2: Review status and whitespace**

Run: `git diff --check` and `git status --short --branch`
Expected: no whitespace errors and only the intended committed change set.

- [x] **Step 3: Prepare the PR summary after Windows CI**

Summarize the workflow boundary and preserved behavior. Do not merge until all required checks are green and the user approves the merge.


## Completion

DocumentWorkflow changes are present in local main; PR #18 was merged. Local Windows verification passed with 80 tests in the integrated codebase.
