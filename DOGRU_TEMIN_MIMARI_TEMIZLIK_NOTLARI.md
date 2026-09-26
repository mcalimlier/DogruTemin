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
