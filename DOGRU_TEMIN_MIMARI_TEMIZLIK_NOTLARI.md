# DoğruTemin — Mimari Temizlik Notları

## Aşama 1 — Core model ayrıştırması

Tarih: 2026-09-26

### Yapılanlar

- `ProcurementData.cs` yalnızca ana işlem veri modelini taşıyacak şekilde sadeleştirildi.
- Firma modeli `Firm.cs` dosyasına ayrıldı.
- Ürün modeli `ProductItem.cs` dosyasına ayrıldı.
- Firma teklif modeli `FirmOffer.cs` dosyasına ayrıldı.
- Komisyon üyesi modeli `CommissionMember.cs` dosyasına ayrıldı.
- Süreç tarihleri `ProcessDates.cs` dosyasına ayrıldı.
- Kurum bilgileri `InstitutionInfo.cs` dosyasına ayrıldı.
- Onay belgesi bilgileri `ApprovalDocumentInfo.cs` dosyasına ayrıldı.
- Uygulama ayarları `ApplicationSettings.cs` dosyasına ayrıldı.
- Doğrulama enum/model yapıları `ValidationModels.cs` dosyasına ayrıldı.

### Neden?

Bu değişiklik çalışma mantığını değiştirmeden Core katmanının bakımını kolaylaştırır. Her model kendi sorumluluğunda tutulur; yeni özellik eklenirken tek bir büyük model dosyasına sürekli müdahale edilmesi önlenir.

### Değişmeyenler

- Hesap motoru kuralları değiştirilmedi.
- Veri alanlarının adları ve varsayılan değerleri değiştirilmedi.
- JSON serileştirme için kullanılan model isimleri korunmuştur.
- WPF ekran akışı değiştirilmedi.
- Depolama, yedekleme ve kurtarma mantığı değiştirilmedi.

## Sonraki aşamalar

1. MainWindow partial sınıflarındaki ortak altyapıyı azaltmak.
2. UI olaylarından workflow/business işlemlerini Application servislerine taşımak.
3. ValidationService'i sorumluluklarına göre modülerleştirmek.
4. Golden Scenario entegrasyon testini eklemek.
5. SchemaVersion için açık migration hattı oluşturmak.
6. Belge üretimini DocumentDefinition tabanlı daha ölçeklenebilir bir yapıya taşımak.
7. Ana ekranı bilgi yoğunluğunu azaltacak şekilde sadeleştirmek.

Temel prensip:

> İçeride uzman bir sistem, dışarıda basit bir program.

## Faz 3 — Belge uygunluk kurallarının Core'a taşınması

- DocumentReadinessService oluşturuldu.
- Belge üretimi için temel zorunluluklar artık MainWindow.Documents.cs içinde hesaplanmıyor.
- Ana ekran da aynı merkezi servisi kullanıyor; böylece aynı kuralın farklı ekranlarda farklı uygulanması riski azaltıldı.
- WPF tarafı belge kurallarının ayrıntısını bilmek yerine sonucu tüketiyor.
- Kullanıcı arayüzü sade kalırken iç taraftaki iş kuralları daha test edilebilir hale getirildi.

## GitHub Desktop / Fetch notu

- refactor/architecture-cleanup-phase1 uzak repoda doğrulandı.
- Branch main karşısında 14 commit ileride ve 0 commit geride.
- GitHub tarafında kayıp commit görünmüyor. GitHub Desktop'ta görünmeyen/eski kalan içerik için Fetch origin ile yerel remote referansı yenilenmeli.
- Yerel branch uzak branch ile eşleşmiyorsa yerel değişiklikler korunarak Origin/refactor/architecture-cleanup-phase1 üzerinden checkout edilmelidir.

## Phase 4 — CI doğrulaması ve bir sonraki temizlik adımı
- `refactor/architecture-cleanup-phase1` için Windows CI yeniden çalıştırıldı ve başarılı tamamlandı.
- Release derlemesi, otomatik testler ve setup smoke testleri başarılı.
- Sonraki temizlikte UI dosyalarındaki gereksiz bağımlılık/using yükünü azaltma ve doğrulama sorumluluklarını daha net sınırlara ayırma hedefleniyor.

## Phase 5 — Doğrulama sorumluluklarının ayrıştırılması
- Temel alan doğrulamaları ayrı `InputValidationService` sınıfına taşındı: vergi no, telefon ve e-posta.
- `ValidationService` iş akışı ve belge kurallarının merkezi olarak kaldı.
- Eski `ValidationService.ValidTaxNo/ValidPhone/ValidEmail` çağrıları geriye dönük uyumluluk için korundu.
- Mevcut iş kuralları ve kullanıcı akışı değiştirilmedi.

