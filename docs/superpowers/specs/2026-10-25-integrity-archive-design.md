# DoğruTemin kayıt ve belge bütünlüğü

Kapsam, kullanıcının 25 Ekim 2026 tarihli dokuz maddelik uygulama talimatıdır.

- Mevcut WPF/.NET uygulaması korunur. Fiyat ve miktar girişleri en fazla iki ondalıktır; fazla basamak sessizce yuvarlanmaz.
- Kapasite 10 ürün ve 5 firmadır. Yükleme ve kaydetmede fazla kayıt kesilmez; açıklayıcı hata verilir, kaynak dosya korunur.
- Mevcut arşiv kaydı değişecekse işlem adı ve sonuçları belirtilerek onay alınır. Tek güncel kayıt tutulur; arşivde eski sürüm satırları oluşturulmaz. Mevcut güvenlik yedekleri korunur.
- Son çıktı kayıt kimliği, içerik özeti, kayıt sürümü, belge türleri ve dosya özetiyle ilişkilendirilir. Nihai çıktı kayıtlı veriden üretilir. Düzeltme sonrasında eski çıktı güncel sayılmaz.
- Arşiv Excel benzeri satırlarda tutar ve çıktı durumu gösterir; satır yanında Düzenle, Klasör, Sil eylemleri bulunur. Satır üzerinde kontrolsüz parasal düzenleme yapılmaz; Düzenle mevcut doğrulamalı forma gider.
- Teklifi Kaydet tüm işlem kaydını da diske yazar. Başarısızlık/iptal halinde kaydedildi mesajı gösterilmez.
- PDF ve yazdırma öncesi metin sığması doğrulanır. Metin kaybı yerine alanı belirten hata verilir. Nihai PDF metni ve tüm 10 ürün test edilir.
- Küçük ekranlarda pencere çalışma alanına sığar; tablolar kaydırılır, işlemler erişilebilir kalır. Hesaplanan ekran profilleri gerçek DPI kabulü diye sunulmaz.
- EDHS kartlarının lacivert/altın görsel dili ve vektör sembolleri uyarlanır. Mevcut ekran adları/iş akışı korunur.

## Yaklaşım
Tek güncel kayıt + içerik kimliği seçildi: kullanıcı eski arşiv sürümlerini istemiyor. Ayrı revizyon arşivi ve uygulamayı yeniden yazma gereksiz. Sayısal revizyon yalnız çıktı eşleştirme/çakışma kontrolü içindir, yeni arşiv dosyası yaratmaz.

## Kabul
İki ondalık sınırı; 10 ürün hesap/kayıt/PDF; 11 ürün ve 6 firma için açık hata; onaysız değiştirme engeli; çıktı sonrası düzeltmede eski bağlantının geçersiz olması; iptalde kaydın değişmemesi; taşan metinde hedef PDF'nin korunması; arşiv satır eylemleri; küçük pencere/DPI erişilebilirliği regresyonlarla sınanır.
