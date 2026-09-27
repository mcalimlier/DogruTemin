# DoğruTemin EXE oluşturma

Yeni Windows x64 kurulum dosyasını oluşturmak için `EXE_OLUSTUR.bat` dosyasına çift tıklayın.

Betik Release derlemesini ve otomatik testleri çalıştırır, self-contained uygulama dosyalarını hazırlar, ardından NSIS ile Setup EXE üretir. Hedef bilgisayarda .NET Runtime gerekmez. Bu 4.3.0 dağıtımı sertifikasız ve imzasızdır; Windows SmartScreen "Bilinmeyen yayıncı" uyarısı gösterebilir.

Üretilen dosyalar `DAGITIM` klasöründe görünür:

- `DogruTemin_Setup_4.3.0_x64.exe`
- `DogruTemin_Setup_4.3.0_x64_SHA256.txt`
- Kurulum notu, kullanıcı kılavuzu, yayın notları ve SmartScreen bilgilendirmesi

Kurulum dosyasını yalnız doğrulanmış dağıtım kaynağından alın. Paket içindeki SHA-256 değerini doğrulayın ve Windows güvenlik özelliklerini kapatmayın. Üretim için .NET 8 SDK ve NSIS kurulu olmalıdır.
