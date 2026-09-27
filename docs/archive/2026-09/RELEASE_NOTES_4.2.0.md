# DoğruTemin 4.2.0 — Yayın Notları

DoğruTemin 4.2.0; doğrudan temin sürecindeki mal/hizmet, firma/teklif, komisyon, tarih, resmî belge, arşiv ve veri güvenliği akışlarını tek Windows masaüstü uygulamasında toplar.

## Öne çıkanlar

- Çok ürünlü ve çok firmalı teklif değerlendirme akışı; `Adet` ve `Miktar` birimleri.
- Yaklaşık maliyet ve nihai kazanan hesabı için merkezi C# hesap motoru.
- Onay Belgesi ve diğer resmî belge/PDF önizleme ve çıktı akışı.
- Masaüstü `DoğruTemin` altında merkezi Veri, Yedekler, Loglar ve PDF klasörleri.
- Eski `Belgeler\DoğruTemin` verilerini silmeden ve hedef dosyaların üzerine yazmadan güvenli geçiş.
- Bütünlük kontrolü, manuel güvenlik yedeği, güncelleme öncesi yedek ve doğrulanmış geri yükleme.
- Beklenmeyen kapanma algılama ve sonraki açılışta koruyucu güvenlik yedeği.
- Kullanıcı işlem JSON/PDF verilerini içermeyen gizlilik odaklı Tanılama Paketi.
- Responsive/DPI, klavye erişilebilirliği, yüksek kontrast ve büyük listelerde sanallaştırma.

## Kalite güvencesi

4.2.0 sürümü GitHub Actions üzerinde Windows Release derlemesi, otomatik birim/entegrasyon/yerleşim/güvenlik/performance testleri, NuGet güvenlik taraması, coverage çıktısı ve gerçek sessiz Setup smoke testi ile denetlenir. Setup smoke testi temiz kurulum, güncelleme, eski program artığı temizleme, kullanıcı verisini koruma ve kaldırma sonrası kullanıcı verisinin kalması senaryolarını kapsar.

## Dağıtım

Final dağıtım paketinde Authenticode kod imzası zorunludur ve ayrıca SHA-256 doğrulama dosyası üretilir. `-AllowUnsignedFinal` yalnız CI/yerel smoke testi içindir ve dağıtım amacıyla kullanılmaz.
