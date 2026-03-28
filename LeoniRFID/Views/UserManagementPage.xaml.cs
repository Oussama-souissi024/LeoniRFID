using LeoniRFID.ViewModels;

namespace LeoniRFID.Views;

public partial class UserManagementPage : ContentPage
{
    private readonly UserManagementViewModel _viewModel;

    public UserManagementPage(UserManagementViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadUsersCommand.ExecuteAsync(null);
    }
}
