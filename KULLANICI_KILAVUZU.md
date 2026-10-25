# DoğruTemin Kullanıcı Kılavuzu

DoğruTemin, doğrudan temin sürecindeki mal/hizmet, firma/teklif, komisyon, tarih, belge ve arşiv adımlarını tek uygulamada yönetir.

## Tarih alanları
Bir tarih seçildikten sonra alanın sağındaki **×** düğmesiyle temizlenebilir. Klavyeyle tarih alanına geldiğinizde **Delete** veya **Backspace** de tarihi siler. **Enter / Space / Aşağı ok** takvimi açar.

## Veri güvenliği
Kullanıcı verileri `Masaüstü\DoğruTemin` altında tutulur. Güvenlik yedeği ve geri yükleme araçları **Hakkında / Güvenlik** ekranındadır. Beklenmeyen kapanma algılanırsa sonraki açılışta koruyucu yedek alınır.

## Kayıt ve belge işlemleri
En fazla **10 ürün** ve **5 firma** eklenebilir. Fiyat ve miktar girişlerinde en fazla iki ondalık basamak kullanılır. **Teklifi Kaydet**, işlem kaydını da kalıcı olarak kaydeder.

Kayıtlı bilgiler değiştirilirken, düzeltilmiş kaydın mevcut kaydın yerini alacağı açıkça bildirilir ve onayınız alınır. İptal ederseniz diskteki kayıt değişmez. Arşivde tek güncel satır tutulur.

**PDF Kaydet**, son kaydedilmiş işlemden bir güncel belge paketi üretir. Arşivdeki **Klasör** düğmesi PDF ve kaynak bilgilerinin bulunduğu klasörü açar. **Düzenle** kaydı forma getirir; **Sil** seçili satıra uygulanır. Tutar ve tarih sütunları kendi sayısal/tarih değerlerine göre sıralanır.

İşlem düzeltildikten sonra belge durumunda **yeniden üretin** uyarısı çıkar. Önceki basılı belgeleri yeni bilgilerle tekrar oluşturunuz. Uzun metin belgeye sığmıyorsa eksik çıktı yerine açıklayıcı hata gösterilir.

## Tanılama paketi
**Tanılama Paketi** yalnız uygulama/ortam özeti ve son hata günlüklerini içerir; işlem JSON verileri ve PDF belgeleri eklenmez.

## Klavye
- `Ctrl+S`: kaydet
- `Ctrl+F`: arşiv aramasına odaklan
- `Alt+A`: Ana Ekran
- `Alt+V`: Veri Girişi
- `Alt+K`: Kontrol Merkezi
- `Alt+İ`: İstatistikler
- `Alt+R`: Arşiv
- `Alt+H`: Hakkında / Güvenlik
