using Avalonia.Controls;
using Avalonia.Interactivity;
using CourierDelivery.Models;
using CourierDelivery.Services;

namespace CourierDelivery.Views;

public partial class OrderCreateView : UserControl
{
    // Параметры, переданные со страницы «Тарифы» (могут отсутствовать)
    public int? PresetZoneId { get; init; }
    public decimal? PresetWeight { get; init; }
    public bool PresetUrgent { get; init; }

    private int discount;

    public OrderCreateView()
    {
        InitializeComponent();

        // Клиент оформляет заказ только на себя — выбор клиента ему не нужен
        ClientPanel.IsVisible = Session.IsDispatcher;

        Loaded += async (s, e) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var zones = await ZoneService.GetZonesAsync();
            ZoneBox.ItemsSource = zones;
            ZoneBox.SelectedItem = zones.FirstOrDefault(z => z.ZoneId == PresetZoneId) ?? zones.FirstOrDefault();
            if (PresetWeight.HasValue) WeightBox.Value = PresetWeight;
            UrgentBox.IsChecked = PresetUrgent;

            if (Session.IsDispatcher)
            {
                ClientBox.ItemsSource = await StaffService.GetClientsAsync();
            }
            else if (Session.CurrentUser is { } me)
            {
                // Контакты отправителя по умолчанию — сам клиент
                PickupContactBox.Text = me.FullName;
                PickupPhoneBox.Text = me.PhoneText;
                discount = await OrderService.GetDiscountAsync(me.UserId);
            }

            Recalculate();
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    // Выбран другой клиент — заново определяем его скидку
    private async void ClientChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ClientBox.SelectedItem is not User client) return;
        try
        {
            discount = await OrderService.GetDiscountAsync(client.UserId);
            if (string.IsNullOrWhiteSpace(PickupContactBox.Text))
            {
                PickupContactBox.Text = client.FullName;
                PickupPhoneBox.Text = client.PhoneText;
            }
            Recalculate();
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    private void Recalculate()
    {
        if (TotalText == null) return;

        decimal weight = UiSupport.ValueOf(WeightBox);
        bool urgent = UrgentBox.IsChecked == true;

        WeightText.Text = $"{weight:0.##} кг";
        UrgentText.Text = urgent ? "×1,5" : "×1";
        DiscountText.Text = $"{discount} %";
        DiscountText.Foreground = UiSupport.Brush(discount > 0 ? "DoneBrush" : "InkBrush");

        if (ZoneBox.SelectedItem is Zone zone && weight > 0 && weight <= PriceRules.MaxWeight)
        {
            RateText.Text = zone.RateText;
            TotalText.Text = $"{PriceRules.Total(zone.RatePerKg, weight, urgent, discount):N2} руб.";
        }
        else
        {
            RateText.Text = (ZoneBox.SelectedItem as Zone)?.RateText ?? "—";
            TotalText.Text = "—";
        }
    }

    private void ParamsChanged(object? sender, SelectionChangedEventArgs e) => Recalculate();
    private void WeightChanged(object? sender, NumericUpDownValueChangedEventArgs e) => Recalculate();
    private void UrgentChanged(object? sender, RoutedEventArgs e) => Recalculate();

    private async void SaveClick(object? sender, RoutedEventArgs e)
    {
        SaveButton.IsEnabled = false;
        try
        {
            var draft = new OrderDraft
            {
                ClientId = (ClientBox.SelectedItem as User)?.UserId ?? 0,
                ZoneId = (ZoneBox.SelectedItem as Zone)?.ZoneId ?? 0,
                Weight = UiSupport.ValueOf(WeightBox),
                IsUrgent = UrgentBox.IsChecked == true,
                PickupAddress = PickupAddressBox.Text ?? "",
                PickupContact = PickupContactBox.Text ?? "",
                PickupPhone = PickupPhoneBox.Text ?? "",
                DeliveryAddress = DeliveryAddressBox.Text ?? "",
                DeliveryContact = DeliveryContactBox.Text ?? "",
                DeliveryPhone = DeliveryPhoneBox.Text ?? "",
                Comment = CommentBox.Text
            };

            int id = await OrderService.CreateOrderAsync(draft);

            MainWindow.Instance.OpenPage(new OrderDetailsView(id), clearHistory: true);
            MainWindow.Instance.ShowMessage($"Заказ № {id:D5} оформлен. Статус: «Новый».");
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
        finally
        {
            SaveButton.IsEnabled = true;
        }
    }

    private void BackClick(object? sender, RoutedEventArgs e) => MainWindow.Instance.GoBack();
}
