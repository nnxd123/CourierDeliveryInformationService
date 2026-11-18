using Avalonia.Controls;
using Avalonia.Interactivity;
using CourierDelivery.Services;

namespace CourierDelivery.Views;

public partial class CouriersView : UserControl
{
    public CouriersView()
    {
        InitializeComponent();
        Loaded += async (s, e) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            CouriersList.ItemsSource = await StaffService.GetCouriersAsync();
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    private async void ReloadClick(object? sender, RoutedEventArgs e) => await LoadAsync();
}
