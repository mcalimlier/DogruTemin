# İş Akışı Ekran Düzenlemeleri — Tasarım

**Tarih:** 2026-09-27  
**Durum:** Kullanıcı tasarım yaklaşımını onayladı; yazılı tasarım incelemesi bekleniyor.

## Amaç

Kurum, firma/teklif, onay belgesi, belge tarihleri ve çıktı ekranlarını daha anlaşılır ve tutarlı hale getirmek; kullanıcıdan daha önce girdiği tarihi yeniden hatırlamasını istememek; tarihleri kaydederken mevcut kronoloji kurallarını uygulamak.

## Kapsam ve kararlar

1. **Kurum ve komisyon alanları:** “Mal/Hizmet Bilgisi” ekranındaki “Kurul / Komisyon Onay Tarihi” ile “Komisyon Sayısı” alanları “Kurum Bilgisi” ekranına taşınır. Sayı alanının görünür adı “Komisyon Onay Sayısı” olur. Verilerin mevcut `ProcessDates.AssignmentDate` ve `ProcessDates.AssignmentNo` alanlarında saklanması sürdürülür; böylece mevcut kayıtlar ve belgelerle uyumluluk korunur.
2. **Firma telefonu:** Firma telefon girişinde `(0)` sabit, düzenlenemeyen önek olarak gösterilir; kullanıcı numaranın kalanını yazar. Saklanan telefon değeri tek bir `0` öneki içerecek şekilde düzenlenir. Var olan kayıt yüklenirken baştaki `0` arayüzde ikinci kez gösterilmez. Diğer numara karakterlerine yönelik yeni bir doğrulama veya ülke/uzunluk kısıtı bu değişikliğin kapsamına girmez.
3. **Firma/Teklifler hizası:** Sol firma formu ve firma listesi, sağdaki teklif düzenleme alanıyla ortak alt hizaya gelecek biçimde yeniden düzenlenir. Sabit ekran yüksekliğinde taşmayı önlemek için mevcut kolon ve içerik boyutları birlikte ayarlanır; gerekirse iç listeler kendi kaydırma alanlarını kullanır.
4. **Açılır listeler:** Ortak ComboBox arayüzünün seçim alanı, metin bölgesi ve ok dâhil herhangi bir yerine tıklamak listeyi açar. Açık listeden seçim ve listenin kapanması mevcut davranışı korur.
5. **Onay Belgesi ekranı:** Dikey kaydırma çubuğu kaldırılır. Kart aralıkları, iç boşluklar ve metin alanı yüksekliği uygulamanın kullanılabilir pencere alanına sığacak şekilde düzenlenir; gerekli bilgiler ve alanlar kaldırılmaz.
6. **Onay tarihi bilgisi:** “Belgeler / Tarihler” ekranında, “Onay Belgesi” ekranındaki `ApprovalDocumentDate` değerini gösteren salt okunur bir bilgi yer alır. Tarih burada ikinci kez girilmez.
7. **Tarihleri kaydetme:** “Belgeler / Tarihler” ekranına bir Kaydet düğmesi eklenir. Düğme tarih alanlarını mevcut ortak kaydetme akışına aktarır; kaydetme öncesi `DateValidationService.RequiredSequenceIssues` kuralları hata düzeyinde değerlendirilir. Kronoloji hatası varsa kayıt durdurulur ve ihlal edilen kural Türkçe gösterilir. Geçerli tarihler mevcut kayıt akışından kaydedilir. Başka ekranlardan yapılan genel kaydetme davranışı korunur.
8. **Çıktı varsayılanları:** Çıktı belge listesi ilk oluşturulduğunda bütün belgeler seçili olur. Kullanıcı her belgeyi bağımsız biçimde kaldırıp yeniden seçebilir; sekme geçişleri seçimleri sıfırlamaz.

## Kapsam dışı

- Veri şeması veya eski kayıtların biçimi değiştirilmez.
- Tarih sıralama kuralları yeniden tanımlanmaz; mevcut `DateValidationService` kuralları kullanılır.
- Telefon alanına yeni telefon doğrulama, ülke kodu seçimi veya numara uzunluğu kısıtı eklenmez.
- Çıktı seçiminin işlem verisine kalıcı olarak yazılması bu çalışmada yapılmaz.

## Kabul ölçütleri

- Kurul/komisyon onay tarihi ve sayısı yalnızca Kurum Bilgisi ekranında düzenlenebilir; kaydedilip yeniden açıldığında mevcut değerler korunur.
- Komisyon sayı alanı ekranda “Komisyon Onay Sayısı” olarak görünür ve mevcut belgelerde aynı sayı üretilir.
- Telefon girişinde sabit `(0)` görünür; eski veya yeni numarada baştaki sıfır iki kez gösterilmez ve kaydedilen değer tek sıfır öneki taşır.
- Firma ve teklif panellerinin alt kenarları aynı hizadadır; alanlar dar pencerede erişilebilir kalır.
- Ortak görsel stile sahip açılır listelerde ok dışındaki seçim alanına tıklamak listeyi açar; listeden seçim çalışır.
- Onay Belgesi sayfasında dikey kaydırma çubuğu görünmez ve alanlara erişilebilir.
- Tarihler ekranındaki bilgi, Onay Belgesi ekranındaki tarihle aynı değeri gösterir.
- Tarihler ekranından geçersiz kronolojiyle kayıt engellenir ve anlaşılır hata gösterilir; geçerli kronoloji kaydedilir.
- Çıktı ekranı ilk oluşturulduğunda tüm belgeler seçilidir; kullanıcı seçimleri değiştirebilir ve alt sayfalar arasında geçişte korunur.
- İlgili testler ve Release test paketi geçer; yeni Windows EXE başlatılıp açılışı doğrulanır.

## Uygulama ve doğrulama notları

Uygulama öncesi yazılı tasarım gözden geçirilecektir. Uygulama planı onayından sonra testler önce başarısız olacak şekilde eklenir veya genişletilir; ardından mevcut WPF kontrolleri, `DateValidationService` ve kaydetme akışı kullanılarak değişiklikler yapılır. Son aşamada Release testleri, yayın derlemesi ve uygulama açılışı doğrulanır.
