using LeoniRFID.Services;
using LeoniRFID.ViewModels;
using LeoniRFID.Views;

namespace LeoniRFID;

public partial class AppShell : Shell
{
    private readonly IServiceProvider _services;

    public AppShell(IServiceProvider services)
    {
        _services = services;

        try
        {
            // Initialise le XAML de la Shell (peut lancer une exception si un resource/contrôle est manquant)
            // 🎓 Pédagogie PFE : Initialisation des composants graphiques
            // `InitializeComponent` charge le fichier `AppShell.xaml` et construit l'interface.
            // Attention : Si une image (comme une icône du menu) ou un style (StaticResource) 
            // est manquant dans le XAML, c'est ici que l'application plantera (Exception).
            InitializeComponent();

            // 🎓 Pédagogie PFE : Routage Absolu vs Relatif
            // Le `RegisterRoute` sert à enregistrer des pages qui ne sont PAS directement 
            // déclarées dans le menu latéral (AppShell.xaml). Cela nous permet par exemple 
            // de naviguer "à l'intérieur" d'une grappe (comme aller au détail d'une machine 
            // depuis la page scanner) de façon dynamique.
            Routing.RegisterRoute("machinedetail", typeof(MachineDetailPage));
            Routing.RegisterRoute("maintenancesession", typeof(MaintenanceSessionPage));
        }
        catch (Exception ex)
        {
            // Log et fallback visuel pour éviter l'écran noir
            System.Diagnostics.Debug.WriteLine($"[CRASH] AppShell.InitializeComponent: {ex}");

            // Construire une Shell minimale affichant l'erreur afin que l'app n'affiche pas un écran noir
            var errorPage = new ContentPage
            {
                BackgroundColor = Colors.Black,
                Content = new ScrollView
                {
                    Content = new Label
                    {
                        Text = $"APP SHELL STARTUP ERROR:\n\n{ex}",
                        TextColor = Colors.Red,
                        FontSize = 14,
                        Padding = new Thickness(20),
                        VerticalOptions = LayoutOptions.Center
                    }
                }
            };

            Items.Clear();
            Items.Add(new FlyoutItem
            {
                Title = "Error",
                Icon = "warning.png",
                FlyoutDisplayOptions = FlyoutDisplayOptions.AsMultipleItems,
                Items =
                {
                    new ShellSection
                    {
                        Title = "Error",
                        Items =
                        {
                            new ShellContent
                            {
                                Content = errorPage
                            }
                        }
                    }
                }
            });
        }
    }

    protected override void OnNavigated(ShellNavigatedEventArgs args)
    {
        base.OnNavigated(args);
        UpdateAdminVisibility();
    }

    // 🎓 Pédagogie PFE : RBAC (Role-Based Access Control)
    // Cette méthode gère dynamiquement la visibilité des onglets d'administration.
    // Elle est appelée à chaque fois qu'on navigue `OnNavigated` car l'état de la session 
    // peut changer. C'est une sécurité *front-end* qui accompagne la sécurité de la DB.
    private void UpdateAdminVisibility()
    {
        // On récupère le SupabaseService manuellement via _services car AppShell n'utilise
        // pas toujours l'injection de dépendances standard de façon directe.
        var supabase = _services.GetService<SupabaseService>();
        bool isAdmin = supabase?.IsAdmin ?? false;
        bool isMaintenance = supabase?.IsMaintenance ?? false;

        System.Diagnostics.Debug.WriteLine($"[SHELL] UpdateAdminVisibility → IsAdmin={isAdmin}, IsMaintenance={isMaintenance}, Profile={supabase?.CurrentProfile?.FullName ?? "null"}, Role={supabase?.CurrentProfile?.Role ?? "null"}");
        
        // 🔧 Menu Maintenance : visible uniquement pour le rôle Maintenance (et Admin)
        if (MaintenanceFlyoutItem != null)
        {
            bool showMaint = isMaintenance || isAdmin;
            MaintenanceFlyoutItem.FlyoutItemIsVisible = showMaint;
            MaintenanceFlyoutItem.IsVisible = showMaint;
        }

        // 👑 Menus Admin : visible uniquement pour le rôle Admin
        if (AdminFlyoutItem != null)
        {
            AdminFlyoutItem.FlyoutItemIsVisible = isAdmin;
            AdminFlyoutItem.IsVisible = isAdmin;
        }
        
        if (UserMgmtFlyoutItem != null)
        {
            UserMgmtFlyoutItem.FlyoutItemIsVisible = isAdmin;
            UserMgmtFlyoutItem.IsVisible = isAdmin;
        }

        // 🏭 Menu Départements : visible uniquement pour le rôle Admin
        if (DepartmentFlyoutItem != null)
        {
            DepartmentFlyoutItem.FlyoutItemIsVisible = isAdmin;
            DepartmentFlyoutItem.IsVisible = isAdmin;
        }
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        var supabase = _services.GetService<SupabaseService>();
        if (supabase is not null) await supabase.LogoutAsync();
        FlyoutIsPresented = false;
        await GoToAsync("//login");
    }
}