## Phase 5.1 — CI düzeltmesi
- CI kontrolünde `ValidationService` içindeki `Regex` namespace bağımlılığı eksikliği tespit edildi ve düzeltildi.
- `InputValidationService` içindeki e-posta kontrolü analyzer uyarısı vermeyecek şekilde açık `StringComparison` kullanacak biçimde temizlendi.
- İş kuralları ve kullanıcı akışı değiştirilmedi.

## Phase 6 — Tarih doğrulamasını bağımsız Core servisine ayırma
- Tarih kronolojisi kuralları `DateValidationService` içine taşındı.
- `ValidationService` artık tarih sıralama ayrıntısını kendi içinde uygulamak yerine ortak tarih servisini kullanıyor.
- `DateLogicWarnings` ve belge/iş akışı kontrollerinin mevcut dış API davranışı korunuyor.
- Kullanıcı arayüzü ve iş kuralları değiştirilmedi; amaç sorumluluk sınırını netleştirmek ve sonraki Golden Scenario testlerine daha küçük birimler sunmak.

## Phase 6.1 — CI derleme düzeltmesi
- CI kontrolünde `ValidationService.DateLogicWarnings` içinde tarih servisine dönüşten sonra kalan erişilemez `return` satırı tespit edildi.
- Erişilemeyen kod kaldırıldı; tarih doğrulama davranışı değiştirilmedi.
- Sonraki doğrulama: Release derlemesi, test/coverage ve setup smoke kontrolleri.

## Phase 7 — Belge doğrulamasını bağımsız Core servisine ayırma
- Belgeye özel doğrulama kuralları `DocumentValidationService` sınıfına taşındı.
- `ValidationService.ValidateDocument(...)` geriye dönük uyumluluk için korunarak yeni servise delegasyon yapıyor.
- Tarih sıralaması `DateValidationService` üzerinden kullanılmaya devam ediyor.
- Belge türlerine göre gerekli alanlar, komisyonlar, firma/teklif kontrolleri ve kazanan firma adresi kontrolü mevcut davranış korunarak yeni serviste toplandı.
- Amaç: `ValidationService` içindeki sorumlulukları küçük, test edilebilir Core servislerine ayırmak; WPF arayüzünü değiştirmemek.

## Phase 7.1 — CI derleme düzeltmesi
- PR #10 CI kontrolünde belge doğrulama yardımcıları ayrıştırılırken `ValidationService` içinde hâlâ kullanılan ortak yardımcıların da taşındığı tespit edildi.
- `OfferFor`, `RequireCommission`, `RequireText`, `RequireDate`, `Critical` ve `Warning` yardımcıları `ValidationService` içinde korunarak workflow doğrulamasının mevcut davranışı tamamlandı.
- Belgeye özel yardımcılar `DocumentValidationService` içinde kalmaya devam ediyor.
- Kullanıcı akışı ve doğrulama kuralları değiştirilmedi.

## Phase 8 — İş akışı doğrulamasını bağımsız Core servisine ayırma
- `ValidateWorkflow` içindeki işlem hazırlığı, ürün/firma/komisyon, tarih, teklif ve kazanan firma kontrolleri `WorkflowValidationService` sınıfına taşındı.
- `ValidationService.ValidateWorkflow(...)` geriye dönük uyumluluk korunacak şekilde yeni servise delegasyon yapıyor.
- `ValidationService` artık taslak doğrulama, tarih doğrulama, belge doğrulama ve hazırlık yüzdesi gibi daha sınırlı sorumlulukları taşıyor.
- Kullanıcı akışı ve mevcut doğrulama mesajları değiştirilmedi.
- Amaç: doğrulama servisinin büyümesini durdurmak ve Golden Scenario testinde iş akışı doğrulamasını bağımsız olarak test edilebilir hale getirmek.

## Phase 9 — Hazırlık yüzdesini bağımsız Core servisine ayırma
- `ValidationService.ReadinessPercent(...)` içindeki hazırlık kontrol listesi `ReadinessService` sınıfına taşındı.
- `ValidationService.ReadinessPercent(...)` geriye dönük uyumluluk için facade olarak korunarak yeni servise delegasyon yapıyor.
- Mevcut 19 hazırlık kontrolü, yüzde hesaplama yöntemi ve veri alanları değiştirilmedi.
- Amaç: `ValidationService` sorumluluğunu daha da daraltmak ve hazırlık durumunu bağımsız test edilebilir bir Core servisi haline getirmek.
- Kullanıcı arayüzü ve mevcut iş akışı değiştirilmedi.

