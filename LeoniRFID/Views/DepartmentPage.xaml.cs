using LeoniRFID.ViewModels;

namespace LeoniRFID.Views;

public partial class DepartmentPage : ContentPage
{
    private readonly DepartmentViewModel _vm;

    public DepartmentPage(DepartmentViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }
}
