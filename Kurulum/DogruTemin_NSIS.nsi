Unicode true

!include "MUI2.nsh"
!include "x64.nsh"
!include "LogicLib.nsh"
!include "WinVer.nsh"
!include "WordFunc.nsh"

!insertmacro VersionCompare

!define APPNAME "DoğruTemin"
!define APPVERSION "4.2.0"
!define APPEXENAME "DogruTemin.exe"
!define APPDESC "Doğrudan Temin Süreç Yönetim Programı"
!define APPKEY "Software\DoğruTemin"
!define UNINSTKEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\DogruTemin.Desktop.4B5374E9-9DB8-4B4F-93D6-57B0BA5704C4"

Name "${APPNAME} ${APPVERSION}"
OutFile "..\_YAYIN\Setup\DogruTemin_Setup_4.2.0_x64.exe"
InstallDir "$PROGRAMFILES64\DoğruTemin"
InstallDirRegKey HKLM "${APPKEY}" "InstallDir"
RequestExecutionLevel admin
CRCCheck force
SetCompressor /SOLID lzma
SetCompressorDictSize 32
ShowInstDetails show
ShowUninstDetails show
BrandingText "DoğruTemin ${APPVERSION}"

VIProductVersion "4.2.0.0"
VIAddVersionKey /LANG=1055 "ProductName" "DoğruTemin"
VIAddVersionKey /LANG=1055 "FileDescription" "DoğruTemin Kurulum Programı"
VIAddVersionKey /LANG=1055 "FileVersion" "4.2.0.0"
VIAddVersionKey /LANG=1055 "ProductVersion" "4.2.0"
VIAddVersionKey /LANG=1055 "CompanyName" "DoğruTemin"
VIAddVersionKey /LANG=1055 "LegalCopyright" "DoğruTemin"

!define MUI_ICON "..\DogruTemin.Wpf\Assets\AppIcon.ico"
!define MUI_UNICON "..\DogruTemin.Wpf\Assets\AppIcon.ico"
!define MUI_ABORTWARNING
!define MUI_WELCOMEPAGE_TITLE "DoğruTemin 4.2.0 Kurulumuna Hoş Geldiniz"
!define MUI_WELCOMEPAGE_TEXT "DoğruTemin Windows 10/11 64-bit için self-contained hazırlanmıştır. Hedef bilgisayarda .NET SDK, .NET Runtime, Visual Studio veya Excel gerekmez.$\r$\n$\r$\nMevcut DoğruTemin kullanıcı verileri ve arşivleri korunur. Kurulumdan önce açık DoğruTemin pencerelerini kapatmanız önerilir."
!define MUI_FINISHPAGE_TITLE "DoğruTemin Kurulumu Tamamlandı"
!define MUI_FINISHPAGE_TEXT "DoğruTemin başarıyla kuruldu.$\r$\n$\r$\nKullanıcı verileriniz Masaüstü\DoğruTemin klasöründe tutulur ve program kaldırıldığında silinmez."
!define MUI_FINISHPAGE_RUN "$INSTDIR\${APPEXENAME}"
!define MUI_FINISHPAGE_RUN_TEXT "DoğruTemin'i şimdi çalıştır"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH

!insertmacro MUI_LANGUAGE "Turkish"

Function .onInit
  ${IfNot} ${AtLeastWin10}
    MessageBox MB_ICONSTOP|MB_OK "DoğruTemin yalnızca Windows 10/11 64-bit bilgisayarlarda desteklenir."
    Abort
  ${EndIf}

  ${IfNot} ${RunningX64}
    MessageBox MB_ICONSTOP|MB_OK "DoğruTemin yalnızca Windows 10/11 64-bit bilgisayarlarda çalışır."
    Abort
  ${EndIf}

  SetRegView 64
  ReadRegStr $0 HKLM "${UNINSTKEY}" "DisplayVersion"

  ; Daha yeni bir sürüm zaten kuruluysa yanlışlıkla geri sürüme dönmeyi engelle.
  ${If} $0 != ""
    ${VersionCompare} "$0" "${APPVERSION}" $1
    ${If} $1 == 1
      MessageBox MB_ICONSTOP|MB_OK "Bu bilgisayarda DoğruTemin $0 sürümü zaten kurulu.$\r$\n$\r$\nKurmak istediğiniz ${APPVERSION} sürümü daha eskidir. Veri ve program uyumluluğunu korumak için kurulum durduruldu."
      Abort
    ${EndIf}
  ${EndIf}
FunctionEnd

Function CloseDogruTemin
  ; Önceki sürüm çalışıyorsa sessizce kapat ve Windows'un DLL kilitlerini bırakması için bekle.
  nsExec::ExecToStack '"$SYSDIR\taskkill.exe" /IM "${APPEXENAME}" /T /F'
  Pop $0
  Pop $1
  Sleep 2500
FunctionEnd

Function CheckInstallEnvironment
  ; Program Files hedef sürücüsünde en az 500 MB boş alan iste.
  ; PowerShell Windows 10/11'in yerleşik bileşenidir. Kontrol çalışamazsa kurulum devam eder;
  ; yalnız gerçekten yetersiz alan saptanırsa durdurulur.
  nsExec::ExecToStack `"$SYSDIR\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -ExecutionPolicy Bypass -Command "if((New-Object System.IO.DriveInfo ([System.IO.Path]::GetPathRoot('$INSTDIR'))).AvailableFreeSpace -ge 524288000){exit 0}else{exit 2}"`
  Pop $0
  Pop $1
  ${If} $0 == 2
    MessageBox MB_ICONSTOP|MB_OK "Kurulum için yeterli boş disk alanı yok.$\r$\n$\r$\nDoğruTemin kurulumu için hedef sürücüde en az 500 MB boş alan bırakın ve kurulumu yeniden çalıştırın."
    Abort
  ${EndIf}

  ; Yönetici yetkisi alınmış olsa bile Program Files hedefinin gerçekten yazılabildiğini doğrula.
  CreateDirectory "$INSTDIR"
  ClearErrors
  FileOpen $2 "$INSTDIR\.dogrutemin_write_test.tmp" w
  IfErrors write_test_failed
  FileWrite $2 "DoğruTemin kurulum yazma testi"
  FileClose $2
  Delete "$INSTDIR\.dogrutemin_write_test.tmp"
  Return

