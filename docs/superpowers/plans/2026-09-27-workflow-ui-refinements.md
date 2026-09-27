# İş Akışı Ekran Düzenlemeleri Uygulama Planı

> **Agentic worker:** Bu planı görev görev uygulamak için `superpowers:subagent-driven-development` (önerilen) veya `superpowers:executing-plans` alt becerisini kullanın. Adımlar izleme için `- [ ]` kutuları kullanır.

**Amaç:** Kurum, firma/teklif, onay belgesi, tarih ve çıktı ekranlarını kullanıcının sekiz isteğine göre iyileştirmek; mevcut kayıt verileriyle uyumluluğu korumak.

**Mimari:** Mevcut WPF `MainWindow` ve ortak ComboBox şablonu düzenlenecek. Tarih kuralları mevcut `DateValidationService` üzerinden çalışacak; yeni veri şeması eklenmeyecek. WPF entegrasyon testleri davranışları, Release derlemesi ve son açılış kontrolü yayınlanabilir uygulamayı doğrulayacak.

**Teknoloji:** .NET 8, WPF, xUnit, PowerShell.

**Tasarım:** `docs/superpowers/specs/2026-09-27-workflow-ui-refinements-design.md`

## Genel kısıtlar

- Veri şeması veya eski kayıtların biçimi değiştirilmeyecek.
- Tarih sıralama kuralları yeniden tanımlanmayacak; `DateValidationService` kullanılacak.
- Telefon boş bırakılabilir; girilmişse sabit `(0)` dışında tam 10 rakam olmalı. Ülke kodu seçimi veya başka biçim kısıtı eklenmeyecek.
- Çıktı seçimi işlem verisine kalıcı yazılmayacak.
- Firma/teklif, onay, tarih ve çıktı ekranlarındaki mevcut işlevler korunacak.

## İnceleme odağı

- **Eski komisyon alanı değerleri:** Taşınan tarih ve sayı, `BuildDataFromUi` ile kayıt ve yeniden yüklemede korunmalı. Test: taşınmış kontrollerin Kurum Bilgisi altında bulunması ve aynı `ProcessDates` değerlerini vermesi.
- **Telefonun eski biçimleri:** Baştaki `0` olan/olmayan numaralar, sabit önekle gösterilirken iki kez `0` üretmemeli; boş, 9, 10, 11 rakamlı ve ayraçlı değerler doğru kabul/red edilmelidir. Test: editör/kayıt dönüşümü ve tam 10 rakam doğrulaması.
- **Açılır liste öğe seçimi:** Açık ComboBox içinden seçim yapınca tıklama yeniden açılış tetiklememeli. Test: ortak davranış seçimden sonra listenin kapalı kalması.
- **Pencere yüksekliği ve ekran alanı:** Scrollbar’sız Onay Belgesi içeriği erişilebilir olmalı. Test: scrollbar ayarı ve son masaüstü açılışında görsel kontrol.
- **Eksik/eşit tarihler:** İsteğe bağlı boş tarihler geçerli kalmalı, kuralın yasakladığı eşit/ters sıra kaydı durdurmalı. Test: geçerli, eksik, eşit ve ters tarih örnekleri.

---

### Task 1: Komisyon tarih ve sayısını Kurum Bilgisi ekranına taşı

**Dosyalar:**
- Değiştir: `DogruTemin.Wpf/MainWindow.xaml`
- Değiştir: `DogruTemin.Wpf/UiAccessibilityService.cs`
- Test: `DogruTemin.Tests/WorkflowUiRefinementsTests.cs`

**Arayüz:** `AssignmentDatePicker` ve `AssignmentNoBox` adları/değer bağları korunur; görsel alanlar `InstitutionView` içindeki aktif işlem komisyonu alanına alınır. Etiket `Komisyon Onay Sayısı` olur; tarih için erişilebilir ad `Komisyon Onay Tarihi` olur.

- [ ] **1. Adım: Başarısız entegrasyon testini yaz** — test, iki kontrolün `InstitutionView` görsel ağacında bulunduğunu, `GeneralView` altında bulunmadığını ve etiketin tam olarak `Komisyon Onay Sayısı` olduğunu doğrulasın.
- [ ] **2. Adım: Testi çalıştır ve beklenen nedenle başarısız olduğunu gör** — `dotnet test ./DogruTemin.Tests/DogruTemin.Tests.csproj -c Release -p:NuGetAudit=false --filter FullyQualifiedName~WorkflowUiRefinementsTests`.
- [ ] **3. Adım: XAML alanlarını taşı ve erişilebilir adları güncelle** — `ProcessDates.AssignmentDate` ve `AssignmentNo` eşlemelerini değiştirme.
- [ ] **4. Adım: Testi çalıştır ve geçmesini doğrula** — aynı filtre; taşınmış alanlar ve etiket geçmeli.
- [ ] **5. Adım: Değişikliği commit et** — `feat: move commission approval fields to institution`.

