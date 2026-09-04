# DoğruTemin 4.2.0 RC1 kalite turu

- DoğruFatura'daki premium bilgi kartı görsel dili ve keskin metin kuralları DoğruTemin'e aktarıldı.
- Metin taşıyan kartlarda DropShadow kaldırıldı; ClearType + Display metin işleme ortaklaştırıldı.
- Premium giriş alanları, buton ölçüleri, tipografi, köşe ve kontrast kuralları DoğruFatura ile eşitlendi.
- Tarih alanlarına görünür temizle (×) düğmesi ve Delete/Backspace ile silme desteği eklendi.
- Splash dolum çubuğu korunarak sahte zamanlayıcı kaldırıldı; ilerleme gerçek açılış kilometre taşlarına bağlandı.
- Pencere yeniden boyutlandırılabilir hale getirildi; Viewbox yalnız küçültme için kullanılır.
- DataGrid sanallaştırma ve boş durum mesajları eklendi.
- Merkezi sürüm kaynağı, otomatik testler, GitHub Actions ve Setup smoke hattı eklendi.
- Veri kökü Masaüstü\DoğruTemin olarak standardize edildi; eski Belgeler verileri silmeden ve overwrite etmeden migrate edilir.
- Beklenmeyen kapanma sonrası koruyucu yedek ve gizlilik odaklı tanılama paketi eklendi.
- Rutin başarı mesajlarının bir bölümü DoğruFatura tarzı keskin, non-modal bildirimlere dönüştürüldü; kritik hata ve onaylar modal bırakıldı.
- Depolama katmanına test edilebilir kök/legacy yol desteği eklendi; veri taşıma ve overwrite-etmeme davranışı otomatik teste bağlandı.
- Merkezi sürüm, hesap motoru, validasyon, sayı ayrıştırma, responsive/DPI ve depolama senaryoları için xUnit test kapsamı oluşturuldu.
- `TEST_AC.bat` ile RC1 kullanıcı bilgisayarında tek tıklamayla Release çalıştırma yolu eklendi.
- Sessiz Setup smoke testinde etkileşimli SmartScreen bilgilendirmesi atlanır; normal kullanıcı kurulumunda bilgilendirme korunur.
- CI işlerine 20 dakikalık üst süre sınırı ve güncellenmiş eşzamanlılık grubu eklendi; takılan eski koşular yeni doğrulamayı bloke etmez.
