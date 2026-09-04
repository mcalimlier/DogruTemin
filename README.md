# DoğruTemin 4.2.0

DoğruTemin; doğrudan temin süreçlerinin hazırlanması, firma tekliflerinin karşılaştırılması, belge üretimi, arşiv ve veri güvenliği için geliştirilmiş .NET 8 / WPF Windows masaüstü uygulamasıdır.

## 4.2.0 Final

- Mal/hizmet, firma ve teklif yönetimi
- Yaklaşık maliyet ve nihai teklif değerlendirme motoru
- Onay Belgesi ve diğer resmî belge/PDF çıktıları
- İşlem arşivi, otomatik yedekleme ve doğrulanmış geri yükleme
- Beklenmeyen kapanma sonrası kurtarma yedeği
- İşlem JSON/PDF verilerini dışarı çıkarmayan gizlilik odaklı Tanılama Paketi
- Responsive/DPI koruması, klavye erişilebilirliği ve yüksek kontrast desteği
- xUnit otomatik testleri, GitHub Actions build/test/setup smoke kalite kapıları
- NuGet güvenlik taraması ve coverage raporu
- Final dağıtımda Authenticode kod imzası güvenlik kilidi

## Kullanıcı verileri

Yeni veri kökü `Masaüstü\DoğruTemin` klasörüdür. Eski `Belgeler\DoğruTemin` verileri ilk açılışta kaynak silinmeden ve mevcut hedef dosyaların üzerine yazılmadan güvenli biçimde kopyalanır.

## Geliştirme ve kalite

Merkezi sürüm bilgisi `Directory.Build.props` dosyasındadır. Compiler warning'leri build kapısıdır; .NET analizörleri ve code-style denetimi aktiftir. Final Setup üretimi için `CODE_SIGNING.md` içindeki politika uygulanır.
