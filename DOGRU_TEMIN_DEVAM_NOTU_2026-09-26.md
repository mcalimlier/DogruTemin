# DogruTemin — Yeni Sohbete Devam Notu

Tarih: 2026-09-26

## Proje
- Repo: https://github.com/mcalimlier/DogruTemin
- Stack: .NET 8 WPF
- Projeler: DogruTemin.Core, DogruTemin.Wpf, DogruTemin.Tests
- Ana hedef: “İçeride uzman bir sistem, dışarıda basit bir program.”
- UI sade kalacak; iş kuralları ve workflow mantığı Core/Application tarafında ayrıştırılacak.

## Şu an tam olarak kaldığımız yer

Phase 9 tamamlandı ve PR #12 başarıyla merge edildi.

- PR: #12 — “Refactor: hazırlık yüzdesini Core servisine ayır”
- Branch: refactor/architecture-cleanup-phase5
- CI: DogruTemin Windows CI
- Run: #282
- CI sonucu: SUCCESS / GREEN
- Merge commit (main): 9a6bfcbfecf39b4727adbf19d110e61c07142e97

### Phase 9 yapılan değişiklik
Yeni dosya:
- DogruTemin.Core/Services/ReadinessService.cs

Taşınan mantık:
- ProcurementData içindeki hazırlık yüzdesini hesaplayan 19 kontrol
- yüzde hesabı
- mevcut veri alanlarının kullanımı

ValidationService artık facade olarak:
- ReadinessService.CalculatePercent(data)

Korunan davranış:
- UI/workflow davranışı değiştirilmedi.
- Mevcut ValidationService çağrı noktası korunuyor.
- Amaç Core tarafında küçük, test edilebilir servisler oluşturmak.

## Önceki tamamlanan mimari fazlar

### Phase 1
ProcurementData.cs parçalandı:
- Firm.cs
- ProductItem.cs
- FirmOffer.cs
- CommissionMember.cs
- ProcessDates.cs
- InstitutionInfo.cs
- ApprovalDocumentInfo.cs
- ApplicationSettings.cs
- ValidationModels.cs

ProcurementData artık ana veri modeli olarak tutuluyor.

### Phase 2
MainWindow.Core.cs içindeki startup mantığı MainWindow.Startup.cs dosyasına ayrıldı.

### Phase 3
DocumentReadinessService oluşturuldu. Belge hazırlık kuralları WPF dışına taşındı.

### Phase 4
MainWindow.Core.cs usings/temizlik düzenlendi.

### Phase 5
InputValidationService oluşturuldu:
- ValidTaxNo
- ValidPhone
- ValidEmail

ValidationService geriye dönük facade olarak kaldı.

### Phase 6
DateValidationService oluşturuldu. Tarih sıralaması/uyarı mantığı buraya taşındı.

### Phase 7
DocumentValidationService oluşturuldu. Belgeye özel validation mantığı buraya taşındı.

### Phase 8
WorkflowValidationService oluşturuldu. Eski ValidateWorkflow mantığı buraya taşındı.
ValidationService: ValidateWorkflow(data) => WorkflowValidationService.Validate(data)

## Mevcut ValidationService yapısı

ValidationService içinde hâlâ facade/backward-compatible noktalar bulunuyor:
- ValidateDraft
- ValidateWorkflow -> WorkflowValidationService
- DateLogicWarnings -> DateValidationService
- ValidateDocument -> DocumentValidationService
- ReadinessPercent -> ReadinessService
- ValidTaxNo / ValidPhone / ValidEmail -> InputValidationService

ValidationService'i bir anda silmek yerine kalan gerçek iş mantığı tespit edilip uygun servislere ayrılacak.

## Mimari hedef

WPF UI
  View
  ViewModel / Presentation State
  Commands
      ↓
Application Layer
  ProcurementWorkflow
  DocumentWorkflow
  ArchiveWorkflow
  ValidationWorkflow
      ↓
Core
  Models
  ProcurementEngine
  Validation
  Rules
      ↓
Infrastructure
  Storage
  PDF
  Backup
  Recovery

