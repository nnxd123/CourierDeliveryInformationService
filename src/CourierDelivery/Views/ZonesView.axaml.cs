using Avalonia.Controls;
using Avalonia.Interactivity;
using CourierDelivery.Models;
using CourierDelivery.Services;

namespace CourierDelivery.Views;

public partial class ZonesView : UserControl
{
    // 0 — добавляем новую зону, иначе редактируем выбранную
    private int editId;

    public ZonesView()
    {
        InitializeComponent();
        Loaded += async (s, e) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            ZonesList.ItemsSource = await ZoneService.GetZonesAsync();
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    private void ZoneSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (ZonesList.SelectedItem is not Zone zone) return;
        editId = zone.ZoneId;
        FormTitle.Text = "Редактирование зоны";
        NameBox.Text = zone.Name;
        RateBox.Value = zone.RatePerKg;
        DescriptionBox.Text = zone.Description;
        DeleteButton.IsEnabled = true;
    }

    private void NewClick(object? sender, RoutedEventArgs e)
    {
        editId = 0;
        ZonesList.SelectedItem = null;
        FormTitle.Text = "Новая зона";
        NameBox.Text = "";
        RateBox.Value = 150;
        DescriptionBox.Text = "";
        DeleteButton.IsEnabled = false;
    }

    private async void SaveClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            await ZoneService.SaveZoneAsync(editId, NameBox.Text ?? "", UiSupport.ValueOf(RateBox), DescriptionBox.Text);
            MainWindow.Instance.ShowMessage(editId == 0 ? "Зона добавлена." : "Изменения сохранены.");
            await LoadAsync();
            NewClick(sender, e);
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    private async void DeleteClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            await ZoneService.DeleteZoneAsync(editId);
            MainWindow.Instance.ShowMessage("Зона удалена.");
            await LoadAsync();
            NewClick(sender, e);
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }
}