### Task 2: Firma telefonunda sabit `(0)` öneki göster

**Dosyalar:**
- Değiştir: `DogruTemin.Wpf/MainWindow.xaml`
- Değiştir: `DogruTemin.Wpf/MainWindow.FirmsProducts.cs`
- Değiştir: `DogruTemin.Wpf/MainWindow.DataHome.cs`
- Test: `DogruTemin.Tests/WorkflowUiRefinementsTests.cs`

**Arayüz:** `PhoneBox` yanında düzenlenemeyen `(0)` görsel öneki gösterilir. Editör metni, kayıtlı numaranın başındaki ulusal sıfır olmadan kalan kısmını gösterir. Alan boş bırakılabilir; girildiğinde rakamlar (boşluk/tire gibi ayraçlar hariç) tam 10 olmalıdır. Kayıtta sabit önek bir kez birleştirilir; ülke kodu ve başka numara biçim kısıtları eklenmez.

- [ ] **1. Adım: Telefon dönüşümleri ve doğrulaması için başarısız testleri yaz** — `03121234567` görüntülemede `3121234567` olur; `3121234567` kayıtta `03121234567` olur; boş değer geçer; 9 ve 11 rakam geçmez; 10 rakam ve ayraçlı 10 rakam geçer; yükle/kaydet döngüsünde fazladan sıfır oluşmaz.
- [ ] **2. Adım: Filtreli testi çalıştır ve eksik dönüşüm nedeniyle başarısız olduğunu gör** — aynı test projesi ve `WorkflowUiRefinementsTests` filtresi.
- [ ] **3. Adım: `internal static string PhoneForEntry(string? storedPhone)`, `internal static string PhoneForStorage(string? enteredPhone)` ve `internal static bool IsValidPhoneEntry(string? enteredPhone)` metotlarını `MainWindow.FirmsProducts.cs` dosyasına ekle ve firma EKLE/KAYDET akışında kullan** — alan boş olabilir; rakam dışı ayraçları saymadan tam 10 rakamı kabul et; XAML’de sabit `(0)` önekini ekle.
- [ ] **4. Adım: Testleri çalıştır ve eski/yeni telefon biçimlerini doğrula** — aynı filtre.
- [ ] **5. Adım: Değişikliği commit et** — `feat: show fixed zero prefix for firm phone`.

### Task 3: Firma ve teklif kolonlarının alt hizasını eşitle

**Dosyalar:**
- Değiştir: `DogruTemin.Wpf/MainWindow.xaml`
- Test: `DogruTemin.Tests/WorkflowUiRefinementsTests.cs`

- [ ] **1. Adım: Kolon yerleşimi için başarısız WPF testi yaz** — ölçüm sonrası `FirmsView` ve `OffersView` alt koordinatlarının en fazla 1 DIP farklı olduğunu kontrol et.
- [ ] **2. Adım: Filtreli testi çalıştır ve mevcut ayrı yükseklik davranışını doğrula** — aynı test projesi ve filtre.
- [ ] **3. Adım: Firma formu/liste ve teklif içeriğini ortak satırda esnetecek XAML düzenini uygula** — gerekirse kaydırmayı yalnız iç listelere bırak; sabit `FirmGrid` yüksekliğini kolon alt sınırını belirleyen tek ölçü olarak kullanma.
- [ ] **4. Adım: Filtreli testi ve görsel açılış kontrolünü çalıştır** — iki alt kenarın eşit hizalandığını ve kontrollerin erişilebilir olduğunu doğrula.
- [ ] **5. Adım: Değişikliği commit et** — `ui: align firms and offers columns`.

### Task 4: ComboBox’ın her noktasına tıklayarak açılmasını sağla

**Dosyalar:**
- Değiştir: `DogruTemin.Wpf/App.xaml`
- Test: `DogruTemin.Tests/WorkflowUiRefinementsTests.cs`

