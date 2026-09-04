using DogruTemin.Core.Services;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace DogruTemin.Wpf;

public partial class App : Application
{
    private const string SingleInstanceMutexName = @"Local\DogruTemin.Desktop.SingleInstance.4B5374E9-9DB8-4B4F-93D6-57B0BA5704C4";
    private static Mutex? _singleInstanceMutex;
    private static bool _ownsSingleInstanceMutex;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SetCurrentProcessExplicitAppUserModelID(string appID);

    public App()
    {
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        try
        {
            if (!TryAcquireSingleInstance())
            {
                MessageBox.Show(
                    "DoğruTemin zaten açık. Aynı anda ikinci bir DoğruTemin penceresi çalıştırılmaz.",
                    "DoğruTemin",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                Shutdown(0);
                return;
            }

            try
            {
                if (SetCurrentProcessExplicitAppUserModelID("DogruTemin.Wpf.App") != 0)
                {
                    // Shell kimliği atanamazsa Windows yürütülebilir dosya kimliğine güvenli biçimde geri döner.
                }
            }
            catch { }

            var splash = new SplashWindow();
            splash.Show();
        }
        catch (Exception ex)
        {
            ShowFatalError("Program açılışı sırasında hata oluştu.", ex);
            Shutdown(-1);
        }
    }

    private static bool TryAcquireSingleInstance()
    {
        try
        {
            _singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out var createdNew);
            _ownsSingleInstanceMutex = createdNew;
            if (!createdNew)
            {
                _singleInstanceMutex.Dispose();
                _singleInstanceMutex = null;
            }
            return createdNew;
        }
        catch
        {
            // Nadir bir Windows oturum/izin sorunu programı tamamen kullanılamaz hale getirmesin.
            _singleInstanceMutex = null;
            _ownsSingleInstanceMutex = false;
            return true;
        }
    }

    internal static void ReleaseSingleInstanceForRestart()
    {
        ReleaseSingleInstanceMutex();
    }

    private static void ReleaseSingleInstanceMutex()
    {
        if (_singleInstanceMutex is null) return;
        try
        {
            if (_ownsSingleInstanceMutex)
                _singleInstanceMutex.ReleaseMutex();
        }
        catch { }
        finally
        {
            _ownsSingleInstanceMutex = false;
            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        ReleaseSingleInstanceMutex();
        base.OnExit(e);
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ShowFatalError("DoğruTemin beklenmeyen bir hatayla karşılaştı.", e.Exception);
        e.Handled = true;
        Shutdown(-1);
    }

    private void CurrentDomain_UnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            WriteCrashLog(ex);
    }

    private static void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        WriteCrashLog(e.Exception);
        e.SetObserved();
    }

    private static void ShowFatalError(string title, Exception ex)
    {
        var path = WriteCrashLog(ex);
        MessageBox.Show(
            title + "\n\nTeknik ayrıntılar güvenlik amacıyla ekranda gösterilmez. Hata günlüğü:\n" + path,
            "DoğruTemin - Hata",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    internal static string WriteCrashLog(Exception ex)
    {
        string path;
        try
        {
            var logFolder = new StorageService().LogFolder;
            Directory.CreateDirectory(logFolder);
            path = Path.Combine(logFolder, "DogruTemin_Hata.log");
            var version = AppVersionInfo.InformationalVersion;
            File.AppendAllText(path,
                $"\n===== {DateTime.Now:dd.MM.yyyy HH:mm:ss} =====\n" +
                $"Sürüm: {version}\nİşletim Sistemi: {Environment.OSVersion}\n.NET: {Environment.Version}\n64-bit: {Environment.Is64BitProcess}\n\n{ex}\n",
                Encoding.UTF8);
        }
        catch
        {
            path = Path.Combine(Path.GetTempPath(), "DogruTemin_Hata.log");
            try
            {
                var version = AppVersionInfo.InformationalVersion;
                File.AppendAllText(path,
                    $"\n===== {DateTime.Now:dd.MM.yyyy HH:mm:ss} =====\nSürüm: {version}\n.NET: {Environment.Version}\n\n{ex}\n",
                    Encoding.UTF8);
            }
            catch { }
        }
        return path;
    }
}
