# DoğruTemin 4.2.0 RC1 — Final Kalite Kontrolü

Bu liste otomatik CI kontrollerine ek olarak gerçek Windows oturumunda yapılacak son kullanıcı testidir. Final `v4.2.0` etiketi ve final Setup bu liste tamamlanmadan üretilmez.

## A. Görsel ekran turu

Her ekranda kesik yazı, taşan buton, bulanık metin, yatay kaydırma ve gereksiz boşluk olmamalıdır.

- [ ] Ana Ekran
- [ ] Mal / Hizmet girişi
- [ ] Firma / Teklif girişi
- [ ] Komisyon / tarih alanları
- [ ] Maliyet / teklif sonucu
- [ ] Resmi belgeler / önizleme
- [ ] İşlem arşivi
- [ ] Yönetim / mali özet
- [ ] Kontrol merkezi
- [ ] Hakkında / Güvenlik
- [ ] Açılış ekranı

## B. Ekran / ölçekleme

- [ ] 1366×768 / %100
- [ ] 1920×1080 / %100
- [ ] 1920×1080 / %125
- [ ] 1920×1080 / %150
- [ ] Mümkünse 2K / %150–175
- [ ] Mümkünse 4K / %200
- [ ] Pencere küçültme / büyütme / geri yükleme

## C. Klavye ve erişilebilirlik

- [ ] TAB sırası doğal ilerliyor
- [ ] Klavye odağı net görünüyor
- [ ] Delete / Backspace tarih alanlarını güvenli temizliyor
- [ ] Ctrl+S kaydediyor
- [ ] Ctrl+F arşiv aramasına gidiyor
- [ ] Alt+A/V/K/İ/R/H menüleri çalışıyor
- [ ] Windows Yüksek Karşıtlık modunda temel kontroller okunuyor

## D. İşlevsel uçtan uca tur

- [ ] Birden fazla mal / hizmet ekleniyor
- [ ] Adet / Miktar birimi doğru korunuyor
- [ ] En az 3 ve daha fazla firma teklif akışı doğru çalışıyor
- [ ] Eksik / geçersiz tekliflerde kontrol merkezi doğru uyarıyor
- [ ] Eşit teklif halinde kullanıcı seçimi olmadan kazanan otomatik atanmaz
- [ ] Kaydet / yeniden aç akışında veri kaybı yok
- [ ] İşlem kopyalama / silme / silineni geri alma çalışıyor
- [ ] Belge önizleme / PDF / yazdırma kırpılmadan oluşuyor
- [ ] Onay Belgesi iki sütunlu blokları aynı merkez aksında
- [ ] Güvenlik yedeği oluşuyor
- [ ] Yedekten geri yükleme çalışıyor
- [ ] Bozuk JSON veri bütünlüğü kontrolünde tespit ediliyor
- [ ] Tanılama Paketi oluşuyor ve kullanıcı JSON/PDF verisi içermiyor
- [ ] Beklenmeyen kapanma sonrası koruyucu güvenlik yedeği oluşuyor
- [ ] Kaydedilmemiş değişiklik uyarısı veri kaybını engelliyor

## E. Eski veri geçişi

- [ ] 4.1.x `Belgeler\DoğruTemin` verisi `Masaüstü\DoğruTemin` altına kopyalanıyor
- [ ] Eski kaynak silinmiyor
- [ ] Hedefte aynı isimli dosya varsa üzerine yazılmıyor
- [ ] Geçiş sonrası aktif işlem ve arşiv kayıtları açılıyor

## F. Setup turu

CI otomatik olarak temiz kurulum, güncelleme ve kaldırma sonrası kullanıcı verisi korumasını test eder. Final paket ayrıca gerçek kullanıcı bilgisayarında doğrulanır.

- [ ] Temiz Setup kurulumu
- [ ] Mevcut sürüm üstüne güncelleme
- [ ] Program kaldırma
- [ ] Yeniden kurulum
- [ ] `Masaüstü\DoğruTemin` kullanıcı verileri tüm aşamalarda korunuyor
- [ ] SHA-256 dosyası Setup ile eşleşiyor
- [ ] Kod imzası kullanılıyorsa Windows özelliklerinde imza geçerli görünüyor
- [ ] RC sürümünde normal `SETUP_HAZIRLA` final paket üretmiyor

## Final şartı

`build-and-test` ve `setup-smoke` kontrolleri yeşil, bu manuel liste kritik hata olmadan tamamlanmış ve kullanıcı RC1'i onaylamış olmalıdır. Bundan sonra `VersionSuffix` kaldırılır, `v4.2.0` oluşturulur ve final Setup üretilir.
