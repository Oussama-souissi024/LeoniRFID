using CommunityToolkit.Maui;
using LeoniRFID.Helpers;
using LeoniRFID.Services;
using LeoniRFID.ViewModels;
using LeoniRFID.Views;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Http;

namespace LeoniRFID;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("Roboto-Regular.ttf", "RobotoRegular");
                fonts.AddFont("Roboto-Bold.ttf", "RobotoBold");
                fonts.AddFont("Roboto-Medium.ttf", "RobotoMedium");
            });

        // ── Services ──────────────────────────────────────────────────────────
        builder.Services.AddSingleton<DatabaseService>();
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<IRfidService, RfidService>();
        builder.Services.AddSingleton<SyncService>();
        builder.Services.AddSingleton<ExcelService>();
        builder.Services.AddHttpClient<ApiService>(client =>
        {
            client.BaseAddress = new Uri(Constants.ApiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        // ── ViewModels ────────────────────────────────────────────────────────
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<ScanViewModel>();
        builder.Services.AddTransient<MachineDetailViewModel>();
        builder.Services.AddTransient<AdminViewModel>();
        builder.Services.AddTransient<ReportViewModel>();

        // ── Pages ─────────────────────────────────────────────────────────────
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<ScanPage>();
        builder.Services.AddTransient<MachineDetailPage>();
        builder.Services.AddTransient<AdminPage>();
        builder.Services.AddTransient<ReportPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