write_test_failed:
  MessageBox MB_ICONSTOP|MB_OK "Kurulum klasörüne yazılamıyor:$\r$\n$INSTDIR$\r$\n$\r$\nKurulumu yönetici yetkisiyle yeniden başlatın veya kurumunuzun BT yöneticisine başvurun."
  Abort
FunctionEnd

Section "DoğruTemin" SEC_MAIN
  SetShellVarContext all
  SetRegView 64

  ; Kurulum ortamını önceden doğrula.
  Call CheckInstallEnvironment

  ; Güncellemede çalışan eski sürümün dosya kilitlerini güvenli biçimde kaldır.
  Call CloseDogruTemin

  ; Kullanıcı verileri Masaüstü\DoğruTemin altında olduğundan yalnız Program Files içindeki
  ; eski uygulama dosyaları temizlenir. Bu, eski/stale DLL kalmasını da engeller.
  IfFileExists "$INSTDIR\*.*" 0 clean_install_done

  ClearErrors
  RMDir /r "$INSTDIR"
  IfErrors 0 clean_install_done

  MessageBox MB_RETRYCANCEL|MB_ICONEXCLAMATION "DoğruTemin'in önceki sürümüne ait bazı dosyalar halen Windows tarafından kullanılıyor.$\r$\n$\r$\nAçık DoğruTemin pencerelerini kapatın ve Yeniden Dene düğmesine basın." IDRETRY retry_clean_install IDCANCEL cancel_install

retry_clean_install:
  Call CloseDogruTemin
  ClearErrors
  RMDir /r "$INSTDIR"
  IfErrors 0 clean_install_done

  MessageBox MB_ICONSTOP|MB_OK "Önceki sürümün dosya kilidi kaldırılamadı.$\r$\n$\r$\nBilgisayarı yeniden başlatın ve kurulumu yeniden çalıştırın. Kullanıcı verileriniz korunur."
  Abort

cancel_install:
  Abort

clean_install_done:
  CreateDirectory "$INSTDIR"
  SetOutPath "$INSTDIR"
  SetOverwrite on
  File /r "..\_YAYIN\Uygulama\*.*"

  WriteUninstaller "$INSTDIR\Uninstall.exe"

  CreateDirectory "$SMPROGRAMS\DoğruTemin"
  CreateShortcut "$SMPROGRAMS\DoğruTemin\DoğruTemin.lnk" "$INSTDIR\${APPEXENAME}" "" "$INSTDIR\${APPEXENAME}" 0
  CreateShortcut "$SMPROGRAMS\DoğruTemin\DoğruTemin'i Kaldır.lnk" "$INSTDIR\Uninstall.exe"
  CreateShortcut "$DESKTOP\DoğruTemin.lnk" "$INSTDIR\${APPEXENAME}" "" "$INSTDIR\${APPEXENAME}" 0

  WriteRegStr HKLM "${APPKEY}" "InstallDir" "$INSTDIR"
  WriteRegStr HKLM "${APPKEY}" "Version" "${APPVERSION}"
  WriteRegStr HKLM "${UNINSTKEY}" "DisplayName" "DoğruTemin ${APPVERSION}"
  WriteRegStr HKLM "${UNINSTKEY}" "DisplayVersion" "${APPVERSION}"
  WriteRegStr HKLM "${UNINSTKEY}" "Publisher" "DoğruTemin"
  WriteRegStr HKLM "${UNINSTKEY}" "DisplayIcon" "$INSTDIR\${APPEXENAME}"
  WriteRegStr HKLM "${UNINSTKEY}" "InstallLocation" "$INSTDIR"
  WriteRegStr HKLM "${UNINSTKEY}" "UninstallString" '"$INSTDIR\Uninstall.exe"'
  WriteRegStr HKLM "${UNINSTKEY}" "QuietUninstallString" '"$INSTDIR\Uninstall.exe" /S'
  WriteRegDWORD HKLM "${UNINSTKEY}" "NoModify" 1
  WriteRegDWORD HKLM "${UNINSTKEY}" "NoRepair" 1
SectionEnd

Section "Uninstall"
  SetShellVarContext all
  SetRegView 64

  nsExec::ExecToStack '"$SYSDIR\taskkill.exe" /IM "${APPEXENAME}" /T /F'
  Pop $0
  Pop $1
  Sleep 2500

  Delete "$DESKTOP\DoğruTemin.lnk"
  Delete "$SMPROGRAMS\DoğruTemin\DoğruTemin.lnk"
  Delete "$SMPROGRAMS\DoğruTemin\DoğruTemin'i Kaldır.lnk"
  RMDir "$SMPROGRAMS\DoğruTemin"

  ; Yalnız uygulama kurulum klasörü silinir.
  ; Masaüstü\DoğruTemin içindeki arşiv, ayar, PDF ve yedekler BİLİNÇLİ olarak korunur.
  RMDir /r "$INSTDIR"

  DeleteRegKey HKLM "${UNINSTKEY}"
  DeleteRegKey HKLM "${APPKEY}"
SectionEnd