- [ ] **1. Adım: Ortak ComboBox davranışı için başarısız test yaz** — metin/boş alan tıklamasının açılmayı, listeden seçim yapmanın seçimi ve kapanmayı sağladığını doğrula.
- [ ] **2. Adım: Filtreli testi çalıştır ve boş alan tıklamasında açılmadığını doğrula** — aynı test projesi ve filtre.
- [ ] **3. Adım: `PremiumFieldComboBox` şablonundaki ToggleButton’ı tüm seçim yüzeyini kapsayacak şekilde düzenle** — ok sağda kalmalı, içerik görünür olmalı, popup seçimi yeni açılma tetiklememeli.
- [ ] **4. Adım: Filtreli testi çalıştır ve tüm etkileşimleri doğrula** — aynı filtre.
- [ ] **5. Adım: Değişikliği commit et** — `ui: make combo box selection surface clickable`.

### Task 5: Onay Belgesi ekranını scrollbar olmadan sığdır

**Dosyalar:**
- Değiştir: `DogruTemin.Wpf/MainWindow.xaml`
- Test: `DogruTemin.Tests/WorkflowUiRefinementsTests.cs`

- [ ] **1. Adım: Scrollbar durumunu doğrulayan başarısız testi yaz** — `ApprovalDocumentView.VerticalScrollBarVisibility` kapalı olmalı.
- [ ] **2. Adım: Filtreli testi çalıştır ve mevcut `Auto` değerinde başarısız olduğunu gör** — aynı test projesi ve filtre.
- [ ] **3. Adım: `ApprovalDocumentView` iç boşluklarını ve not alanı yüksekliğini kullanılabilir pencereye sığacak şekilde düzenle; scrollbar’ı kapat** — alan veya zorunlu bilgi kaldırma.
- [ ] **4. Adım: Filtreli testi ve uygulama açılışında sayfa görsel kontrolünü çalıştır** — kaydırma çubuğu yok ve son alana erişim var.
- [ ] **5. Adım: Değişikliği commit et** — `ui: fit approval document without scrollbar`.

### Task 6: Onay Belgesi tarihini Tarihler ekranında bilgi olarak göster

**Dosyalar:**
- Değiştir: `DogruTemin.Wpf/MainWindow.xaml`
- Değiştir: `DogruTemin.Wpf/MainWindow.Documents.cs`
- Test: `DogruTemin.Tests/WorkflowUiRefinementsTests.cs`

- [ ] **1. Adım: Bilgi metni için başarısız test yaz** — tarih boşken “Henüz girilmedi”, tarih seçilince `dd.MM.yyyy` gösterilsin; alan salt okunur olsun.
- [ ] **2. Adım: Filtreli testi çalıştır ve bilgi öğesi olmadığı için başarısız olduğunu gör** — aynı test projesi ve filtre.
- [ ] **3. Adım: Tarihler görünümüne bilgi metni ekle ve `AnyDate_Changed` üzerinden değiştikçe güncelle** — ikinci tarih girişi ekleme.
- [ ] **4. Adım: Filtreli testi çalıştır ve boş/dolu durumlarını doğrula** — aynı filtre.
- [ ] **5. Adım: Değişikliği commit et** — `feat: show approval date on dates screen`.

### Task 7: Tarihler ekranına kronoloji denetimli Kaydet düğmesi ekle

**Dosyalar:**
- Değiştir: `DogruTemin.Wpf/MainWindow.xaml`
- Değiştir: `DogruTemin.Wpf/MainWindow.Documents.cs`
- Test: `DogruTemin.Tests/WorkflowUiRefinementsTests.cs`
- Mevcut kurallar: `DogruTemin.Core/Services/DateValidationService.cs`

**Arayüz:** `DatesView` içinde Kaydet düğmesi bulunur. `SaveDates_Click` mevcut UI verisini alır, `DateValidationService.RequiredSequenceIssues(dates, ValidationSeverity.Critical)` ile denetler; hata varsa mesajları Türkçe gösterip `Save_Click` çağırmadan durur. Hata yoksa ortak `Save_Click` akışına devam eder.

