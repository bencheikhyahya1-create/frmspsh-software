using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ParaAthleticsResults.Data;
using ParaAthleticsResults.Services;
using ParaAthleticsResults.Services.Localization;
using ParaAthleticsResults.Services.Theme;
using ParaAthleticsResults.ViewModels;
using ParaAthleticsResults.Views;

namespace ParaAthleticsResults;

public partial class App : Application
{
    private IHost? _host;
    private bool _screenshotMode;

    public static IServiceProvider Services => ((App)Current)._host!.Services;

    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        _screenshotMode = e.Args.Contains("--screenshots", StringComparer.OrdinalIgnoreCase);

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                services.AddDataServices(useInMemory: _screenshotMode);
                services.AddApplicationServices();
                services.AddSingleton<NavigationService>();
                services.AddSingleton<MainViewModel>();
                services.AddTransient<LoginViewModel>();
                services.AddTransient<DashboardViewModel>();
                services.AddTransient<AthletesViewModel>();
                services.AddTransient<EventsViewModel>();
                services.AddTransient<StartListViewModel>();
                services.AddTransient<ResultsViewModel>();
                services.AddTransient<LiveResultsViewModel>();
                services.AddTransient<SettingsViewModel>();
                services.AddTransient<UsersViewModel>();
                services.AddTransient<ReportsViewModel>();
                services.AddTransient<ScoreboardViewModel>();
            })
            .Build();

        await _host.StartAsync();
        await Services.EnsureDatabaseCreatedAsync();

        if (_screenshotMode)
            await DatabaseSeeder.SeedDemoDataAsync(Services);

        var localization = Services.GetRequiredService<Core.Interfaces.ILocalizationService>() as LocalizationService;
        await localization!.InitializeAsync();

        var theme = Services.GetRequiredService<Core.Interfaces.IThemeService>() as ThemeService;
        await theme!.InitializeAsync();
        ApplyTheme(theme.CurrentTheme);
        theme.ThemeChanged += (_, _) => ApplyTheme(theme.CurrentTheme);

        if (_screenshotMode)
        {
            await ScreenshotRunner.RunAsync(Services);
            return;
        }

        var auth = Services.GetRequiredService<Core.Interfaces.IAuthenticationService>();
        await auth.SeedDefaultUsersAsync();

        var login = new LoginWindow
        {
            DataContext = Services.GetRequiredService<LoginViewModel>()
        };
        login.Show();
        ShutdownMode = ShutdownMode.OnLastWindowClose;
    }

    private void ApplyTheme(Core.Enums.AppTheme theme)
    {
        var dict = new ResourceDictionary
        {
            Source = new Uri(theme == Core.Enums.AppTheme.Dark
                ? "Themes/DarkTheme.xaml"
                : "Themes/LightTheme.xaml", UriKind.Relative)
        };

        if (Resources.MergedDictionaries.Count >= 2)
            Resources.MergedDictionaries.RemoveAt(1);
        Resources.MergedDictionaries.Insert(1, dict);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        base.OnExit(e);
    }
}
