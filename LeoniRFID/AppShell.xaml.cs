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
            // Commentaire pédagogique :
            // - `InitializeComponent` charge `AppShell.xaml` et les contrôles/ressources utilisés par la Shell.
            // - Une image ou un StaticResource manquant dans le XAML provoquera une exception ici.
            InitializeComponent();

            // Register routes for pages not in Shell hierarchy
            Routing.RegisterRoute("machinedetail", typeof(MachineDetailPage));
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
                        Text = $"ERREUR APP SHELL AU DÉMARRAGE:\n\n{ex}",
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
                Title = "Erreur",
                Icon = "warning.png",
                FlyoutDisplayOptions = FlyoutDisplayOptions.AsMultipleItems,
                Items =
                {
                    new ShellSection
                    {
                        Title = "Erreur",
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

    private void UpdateAdminVisibility()
    {
        var supabase = _services.GetService<SupabaseService>();
        bool isAdmin = supabase?.IsAdmin ?? false;

        System.Diagnostics.Debug.WriteLine($"[SHELL] UpdateAdminVisibility → IsAdmin={isAdmin}, Profile={supabase?.CurrentProfile?.FullName ?? "null"}, Role={supabase?.CurrentProfile?.Role ?? "null"}");
        
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
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        var supabase = _services.GetService<SupabaseService>();
        if (supabase is not null) await supabase.LogoutAsync();
        FlyoutIsPresented = false;
        await GoToAsync("//login");
    }
}
