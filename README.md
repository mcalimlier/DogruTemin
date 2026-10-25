# DoğruTemin 4.3.0

DoğruTemin; doğrudan temin süreçlerinin hazırlanması, firma tekliflerinin karşılaştırılması, belge üretimi, arşiv ve veri güvenliği için geliştirilmiş .NET 8 / WPF Windows masaüstü uygulamasıdır.

## 4.3.0

- Mal/hizmet, firma ve teklif yönetimi
- Yaklaşık maliyet ve nihai teklif değerlendirme motoru
- Onay Belgesi ve diğer resmî belge/PDF çıktıları
- İşlem arşivi, otomatik yedekleme ve doğrulanmış geri yükleme
- Beklenmeyen kapanma sonrası kurtarma yedeği
- İşlem JSON/PDF verilerini dışarı çıkarmayan gizlilik odaklı Tanılama Paketi
- Responsive/DPI koruması, klavye erişilebilirliği ve yüksek kontrast desteği
- xUnit otomatik testleri, GitHub Actions build/test/setup smoke kalite kapıları
- NuGet güvenlik taraması ve coverage raporu
- Bu sürüm, yayın kararıyla Authenticode sertifikası olmadan dağıtılır; Windows SmartScreen uyarısı gösterebilir.

## Kullanıcı verileri

25 Ekim geliştirme dalı; 10 ürün kapasitesi, iki ondalık giriş kontrolü, onaylı kayıt düzeltme, kayıtla eşleşen PDF paketi ve satır düğmeli arşiv listesini içerir. Ayrıntılar: [Kayıt ve belge geliştirmeleri](docs/2026-10-25-KAYIT-VE-BELGE-GELISTIRMELERI.md).

Yeni veri kökü `Masaüstü\DoğruTemin` klasörüdür. Eski `Belgeler\DoğruTemin` verileri ilk açılışta kaynak silinmeden ve mevcut hedef dosyaların üzerine yazılmadan güvenli biçimde kopyalanır.

## Geliştirme ve kalite

Merkezi sürüm bilgisi `Directory.Build.props` dosyasındadır. Compiler warning'leri build kapısıdır; .NET analizörleri ve code-style denetimi aktiftir. 4.3.0 imzasız dağıtım istisnası ve imza politikası için `CODE_SIGNING.md` dosyasına bakın. Yeni Setup üretmek için `exe_olustur\EXE_OLUSTUR.bat` dosyasını çalıştırın.
