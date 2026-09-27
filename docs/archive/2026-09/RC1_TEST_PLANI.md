# DoğruTemin 4.2.0 RC1 — Son kullanıcı test planı

Final Setup ve `v4.2.0` yayını bu liste kullanıcı bilgisayarında başarılı olmadan yapılmaz.

1. **Karşılama ekranı** — dolum çubuğu görünür, yüzde gerçek açılış kilometre taşlarıyla ilerler, `v4.2.0 RC1` görünür.
2. **Ana ekran bilgi kartları** — DoğruFatura standardındaki açık KPI kartları ve koyu premium analiz kartları dengeli görünür; kartlar arasında yazı keskinliği farkı olmamalı.
3. **Metin netliği** — metin taşıyan kartlarda bulanıklık/gölge olmamalı; özellikle ana ekran, mali özet, kontrol merkezi ve arşiv karşılaştırılır.
4. **Pencere/DPI** — normal boyut, büyüt/geri yükle ve mümkünse Windows %125/%150 ölçeklemede kırpılma veya üst üste binme olmamalı.
5. **Tarih temizleme — kritik düzeltme** — Assignment, Approval, Cost Calculation, Market Report, Inspection, Delivery ve Offer tarih alanlarında tarih seç; alan içindeki **×** ile sil; tekrar seç ve **Delete** ile sil; tekrar seç ve **Backspace** ile sil.
6. **Klavye** — Tab sırası mantıklı; Ctrl+S kaydeder; Ctrl+F arşiv aramasına gider; Alt+A/V/K/İ/R/H menüleri çalışır.
7. **Firma/teklif** — en az 3 firma, birden fazla mal/hizmet, adet/miktar birimleri ve teklif hesaplamaları regresyonsuz çalışır.
8. **Belge/çıktı** — önizleme, PDF ve yazdırma akışında kırpılma/bozuk metin olmamalı.
9. **Veri güvenliği** — Güvenlik Yedeği oluştur; veri bütünlüğü kontrolü çalıştır; tanılama paketi işlem JSON/PDF içermeden oluşur.
10. **Kaydedilmemiş değişiklik** — bir alanı değiştirip kapat; uyarı görünmeli ve vazgeçildiğinde uygulama açık kalmalı.
11. **Eski veri geçişi** — 4.1.x `Belgeler\DoğruTemin` verisi varsa ilk açılışta `Masaüstü\DoğruTemin` altına kopyalanmalı; eski kaynak silinmemeli ve hedefteki aynı adlı dosyanın üzerine yazılmamalı.
12. **RC kapısı** — `SETUP_HAZIRLA.bat` RC1 sırasında final Setup üretmemeli; GitHub CI ise `-AllowPrerelease` ile yalnız smoke-test Setup'ı üretmelidir.