- [ ] **1. Adım: Kaydet kapısı için başarısız test yaz** — kuralı ihlal eden tarih dizisinde Kaydet eylemi engellensin; geçerli ve eksik tarih içeren diziler geçsin.
- [ ] **2. Adım: Filtreli testi çalıştır ve tarihler ekranında Kaydet kapısı bulunmadığı için başarısız olduğunu gör** — aynı test projesi ve filtre.
- [ ] **3. Adım: `internal static IReadOnlyList<ValidationIssue> GetDateSaveBlockingIssues(ProcessDates dates)` ve `SaveDates_Click(object, RoutedEventArgs)` ekle** — doğrulayıcı `DateValidationService.RequiredSequenceIssues(dates, ValidationSeverity.Critical)` çağrısını sarmalar. Hatalarda ortak kaydetme akışını çağırma; başarılı durumda mevcut `Save_Click` yolunu çağır.
- [ ] **4. Adım: Filtreli testleri ve tarih doğrulama testlerini çalıştır** — `dotnet test ./DogruTemin.Tests/DogruTemin.Tests.csproj -c Release -p:NuGetAudit=false --filter FullyQualifiedName~ValidationTests`.
- [ ] **5. Adım: Değişikliği commit et** — `feat: add chronology-checked date save`.

### Task 8: Tüm çıktı belgelerini varsayılan seçili yap

**Dosyalar:**
- Değiştir: `DogruTemin.Wpf/MainWindow.Shell.cs`
- Test: `DogruTemin.Tests/WorkflowUiRefinementsTests.cs`

- [ ] **1. Adım: Başlangıç seçimi için başarısız test yaz** — `SeedOutputDocuments` sonrasında altı belgenin hepsi seçili olsun; birini kaldırıp Tarihler/Çıktı alt görünümleri arasında geçiş yaptığında seçim korunmalı.
- [ ] **2. Adım: Filtreli testi çalıştır ve ön hazırlık belgelerinin seçili olmaması nedeniyle başarısız olduğunu gör** — aynı test projesi ve filtre.
- [ ] **3. Adım: `SeedOutputDocuments` içindeki altı `OutputDocument.IsSelected` varsayılanını `true` yap** — kullanıcı seçimlerini sekme değişiminde yeniden tohumlama.
- [ ] **4. Adım: Filtreli testi çalıştır ve başlangıç/toggle/geçiş sonuçlarını doğrula** — aynı filtre.
- [ ] **5. Adım: Değişikliği commit et** — `feat: select all output documents by default`.

### Task 9: Tam doğrulama ve yayın EXE

**Dosyalar:**
- Yayın çıktısı: `_YAYIN/Kontrol-2026-09-27-UI-DUZENLEMELERI/`

- [ ] **1. Adım: Tüm Release testlerini çalıştır** — `dotnet test ./DogruTemin.Tests/DogruTemin.Tests.csproj -c Release -p:NuGetAudit=false`; beklenen sonuç 0 başarısız test.
- [ ] **2. Adım: Windows x64 tek dosya EXE’sini yayınla** — `dotnet publish ./DogruTemin.Wpf/DogruTemin.Wpf.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:NuGetAudit=false -o ./_YAYIN/Kontrol-2026-09-27-UI-DUZENLEMELERI`.
- [ ] **3. Adım: EXE’yi başlat ve yanıt verdiğini doğrula** — yeni pencerenin başlığı `DoğruTemin` olmalı; bu sürüm kullanıcı tarafından elle ekran akışlarında kontrol edilebilmeli.
- [ ] **4. Adım: Son durum ve dalı kontrol et** — `git status --short --branch` ve `git branch --list`; yalnız `main` kalmalı.

---

## Öz inceleme

- Tasarımın sekiz kararının her biri ayrı bir görev ve kabul kontrolüyle eşleşiyor; dokuzuncu görev paket doğrulaması ve EXE üretimi.
- Plan, önce test sonra minimal uygulama adımlarını her davranış değişikliği için içeriyor.
- Telefon dönüşümü/doğrulama metotları, parametreleri ve dönüş tipleri net biçimde tanımlandı; boş ve ayraçlı giriş davranışları belirlendi.
- Tarih kaydetme testi ile arayüz handler’ı aynı `GetDateSaveBlockingIssues(ProcessDates)` saf doğrulayıcısını kullanıyor.
- Tarih kaydetme mevcut şemayı veya kronoloji kurallarını değiştirmiyor; `Critical` hatalar kaydetmeyi durduruyor.
- Görsel sığma ve alt hizalama, otomatik testin yanında açılan WPF penceresinde görsel kontrol gerektiriyor.
- Her görevin commit adımı yalnız o görevin test/kod dosyalarını ekleyerek önceki yerel değişiklikleri yanlışlıkla kapsamamalı.

