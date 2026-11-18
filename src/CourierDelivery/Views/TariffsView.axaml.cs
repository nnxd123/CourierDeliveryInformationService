using Avalonia.Controls;
using Avalonia.Interactivity;
using CourierDelivery.Models;
using CourierDelivery.Services;

namespace CourierDelivery.Views;

public partial class TariffsView : UserControl
{
    // Скидка текущего клиента (для гостя и сотрудников — 0)
    private int discount;

    public TariffsView()
    {
        InitializeComponent();

        bool canOrder = Session.IsClient || Session.IsDispatcher;
        OrderButton.IsVisible = canOrder;
        GuestText.IsVisible = Session.IsGuest;

        Loaded += async (s, e) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var zones = await ZoneService.GetZonesAsync();
            ZonesList.ItemsSource = zones;
            ZoneBox.ItemsSource = zones;
            ZoneBox.SelectedIndex = zones.Count > 0 ? 0 : -1;

            if (Session.IsClient)
                discount = await OrderService.GetDiscountAsync(Session.CurrentUser!.UserId);

            Recalculate();
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    // Пересчёт стоимости при любом изменении параметров
    private void Recalculate()
    {
        if (TotalText == null) return; // форма ещё не построена

        if (ZoneBox.SelectedItem is not Zone zone)
        {
            TotalText.Text = "—";
            return;
        }

        decimal weight = UiSupport.ValueOf(WeightBox);
        bool urgent = UrgentBox.IsChecked == true;

        string factor = urgent ? " × 1,5" : "";
        FormulaText.Text = $"{zone.RatePerKg:N0} руб./кг × {weight:0.##} кг{factor}";

        DiscountText.IsVisible = discount > 0;
        DiscountText.Text = $"Скидка постоянного клиента: {discount} %";

        try
        {
            PriceRules.CheckWeight(weight);
            TotalText.Text = $"{PriceRules.Total(zone.RatePerKg, weight, urgent, discount):N2} руб.";
        }
        catch (AppException ex)
        {
            TotalText.Text = "—";
            MainWindow.Instance.ShowWarning(ex.Message);
        }
    }

    private void ParamsChanged(object? sender, SelectionChangedEventArgs e) => Recalculate();
    private void WeightChanged(object? sender, NumericUpDownValueChangedEventArgs e) => Recalculate();
    private void UrgentChanged(object? sender, RoutedEventArgs e) => Recalculate();

    // Переход к оформлению с уже выбранными зоной, весом и срочностью
    private void OrderClick(object? sender, RoutedEventArgs e)
    {
        var page = new OrderCreateView
        {
            PresetZoneId = (ZoneBox.SelectedItem as Zone)?.ZoneId,
            PresetWeight = WeightBox.Value,
            PresetUrgent = UrgentBox.IsChecked == true
        };
        MainWindow.Instance.OpenPage(page);
    }
}