Ana prensipler:
- UI sadece sunum ve kullanıcı etkileşimi.
- Workflow kararları servislerde.
- Core iş kurallarının merkezi.
- Infrastructure dosya/PDF/depolama gibi dış dünya işlemlerini üstlenir.
- Mevcut davranış gereksiz yere değiştirilmeden parça parça refactor yapılacak.
- Her faz sonunda CI yeşil olmadan merge yapılmayacak.
- Kullanıcı GitHub Desktop üzerinde main branch'te kalmak istiyor.

## Bir sonraki adım

Yeni sohbette kullanıcı “devam” dediğinde buradan devam et.

1. Main'in güncel durumda olduğunu doğrula.
2. ValidationService.cs içindeki kalan gerçek mantığı incele.
3. Sonraki küçük ve güvenli refactor adayını belirle.
4. Yeni bir refactor/architecture-cleanup-* branch oluştur.
5. Değişikliği uygula.
6. Gerekirse test ekle/güncelle.
7. DOGRU_TEMIN_MIMARI_TEMIZLIK_NOTLARI.md dosyasını güncelle.
8. CI yeşil olunca PR'ı squash merge et.
9. Kullanıcıya GitHub Desktop'ta Pull origin yapmasını söyle.
10. Her önemli aşamada bu devam notunu güncel tut.

## Önemli merge bilgileri

Phase 8 / PR #11 merge:
- 2df855c9c321959ae09643e864afc029c5a87286

Phase 9 / PR #12 head:
- f16e54800ed9d2874dd0653a10a1b67043617844

Phase 9 merge:
- 9a6bfcbfecf39b4727adbf19d110e61c07142e97

## Kullanıcı çalışma tercihi

- “uygula” dediğinde değişikliği doğrudan repo üzerinde yapmak.
- GitHub Desktop kullanımını mümkün olduğunca korumak.
- CI kontrolünü gerektiğinde buradan yapmak.
- Yapılan mimari değişikliklerin Markdown notunu projede bırakmak.
- Yeni sohbette kaldığımız yeri tekrar uzun uzun anlatmak zorunda kalmamak.

## Son durum

GREEN CI → PR #12 MERGED → main güncellendi.

Yeni sohbet başlangıç noktası:
PHASE 9 TAMAMLANDI. SIRADAKİ İŞ PHASE 10 MİMARİ REFACTOR.


## Phase 10 tamamlandı
- PR #13: Taslak doğrulamasını Core servisine ayırma
- CI Run #289: SUCCESS / GREEN
- PR #13 squash merge edildi.
- Main merge commit: 96d585a72029aca622ecdd1b55715dce57d6c0a8
- Yeni servis: DogruTemin.Core/Services/DraftValidationService.cs
- ValidationService.ValidateDraft(...) facade olarak korunuyor.
- Mevcut taslak doğrulama kuralları ve mesajları korunmuştur.
- Bir sonraki başlangıç noktası: Phase 11; ValidationService içinde kalan gerçek mantığın incelenmesi ve en küçük güvenli sorumluluk ayrıştırmasının uygulanması.


## Phase 11 tamamlandı
- PR #14: `ValidationService` son temizliği
- CI Run #296: SUCCESS / GREEN
- PR #14 squash merge edildi.
- Main merge commit: `200c445600e69fdc540549472766ca99fbebf730`
- `ValidationService` artık gerçek doğrulama iş mantığı taşımayan backward-compatible facade durumunda.

## Phase 12 başladı
- Branch: `refactor/architecture-cleanup-phase8`
- Golden Scenario testi eklendi: `DogruTemin.Tests/GoldenScenarioTests.cs`
- Test; doğrulama → hazırlık → hesaplama → kaydetme/yedek → yeniden yükleme → tekrar hesaplama zincirini tek senaryoda güvence altına alıyor.
- İlk hedef: CI ile testin mevcut modeller ve iş kurallarıyla sorunsuz çalıştığını doğrulamak.
- CI yeşil olduktan sonra PR squash merge edilecek.
- Sonraki başlangıç noktası: Phase 13; schema migration hattını veya Application workflow ayrıştırmasını mevcut kod incelenerek en küçük güvenli adımla ele almak.
