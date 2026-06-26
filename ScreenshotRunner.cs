using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using ParaAthleticsResults.ViewModels;
using ParaAthleticsResults.Views;

namespace ParaAthleticsResults;

/// <summary>
/// Automated screenshot capture for preview generation.
/// Usage: ParaAthleticsResults.exe --screenshots
/// </summary>
public static class ScreenshotRunner
{
    private static readonly string OutputDir = Path.Combine(
        Directory.GetCurrentDirectory(), "previews");

    public static async Task RunAsync(IServiceProvider services)
    {
        try
        {
            Directory.CreateDirectory(OutputDir);

            var auth = services.GetRequiredService<Core.Interfaces.IAuthenticationService>();
            await auth.SeedDefaultUsersAsync();
            var login = await auth.LoginAsync("admin", "Admin@2024");
            if (!login.Success)
            {
                Console.WriteLine($"Login failed: {login.ErrorMessage}");
                return;
            }

            var loginVm = services.GetRequiredService<LoginViewModel>();
            var loginWin = new LoginWindow { DataContext = loginVm };
            loginWin.Show();
            await Task.Delay(800);
            CaptureWindow(loginWin, "01-login.png");
            loginWin.Close();

            var mainVm = services.GetRequiredService<MainViewModel>();
            mainVm.SetUser(login.FullName ?? "Admin");
            mainVm.Initialize();
            var mainWin = new MainWindow { DataContext = mainVm };
            mainWin.Show();
            mainWin.WindowState = WindowState.Normal;
            mainWin.Width = 1600;
            mainWin.Height = 900;
            await Task.Delay(2000);

            var captures = new (string name, Action navigate)[]
            {
                ("02-dashboard.png", () => mainVm.NavigateDashboardCommand.Execute(null)),
                ("03-athletes.png", () => mainVm.NavigateAthletesCommand.Execute(null)),
                ("04-events.png", () => mainVm.NavigateEventsCommand.Execute(null)),
                ("05-startlist.png", () => mainVm.NavigateStartListCommand.Execute(null)),
                ("06-results.png", () => mainVm.NavigateResultsCommand.Execute(null)),
                ("08-settings.png", () => mainVm.NavigateSettingsCommand.Execute(null)),
            };

            foreach (var (file, navigate) in captures)
            {
                navigate();
                await Task.Delay(2000);
                CaptureWindow(mainWin, file);
            }

            var scoreboardVm = services.GetRequiredService<ScoreboardViewModel>();
            var scoreboard = new ScoreboardWindow
            {
                DataContext = scoreboardVm,
                WindowState = WindowState.Normal,
                Width = 1600,
                Height = 900
            };
            scoreboard.Show();
            await Task.Delay(2000);
            CaptureWindow(scoreboard, "07-scoreboard.png");
            scoreboard.Close();

            mainWin.Close();
            Console.WriteLine($"Screenshots saved to: {Path.GetFullPath(OutputDir)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Screenshot error: {ex}");
            File.WriteAllText(Path.Combine(OutputDir, "error.log"), ex.ToString());
        }
        finally
        {
            Application.Current.Shutdown();
        }
    }

    private static void CaptureWindow(Window window, string fileName)
    {
        window.UpdateLayout();
        var width = (int)window.ActualWidth;
        var height = (int)window.ActualHeight;
        if (width <= 0 || height <= 0) return;

        var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(window);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(rtb));

        var path = Path.Combine(OutputDir, fileName);
        using var stream = File.Create(path);
        encoder.Save(stream);
        Console.WriteLine($"Captured: {fileName}");
    }
}