## Phase 10 — Taslak doğrulamasını bağımsız Core servisine ayırma
- `ValidateDraft(...)` içindeki taslak/alan doğrulama kuralları `DraftValidationService` sınıfına taşındı.
- `ValidationService.ValidateDraft(...)` geriye dönük uyumluluk için facade olarak korunarak yeni servise delegasyon yapıyor.
- Ürün birimi/miktarı, firma sınırları, firma vergi no/telefon/e-posta, teklif fiyatları, mükerrer firma ve kurum iletişim bilgileri kontrollerinin mevcut davranışı korunmuştur.
- Doğrulama mesajları ve alan anahtarları değiştirilmedi.
- Amaç: `ValidationService` içindeki kalan gerçek iş mantığını da küçük, bağımsız ve test edilebilir Core servislerine ayırmak.
- Kullanıcı arayüzü ve workflow akışı değiştirilmedi.

## Phase 11 — ValidationService son temizlik

- Phase 10 sonrasında `ValidationService` tekrar incelendi.
- `ValidateDraft`, `ValidateWorkflow`, `DateLogicWarnings`, `ValidateDocument` ve `ReadinessPercent` artık ilgili bağımsız Core servislerine yalnızca delegasyon yapıyor.
- `ValidTaxNo`, `ValidPhone` ve `ValidEmail` geriye dönük uyumluluk facade'ı olarak korunuyor.
- Artık herhangi bir çağrı tarafından kullanılmayan özel `Critical` yardımcı metodu kaldırıldı.
- Böylece `ValidationService` içinde gerçek doğrulama iş mantığı yerine yalnızca geriye dönük uyumluluk yüzeyi kaldı.
- Kullanıcı arayüzü, iş kuralları ve mevcut doğrulama davranışı değiştirilmedi.
- Bu aşamanın amacı, sonraki Golden Scenario ve servis testlerine geçmeden önce doğrulama facade'ını gereksiz koddan arındırmaktır.


## Phase 12 — Golden Scenario entegrasyon testi

- `DogruTemin.Tests/GoldenScenarioTests.cs` eklendi.
- Tek bir gerçekçi satın alma senaryosu üzerinden ana Core akışı birlikte doğrulanıyor:
  - taslak doğrulaması,
  - workflow doğrulaması,
  - %100 hazırlık kontrolü,
  - `ProcurementEngine` kazanan firma ve toplam hesabı,
  - kayıt ve yedek oluşturma,
  - kaydı yeniden yükleme,
  - yeniden yüklenen veride hesap sonucunun korunması.
- Test, kritik doğrulama hatası olmadan tam hazırlanmış bir işlemin hesaplanıp kaydedilebildiğini ve yeniden yüklendiğinde aynı sonucu verdiğini güvence altına alıyor.
- Mevcut UI veya iş kuralları değiştirilmedi.
- Amaç: sonraki mimari refactorlarda ana satın alma akışının sessizce bozulmasını CI seviyesinde yakalamak.


## Phase 13 — SchemaVersion migration hattının oluşturulması

- `SchemaMigrationService` oluşturuldu.
- Mevcut persisted model sürümü `4` tek bir merkezi sabit üzerinden yönetiliyor.
- Eski sürümler için açık migration noktası oluşturuldu; mevcut 1-3 sürümleri model varsayılanlarıyla uyumlu şekilde 4'e yükseltiyor.
- Uygulamanın tanımadığı daha yeni bir schema sürümü sessizce açılmıyor; `InvalidDataException` ile açıkça reddediliyor.
- `StorageService.Deserialize(...)` artık kayıtları doğrudan döndürmek yerine migration katmanından geçiriyor.
- `StorageService.Normalize(...)` schema sürümünü kendi başına yükseltmek yerine migration katmanının sonucunu kullanıyor.
- Eski sürüm yükseltme, ileri sürüm reddi ve gerçek StorageService yükleme akışı için testler eklendi.
- Mevcut UI ve iş kuralları değiştirilmedi.
- Amaç: gelecekte SchemaVersion 5+ gibi değişiklikler geldiğinde tüm veri dönüşümlerinin tek, test edilebilir bir noktadan yönetilmesini sağlamak.
