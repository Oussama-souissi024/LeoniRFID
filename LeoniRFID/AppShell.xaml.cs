using LeoniRFID.Services;
using LeoniRFID.ViewModels;
using LeoniRFID.Views;

namespace LeoniRFID;

public partial class AppShell : Shell
{
    private readonly IServiceProvider _services;

    public AppShell(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;

        // Register routes for pages not in Shell hierarchy
        Routing.RegisterRoute("machinedetail", typeof(MachineDetailPage));
    }

    protected override void OnNavigated(ShellNavigatedEventArgs args)
    {
        base.OnNavigated(args);
        UpdateAdminVisibility();
    }

    private void UpdateAdminVisibility()
    {
        var auth = _services.GetService<AuthService>();
        if (AdminFlyoutItem != null)
        {
            AdminFlyoutItem.IsVisible = auth?.IsAdmin ?? false;
        }
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        var auth = _services.GetService<AuthService>();
        auth?.Logout();
        FlyoutIsPresented = false;
        await GoToAsync("//login");
    }
}
