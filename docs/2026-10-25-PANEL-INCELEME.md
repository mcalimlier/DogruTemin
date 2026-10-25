# Panel incelemesi ve arayüz iyileştirmeleri

11 görünüm, 1024×600 ve 1366×768 pencere boyutlarında incelendi. Yerel kanıtlar `.superpowers/panel-review/before` ve `after` klasörlerindedir. Sentetik 10 ürünlü kayıt kullanıldı; gerçek kullanıcı verileri değiştirilmedi.

## Bulgular ve düzeltmeler

- Ortak düğme şablonunda uygulamanın genel TextBlock stili, düğmeye verilen yazı boyutunu bastırıyordu. Font boyutu/ağırlığı/ailesi/rengi düğmeden alınacak şekilde düzeltildi. Özellikle veri ve yedek klasörü düğmelerindeki kesilme giderildi.
- Teklif tablosunun yaklaşık/nihai fiyat başlıkları kesiliyordu. Premium tablo başlıkları satıra bölünebilir hale getirildi. Hücre metinleri için üç nokta ve tam metin ipucu eklendi.
- Genel, onay belgesi ve tarih panellerinde gerektiğinde dikey kaydırma açıldı. Normal boyutta içerik sığıyorsa gereksiz kaydırma çubuğu görünmez.
- Kontrol merkezinin uzun açıklamaları satıra bölünüyor; satır yüksekliği içeriğe göre büyüyebiliyor.
- Güvenlik durumunun koyu zemindeki yeşil/kırmızı renkleri okunabilir açık tonlara taşındı; kontrast testleri eklendi.
- Kayıt açılırken ürün sayacının eski değerde kalması düzeltildi. Kaydedilen teklif düzenleyicide tekrar yüklendiği için `19,5300` girişi başarıyla kaydedildikten sonra `19,53` gösteriliyor.
- Çıktı panelinde eksik komisyon/firma sayıları artık tamamlanmış gibi yeşil gösterilmiyor. Eksik durum sarı, tamamlanan durum yeşil.
- Ürün/firma satırı simgelerine açıklama ve erişilebilir ad; belge eksikliği notlarına tam metin ipucu eklendi. Fiyat hata mesajında iki ondalık sınırı açıkça belirtiliyor.

## Doğrulama

- Release testleri: **124/124 geçti**, atlanan ve başarısız test yok.
- Tek WPF oturumunda 11 görünüm × 2 pencere boyutu kontrol edildi; önce/sonra görüntüleri alındı.
- Düğme yazısı kesilmesi ölçümü: önce güvenlik panelindeki iki düğmede sorun, sonra **0 bulgu**.
- Ürün ve teklif tablolarının 10. satırına kaydırarak erişim doğrulandı.
- Ürün sayacı, fiyatın kanonik gösterimi ve kontrast regresyonları geçti.
- Önceki hesaplama, kayıt, arşiv, veri güvenliği ve PDF testleri de aynı koşumda geçti.

Komut:

```powershell
$env:DOGRU_TEMIN_PANEL_AUDIT = Join-Path (Get-Location) '.superpowers/panel-review/after'
dotnet test DogruTemin.Tests/DogruTemin.Tests.csproj -c Release --no-restore -p:RunAnalyzers=false -p:UseSharedCompilation=false -m:1 --logger 'trx;LogFileName=panel-review.trx'
```

Test sonucu `DogruTemin.Tests/TestResults/panel-review.trx` dosyasındadır. Pencere görüntüleri WPF render'larıdır; Windows DPI ayarının değiştirildiği veya fiziksel ekran/yazıcı kabulü yapıldığı iddia edilmez.
