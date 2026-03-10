using MauiOCRFacturas.ViewModels;

namespace MauiOCRFacturas.Views;

public partial class HistorialPage : ContentPage
{
    public HistorialPage(HistorialViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is HistorialViewModel vm)
            vm.ActualizarHistorialCommand.Execute(null);
    }
}
