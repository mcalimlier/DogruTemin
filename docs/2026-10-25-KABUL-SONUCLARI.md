# DoğruTemin geliştirme kabulü — 25 Ekim 2026

Dal: `codex/dogrutemin-integrity-and-archive`.

## Sonuç

- Son Release test koşumu: **122 başarılı / 122 toplam; 0 başarısız, 0 atlanan**.
- Release derlemesi başarılı. Ayrı analizörlü derlemede 0 hata ve 11 engelleyici olmayan analizör önerisi görüldü; bunların bir bölümü önceden bulunan CLS/kültür önerileri, kalanları test sabit dizilerine ilişkin ayırma önerileridir.
- NuGet geçişli bağımlılık taraması: **bilinen açık yok**. PDFsharp'ın getirdiği PKCS 8.0.1 yerine düzeltilmiş 8.0.2 kullanılıyor.
- 5.000 tam hesaplama için mevcut **8 saniye** performans sınırı değiştirilmeden test geçti. Tam sayı miktarları, gereksiz regex işlemi yapılmadan ayrıştırılıyor.
- `git diff --check` temiz.

## Kullanıcı maddelerinin karşılığı

| Madde | Uygulanan ve doğrulanan davranış |
|---|---|
| 1 | İki ondalık sınırı; anlamlı üçüncü basamak reddedilir. 19,53 / 19,5300 aynı kayıt kimliğini üretir. |
| 2 | PDF metni sessizce kesilmez. Hücre/sayfa sınırı ve yazdırılacak WPF içeriğinin sığması kontrol edilir. Hatalı üretim önceki dosyayı değiştirmez. |
| 3 | Mevcut kaydın üzerine yazma açık onaya bağlıdır. Arşivde tek güncel satır kalır. Eski veri biçimi için tek seferlik geçiş yedeği korunur. |
| 4 | Kayıtlı girdiden PDF ve kaynak paketi; içerik/sürüm/SHA-256 bağlantısı; Excel benzeri arşiv, tutar, belge durumu ve satırda Düzenle/Klasör/Sil. |
| 5 | 10 ürün ve 5 firma; 11 ürün / 6 firma yükleme-kaydetmede kesilmeden reddedilir. Özgün dosya korunur, arşiv uyarısı görünür. |
| 6 | Teklifi Kaydet kalıcı işlem kaydını da tamamlar. İptal/başarısızlık doğru durum mesajı üretir. |
| 7 | Hesap/kayıt/PDF sınırları, onay, geçiş, dosya değiştirme, eski veriyle çıktı, tüm ürünlerin nihai PDF'de bulunması, Türkçe alanlar ve ekran akışı regresyonları. |
| 8 | 1024×600, 1280×720, 1366×768 ve 1920×1080 pencereler; satır düğmelerinin ve yazılarının sığması. Ayrıca %125/%150/%175/%200 dahil hesaplanan ekran profilleri. |
| 9 | EDHS kart dili: lacivert/altın mali kartlar ve para, transfer, koruma, kurum vektör sembolleri; kontrast testleri. |

## Belge kabulü

Sentetik 10 ürün / 5 firma örneğinde altı belge türü üretildi. Nihai PDF'de sayfa sayısı, A4 boyutu, ürünler, toplamlar, firma, kurum, tarihler ve imzacılar PdfPig ile doğrulandı. Ürün tabloları olan beş sayfanın her birinde 1–10 ürünlerinin tamamı ayrıca arandı. Altı sayfa Poppler ile PNG'ye çevrilip görsel olarak incelendi.

PDF veya kaynak JSON'u sonradan değiştirildiğinde paket güncel gösterilmez. Kayıt düzeltildiğinde önceki PDF yeniden üretim gerektirir. Yeniden üretim aynı güncel paketi değiştirir; ek arşiv sürümü oluşturmaz.

## Yerel kanıtlar

- Test sonucu: `DogruTemin.Tests/TestResults/integrity-archive.trx`
- PDF: `.superpowers/acceptance/integrity-archive/ten-products-six-documents.pdf`
- PDF render'ları: aynı klasörde `document-1.png` … `document-6.png`
- Arşiv pencereleri: aynı klasörde `archive-1024x600.png`, `archive-1280x720.png`, `archive-1366x768.png`, `archive-1920x1080.png`
- Mali kartlar: aynı klasörde `home-cards-1366x768.png`
- Gerçek WPF ölçeği: aynı klasörde `native-dpi.txt` — bu cihazda **%100**.

Bu kanıtlar sentetik veridir ve Git'e eklenmez. Gerçek kullanıcı verileriyle test yapılmadı. %125/%150 fiziksel Windows DPI geçişi ve fiziksel yazıcıdan kağıt kabulü yapılmadı; pencere boyutu/hesap profili kontrolleri bu donanım testlerinin yerine geçti diye sunulmaz.

## Çalıştırılan komutlar

```powershell
dotnet restore DogruTemin.Tests/DogruTemin.Tests.csproj
dotnet build DogruTemin.Tests/DogruTemin.Tests.csproj -c Release --no-restore -p:UseSharedCompilation=false -m:1
dotnet test DogruTemin.Tests/DogruTemin.Tests.csproj -c Release --no-restore -p:UseSharedCompilation=false -p:RunAnalyzers=false -m:1 --logger 'trx;LogFileName=integrity-archive.trx'
dotnet list DogruTemin.Tests/DogruTemin.Tests.csproj package --vulnerable --include-transitive --no-restore
```

İlgili kullanım açıklamaları: [Kayıt ve belge geliştirmeleri](2026-10-25-KAYIT-VE-BELGE-GELISTIRMELERI.md).
