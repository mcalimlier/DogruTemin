# Sabit menü ve ekran ayarları

Kullanıcı isteği: Veri Girişi açılır başlığını kaldırmak, alt ekranları sürekli göstermek; Kontrol Merkezi ile başlayan grubu imzaya yaklaştırmak; farklı ekranlarda erişilebilir çalışma alanı ve kalıcı Ayarlar ekranı; Hakkında/Güvenlik görünümünü iyileştirmek.

Uygulama: Sabit boyutlu, bütünüyle küçültülen Viewbox yerine doğal WPF yerleşimi kullanılır. Dar alanlarda metin boyutu korunur ve içerik kaydırılabilir. Menü alt grubu DockPanel ile alta dayanır, çok kısa pencerelerde menü kaydırılır. Üst çubuk dar pencerede ikincil durum kartlarını gizler. Pencere tercihleri Auto, 1280×720, 1366×768, 1920×1080 ve ekranı kapla olarak ayarlara kaydedilir. Geçerli monitörün çalışma alanı ve DPI oranı dikkate alınır. Kurum bilgilerini kaydetmek pencere tercihini korur.

Dar pencerelerde iki sütunlu form grupları alt alta geçer; ana ekran ve istatistik kartları iki sütuna iner. Arşiv, istatistik ve kontrol başlıkları eylem düğmeleriyle çakışmamak için ayrı satırdadır. Arşiv ve kontrol ekranlarının kısa pencerelerde tablo yüksekliği korunur; gerektiğinde dış panel kaydırılır. Tablo sütunları kendi yatay kaydırmalarını kullanır.

Kullanıcının sonraki isteğiyle tipografi ve düğmeler Ücretli Öğretmen Hakediş projesinin `src/Hakedis.App/Themes/Oehs.xaml` ve `Workspace.xaml` kaynaklarıyla eşleştirildi: Segoe UI; normal metin/etiket 11,5, giriş alanı 12, bölüm başlığı 13,5, sayfa başlığı 19,5; işlem düğmeleri 36 yükseklik ve kalın 11,5 metin. Lacivert, mavi ve altın renk ailesi korunur. Büyük metrikler ve kart başlıkları görsel hiyerarşi için ayrı ölçülerini kullanır.

Doğrulama: Boyut ve DPI hesaplamaları, ayarların diske yazılıp okunması, eski/geçersiz ayarların otomatik seçime dönmesi, sabit menü ve tüm panellerin 1024×600 ile 1366×768 boyutlarında düğme/son satır erişimi. Arşiv ayrıca 1280×720 ve 1920×1080 boyutlarında incelenir. Bunlar pencere boyutu ve DPI hesaplama testleridir; işletim sisteminin DPI ayarını değiştirmezler. Görsel çıktılar yerel .superpowers/panel-review altında incelenir.

Sonuç: Release yapılandırmasında 128 testin tamamı geçti. On iki panelin iki boyuttaki ekran görüntüleri üretildi; Ayarlar ve Hakkında ekranlarının gereksiz yatay kaydırma üretmediği, küçük pencerede firma/teklif bölümlerinin alt alta geçtiği, normal pencerede alt menünün imzaya dayandığı doğrulandı. Arşivin sabit sütunlarına minimum genişlik verildi; böylece Düzenle/Klasör/Sil düğmeleri dar pencere nedeniyle küçülmüyor. Test raporu: `DogruTemin.Tests/TestResults/native-layout.trx` (yerel çıktı).
