# DoğruTemin 4.3.0 — Yayın Notları

DoğruTemin 4.3.0; doğrudan temin iş akışını, belge hazırlamayı, arşiv yönetimini ve kullanıcı arayüzünü geliştirir.

## Öne çıkanlar

- Tarih sırası uyarıları onaylanmadan taslak dosyaya yazılmaz; kullanıcı onay verirse kayıt devam eder.
- Belge hazırlığı, doğrulama ve arşiv silme/geri yükleme adımları ayrılmış iş akışlarıyla yürütülür.
- Kurum bilgileri ve çıktı ekranlarının okunabilirliği geliştirildi.
- Kullanıcı verileri, arşiv ve yedekleme korumaları sürdürülür.

## Sistem gereksinimleri

- Windows 10 veya Windows 11, 64-bit.
- Kurulum paketi self-contained'dır; hedef bilgisayara .NET Runtime, Visual Studio veya Excel kurulması gerekmez.

## İmza ve dosya doğrulama

Bu 4.3.0 dağıtımı Authenticode ile imzalanmamıştır. Windows SmartScreen bazı bilgisayarlarda "Bilinmeyen yayıncı" uyarısı gösterebilir. Windows güvenlik özelliklerini kapatmayın. Kurulum dosyasını yalnız resmî DoğruTemin GitHub Release sayfasından indirin ve paket içindeki SHA-256 değeriyle doğrulayın.

## Kalite doğrulaması

- Windows Release derlemesi ve otomatik testler.
- Temiz kurulum, güncelleme, kullanıcı verisi koruma ve kaldırma smoke testleri.
- Setup EXE ve SHA-256 doğrulama dosyası.
