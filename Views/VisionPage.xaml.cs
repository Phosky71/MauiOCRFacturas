using Camera.MAUI;
using MauiOCRFacturas.ViewModels;

namespace MauiOCRFacturas.Views;

public partial class VisionPage : ContentPage
{
    private readonly VisionViewModel _viewModel;

    public VisionPage(VisionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;

        // Espera a que las cámaras estén disponibles antes de iniciar
        CamaraView.CamerasLoaded += async (s, e) =>
        {
            if (CamaraView.NumCamerasDetected > 0)
            {
                CamaraView.Camera = CamaraView.Cameras
                                        .FirstOrDefault(c => c.Position == CameraPosition.Back)
                                    ?? CamaraView.Cameras.First();

                await MainThread.InvokeOnMainThreadAsync(async () =>
                    await CamaraView.StartCameraAsync());
            }
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _viewModel.IniciarAnalisisContinuoAsync(CamaraView);
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.DetenerAnalisis();
        await CamaraView.StopCameraAsync();
    }
}