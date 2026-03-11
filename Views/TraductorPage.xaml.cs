using MauiOCRFacturas.ViewModels;

namespace MauiOCRFacturas.Views;

public partial class TraductorPage : ContentPage
{
    public TraductorPage(TraductorViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}