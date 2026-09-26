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
