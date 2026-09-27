# DoğruTemin Windows Kod İmzalama

DoğruTemin final dağıtımlarında Authenticode kod imzası kullanılır; yalnız aşağıda açıklanan 4.3.0 sürümü açık dağıtım kararıyla bu zorunluluktan muaftır. Depoya sertifika, PFX dosyası, özel anahtar veya parola **asla** eklenmez.

## Üretim kullanımı

1. Windows kod imzalama sertifikasını bilgisayarın sertifika deposuna güvenli biçimde yükleyin.
2. Sertifikanın SHA-1 thumbprint değerini yalnız üretim ortamında `DOGRUTEMIN_SIGN_CERT_SHA1` ortam değişkenine verin.
3. `SETUP_HAZIRLA.ps1` veya `SETUP_HAZIRLA.bat` çalıştırın.
4. Araç önce yayınlanan `DogruTemin.exe` dosyasını, ardından Setup EXE'yi SHA-256 ile imzalar ve `signtool verify /pa` ile doğrular.
5. Zaman damgası varsayılan olarak `http://timestamp.digicert.com` üzerinden SHA-256 ile alınır; gerekirse `-TimestampUrl` ile değiştirilebilir.

```powershell
$env:DOGRUTEMIN_SIGN_CERT_SHA1 = '<sertifika-thumbprint>'
.\SETUP_HAZIRLA.ps1
```

## Final güvenlik kilidi

`Directory.Build.props` içindeki `VersionSuffix` boşsa sürüm final kabul edilir. Final sürümde sertifika tanımlı değilse Setup üretimi durur.

Normal final üretiminde sertifika yoksa Setup oluşturulmaz. 4.3.0 için `-AllowUnsignedFinal` seçeneği yalnız `exe_olustur\EXE_OLUSTUR.bat` tarafından kullanılır; başka final sürümlerinde bu seçenek sürüm koruması nedeniyle üretimi durdurur.

## RC/test sürümleri

`VersionSuffix` doluyken normal final Setup üretilmez. `-AllowPrerelease` yalnız RC/CI kurulum testleri için kullanılabilir.

## 4.3.0 imzasız dağıtım istisnası

Bakım sorumlusunun açık kararıyla 4.3.0 sürümü Authenticode sertifikası olmadan dağıtılabilir. Windows SmartScreen / "Bilinmeyen yayıncı" uyarısı beklenebilir. Bu istisna yalnız 4.3.0 sürümünü kapsar; sonraki final sürümlerde yukarıdaki imzalama zorunluluğu geçerlidir. 4.3.0 paketini `exe_olustur\EXE_OLUSTUR.bat` üretir ve pakete SHA-256 doğrulama dosyası ekler.
