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

        // Commentaire pédagogique :
        // - `MauiApp.CreateBuilder()` initialise le pipeline MAUI et la collection de services DI.
        // - Ici nous enregistrons les services, ViewModels et Pages utilisés par l'application.
        // - `UseMauiCommunityToolkit()` active les helpers du CommunityToolkit (animations, converters, behaviours).
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

        // ── Services (La Couche Logique & Accès aux données) ──────────────────
        // 🎓 Pédagogie PFE : Pourquoi "AddSingleton" ?
        // Un "Singleton" signifie que l'application ne créera qu'une seule instance de ce service 
        // pendant toute sa durée de vie. C'est crucial pour SupabaseService car on veut 
        // garder la même connexion à la base de données et la même session utilisateur partout.
        builder.Services.AddSingleton<SupabaseService>();
        builder.Services.AddSingleton<IRfidService, RfidService>();
        builder.Services.AddSingleton<ExcelService>();

        // ── ViewModels (Le Cerveau Spécifique de chaque Page) ────────────────
        // 🎓 Pédagogie PFE : Pourquoi "AddTransient" ?
        // Un "Transient" signifie qu'à chaque fois qu'on a besoin de l'objet, l'application crée 
        // une *nouvelle instance* vierge. Cela évite, par exemple, qu'un formulaire de saisie
        // garde les anciennes données stockées en mémoire la prochaine fois qu'on ouvre la page.
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<ScanViewModel>();
        builder.Services.AddTransient<MachineDetailViewModel>();
        builder.Services.AddTransient<MachineListViewModel>();
        builder.Services.AddTransient<MaintenanceViewModel>();
        builder.Services.AddTransient<MaintenanceSessionViewModel>();
        builder.Services.AddTransient<AdminViewModel>();
        builder.Services.AddTransient<ReportViewModel>();
        builder.Services.AddTransient<UserManagementViewModel>();
        builder.Services.AddTransient<DepartmentViewModel>();

        // ── Pages (Les Vues Graphiques XAML) ──────────────────────────────────
        // 🎓 Pédagogie PFE : Injection de Dépendances
        // C'est ce mécanisme central (Dependency Injection) qui permet à MAUI de lier automatiquement
        // `LoginPage` à `LoginViewModel` et à `SupabaseService` sans qu'on ait besoin de faire `new ...()`.
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<ScanPage>();
        builder.Services.AddTransient<MachineDetailPage>();
        builder.Services.AddTransient<MachineListPage>();
        builder.Services.AddTransient<MaintenancePage>();
        builder.Services.AddTransient<MaintenanceSessionPage>();
        builder.Services.AddTransient<AdminPage>();
        builder.Services.AddTransient<ReportPage>();
        builder.Services.AddTransient<UserManagementPage>();
        builder.Services.AddTransient<DepartmentPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
