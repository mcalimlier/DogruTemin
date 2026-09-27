# ArchiveWorkflow Tasarımı

## Amaç

Arşiv kaydı silme ve son silineni geri alma kullanım senaryolarını `MainWindow` olay işleyicilerinden ayırmak. Mevcut kullanıcı mesajları, onay sırası, veri dosyası biçimi ve StorageService'in dosya güvenliği davranışları korunur.

## Mevcut durum

- Derlenen arşiv ekranı `DogruTemin.Wpf/MainWindow.Archive.cs` içindedir. `DogruTemin.Wpf.csproj`, eski `MainWindow.xaml.cs` dosyasını derleme dışı bırakır.
- `MainWindow.Archive.cs`, silme öncesi seçimi ve onayı, aktif kayıt korumasını, işlem sonrası liste yenilemeyi ve geri alma sonucunun sunumunu yönetir.
- `StorageService.DeleteRecord` kaydı önce `Yedekler/Silinenler` altına kopyalar, sonra arşiv kaydını siler ve en fazla 30 silinmiş kopyayı tutar.
- `StorageService.RestoreMostRecentlyDeleted` en son silinen kaydı geri koyar. Aynı RecordId arşivde yeniden kullanılmışsa yeni kimlik üretir.
- StorageService başarısızlıkları mevcut sözleşme gereği bool/null ve uyarı metniyle döndürür. Bu tasarım bu sözleşmeyi değiştirmez.

## Kapsam

İlk uygulama dilimi yalnızca:

1. Aktif kaydın silinmesini engelleme ve arşiv kaydını silme.
2. Son silinen kaydı geri yükleme.

Arşiv listeleme/projeksiyon, arama, filtreleme, sıralama, kayıt açma/kopyalama, JSON dışa aktarma ve ZIP yedeği bu dilimde değişmez. Bunlar daha sonra, küçük ve ayrı adımlarla değerlendirilebilir.

## Bileşen sınırları

### ArchiveWorkflow — DogruTemin.Core

- `StorageService` bağımlılığını kurucudan alır; null bağımlılığı reddeder.
- `CanDelete(recordId, activeRecordId)` ile UI'nın onay penceresini göstermeden önce kullanacağı tek aktif-kayıt kuralını sunar.
- `Delete(recordId, activeRecordId)` aynı kuralı yeniden uygular ve `StorageService.DeleteRecord` çağrısını orkestre eder. Ön kontrol ile silme arasındaki durumda da aktif kayıt korunur.
- `RestoreMostRecentlyDeleted()` mevcut StorageService kurtarma metodunu çağırır ve geri yüklenen kaydı veya mevcut uyarıyı döndürür.
- WPF türlerine, `MessageBox`'a veya görsel duruma bağımlı olmaz.

### Sonuç modeli — DogruTemin.Core

Workflow sonucu işlemin durumunu ve gerekiyorsa geri yüklenen `ProcurementData` nesnesini ve StorageService uyarısını taşır. Durumlar hazır, tamamlandı, engellendi ve başarısız ayrımını sağlar. Türkçe kullanıcı mesajlarını üretmek UI'nın sorumluluğunda kalır; mevcut metinler korunur.

### MainWindow — DogruTemin.Wpf

- `ArchiveWorkflow` örneğini mevcut `StorageService` ile başlatır.
- Silme onay penceresini workflow ön kontrolünden sonra gösterir; onay verilince workflow silme metodunu çağırır.
- Engellendi/başarısız/tamamlandı durumlarını mevcut mesaj ve bildirimlerle sunar.
- Başarılı silme veya geri yükleme sonrası arşiv listesini yeniler; geri yüklenen kaydı seçer.

### StorageService

Dosya yolu, JSON serileştirme, atomik yazma, güvenli silinen kayıt kopyası, saklama sınırı ve kimlik çakışması çözümü StorageService'te kalır. Bu ilk dilimde StorageService değiştirilmez.

## Veri akışı

### Silme

1. UI seçili satırı doğrular.
2. UI `CanDelete` çağırır; aktif kayıt engeli aynı mevcut metinle gösterilir.
3. UI mevcut silme onayını gösterir.
4. Onaydan sonra `Delete` aktif-kayıt kontrolünü tekrarlar ve StorageService'i çağırır.
5. UI mevcut başarı/hata davranışını sürdürür ve listeyi yeniler.

### Son silineni geri alma

1. UI workflow'u çağırır.
2. Workflow StorageService'ten sonucu alır.
3. UI başarısızlık uyarısını gösterir; başarıda listeyi yeniler, dönen kaydı seçer ve mevcut bildirimi gösterir.

## Davranış koruma koşulları

- Aktif kayıt silinemez.
- Silme onayı ve mesaj metni korunur.
- Silinen kayıt, mevcut güvenli kopyalama ve 30 kayıtlık saklama davranışını sürdürür.
- Geri yükleme en son silinen kaydı seçer; RecordId çakışmasında StorageService'in yeni kimlik üretmesi değişmez.
- Filtre/sıralama ve arşiv ekranı projeksiyonu değişmez.
- StorageService'in mevcut hata sözleşmesi ve kullanıcıya gösterilen mevcut hata metinleri korunur.

## Test yaklaşımı

Geçici kök klasör kullanan gerçek StorageService ile ArchiveWorkflow davranışı doğrulanır:

- Aktif kayıt için silme ön kontrolü engeller ve dosyaya dokunmaz.
- Başka bir kayıt silinince arşivden çıkar ve silinenler yedeği oluşur.
- Geri alma kaydı aynı kimlik/içerikle arşive döndürür.
- Geri alınacak kayıt yokken başarısız sonuç ve mevcut uyarı döner.
- Mevcut StorageService testleri ve golden senaryo aynen korunur.

## Risk ve azaltma

- **Onay penceresi sırası:** Ön kontrol onaydan önce yapılır; workflow silme çağrısında korumayı tekrarlar.
- **StorageService hatalarının sınırlı ayrıntısı:** DeleteRecord yalnızca bool döndürür. UI mevcut genel hata metnini korur; bu çalışma diliminde StorageService API'si genişletilmez.
- **Kapsamın büyümesi:** Liste, kopyalama, açma ve dışa aktarma bu adımın dışında tutulur.
