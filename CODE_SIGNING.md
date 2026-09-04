# DoğruTemin Windows Kod İmzalama

DoğruTemin final dağıtım paketinde Authenticode kod imzası zorunludur. Depoya sertifika, PFX dosyası, özel anahtar veya parola **asla** eklenmez.

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

`-AllowUnsignedFinal` yalnız GitHub Actions veya yerel smoke testi içindir. Bu seçenekle üretilen imzasız paket dağıtım paketi olarak kullanılmamalıdır.

## RC/test sürümleri

`VersionSuffix` doluyken normal final Setup üretilmez. `-AllowPrerelease` yalnız RC/CI kurulum testleri için kullanılabilir.
