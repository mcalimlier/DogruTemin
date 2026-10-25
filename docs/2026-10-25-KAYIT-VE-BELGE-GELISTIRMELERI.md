# DoğruTemin — kayıt, belge ve arşiv geliştirmeleri

Dal: `codex/dogrutemin-integrity-and-archive`.

## Kullanım

1. En fazla **10 ürün / mal / hizmet** ve **5 firma** girilebilir. Sayısal girişler en fazla iki anlamlı ondalık basamak kabul eder. Fazla basamak yuvarlanarak gizlenmez. `19,53` ve `19,5300` aynı tutardır.
2. **Teklifi Kaydet**, teklif ile birlikte işlem kaydını da diske yazar. İptal veya başarısızlıkta yeşil kalıcı kayıt mesajı gösterilmez. Üstteki Kaydet de açık teklif değişikliklerini işler.
3. Kayıtlı işlem değiştiriliyorsa işlem adı belirtilerek **üzerine yazma onayı** alınır. Hayır cevabında diskteki kayıt değişmez. Arşivde eski sürümler için ek satır oluşturulmaz; mevcut güvenlik yedekleri devam eder.
4. PDF ve yazdırma, son kaydedilmiş işlemden hazırlanır. Kaydedilmemiş değişiklik varsa önce aynı doğrulama ve onay akışı tamamlanır. Eşit teklif kazananı da belge öncesinde kayda alınır.
5. **PDF Kaydet**, işlemin belge klasöründe tek güncel `DogruTemin_Belgeler.pdf` ile `Belge_Kaynak.json` üretir. Son seçilen belge grubu bu güncel paketin içeriğidir. Kaynak kimliği, kayıt sürümü, belge türleri, uygulama/şablon sürümü ve SHA-256 özetleri ilişkilendirilir.
6. Arşivdeki **Düzenle**, **Klasör**, **Sil** düğmeleri ilgili satıra uygulanır. Düzenle doğrulamalı forma gider. Aktif kaydı yanlışlıkla silmeme ve silineni geri alma korumaları sürer. Arama, filtre ve sıralama korunur.
7. Arşiv, nihai tutarı ve belge durumunu gösterir. Kayıt değiştirilmişse **Değişti • yeniden üretin**, PDF/kaynak dosyası eksik veya değiştirilmişse ilgili uyarı görünür. Önceki basılı belgeler değişmez; düzeltme sonrasında yeniden üretilmelidir.
8. Uzun metin güvenli biçimde sığdırılamıyorsa alanın başlangıcı belirtilerek işlem durdurulur. Metnin son satırları sessizce atılmaz; başarısız üretimde önceki PDF paketi korunur. Yazdırmadan önce aynı sığma kontrolü uygulanır.

## Veri geçişi

Yeni kayıt şeması **5**'tir. Şema 1–4 kayıtları okunur. İlk üzerine yazmadan önce eski dosyalar, otomatik yedek kapalı olsa bile `Yedekler/Kayıtlar/gecis_v5_*.json` altında bir kez korunur. Eski uygulamaların 10 ürünlü yeni kayıtları 8 ürüne kırpmasını önlemek için yeni şema eski uygulamalarda açılmaz.

11 ürün veya 6 firma içeren dış kayıt kesilmez. Dosya korunur ve açıklayıcı hata gösterilir. Arşiv listesinde okunamayan kayıtların dosya adları ve nedenleri ayrıca bildirilir.

## Görünüm

- EDHS'deki lacivert/altın kart dili, para/transfer/koruma/kurum vektör sembolleriyle mali özete uyarlandı.
- Arşiv, açık renk hücreleri ve satır işlemleriyle Excel benzeri listeye dönüştürüldü. Başlıklar satıra bölünür; tablo gerektiğinde kaydırılabilir.
- Pencere minimumu çalışma alanını aşmaz. Küçük ekranlar ve yüksek ölçeklerdeki pencere sığma hesapları test edilir. 1024×600, 1280×720, 1366×768 ve 1920×1080 boyutlarında gerçek WPF pencere yerleşimleri kontrol edilir.
- Pencere boyutu testi, Windows'un DPI ayarını değiştirmek değildir. Gerçek DPI bilgisi kabul klasöründeki `native-dpi.txt` dosyasındadır.

## Doğrulama

Testler geçici, sentetik kayıtlarla çalışır. Kullanıcının gerçek işlem/veri klasörleri kullanılmaz. Nihai kabul sonuçları aynı klasördeki `2026-10-25-KABUL-SONUCLARI.md` dosyasına kaydedilir.

Altı belge türü için 10 ürün, 5 firma, A4 boyutu, Türkçe karakterler, tarihler, imzacılar ve tutarlar nihai PDF'den çıkarılarak kontrol edilir. Dosya değiştirme, eski kayıtla çıktı denemesi, düzeltme onayı, tek güncel arşiv satırı, kapasite aşımı ve önceki belgenin korunması regresyon kapsamındadır.

## Bağımlılık güvenliği

Tarama, PDFsharp-WPF üzerinden gelen `System.Security.Cryptography.Pkcs 8.0.1` bağımlılığında iki bilinen açık gösterdi. Aynı ana sürümün düzeltilmiş **8.0.2** paketi açıkça seçildi. Kaynaklar: [Microsoft duyurusu / CVE-2026-84008](https://github.com/advisories/GHSA-846f-f764-76mx), [Microsoft duyurusu / CVE-2026-70162](https://github.com/advisories/GHSA-2fmv-rv2c-59x7).

## Yeniden çalıştırma

```powershell
dotnet restore DogruTemin.Tests/DogruTemin.Tests.csproj
$env:DOGRU_TEMIN_ACCEPTANCE_DIR = Join-Path (Get-Location) '.superpowers/acceptance/integrity-archive'
dotnet test DogruTemin.Tests/DogruTemin.Tests.csproj -c Release --no-restore
dotnet list DogruTemin.Tests/DogruTemin.Tests.csproj package --vulnerable --include-transitive
```

Kabul klasöründeki PNG'ler WPF pencere render'larıdır. PDF, Poppler ile ayrıca görsel olarak incelenmiştir; fiziksel yazıcıdan kağıt çıktısı alınmamıştır.
