# Panel kullanılabilirliği inceleme planı

Kapsam: Kullanıcının arayüz ve panellerde gerekli iyileştirmeleri kontrol etme talimatı. Mevcut tema ve iş akışı korunur.

- [x] Gerçek WPF penceresinde 11 görünümü aç, sentetik 10 ürünlü kayıtla görüntülerini al; görünür düğmelerde yazı kesilmesini ölç.
- [x] Ortak düğme şablonunun açık yazı boyutlarını uygulamasını sağla; panelde dar alanda kalan metin ve tablo satırlarını düzelt.
- [x] Genel/onay/tarih formlarında gerektiğinde kaydırmayı aç; tablo başlıklarının kesilmesini gider.
- [x] Önce/sonra görüntülerini karşılaştır; iki pencere boyutu, 10. ürün ve 124/124 testle doğrula.
- [x] Bulguları ve doğrulamayı raporla; mevcut codex dalına yerel commit için hazırla.

Doğrulama: `PanelReviewAcceptance` tek uygulama oturumunda gezinir; gerçek kullanıcı verilerini kullanmaz. Pencere boyutları Windows DPI ayarının değiştirildiği anlamına gelmez.
