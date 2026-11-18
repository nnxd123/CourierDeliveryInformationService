using Avalonia.Controls;
using Avalonia.Interactivity;
using CourierDelivery.Models;
using CourierDelivery.Services;

namespace CourierDelivery.Views;

public partial class OrderDetailsView : UserControl
{
    private readonly int orderId;
    private Order? order;

    // Удаление требует двух нажатий (защита от случайного щелчка)
    private bool deleteArmed;

    public OrderDetailsView() : this(0) { }

    public OrderDetailsView(int orderId)
    {
        InitializeComponent();
        this.orderId = orderId;
        Loaded += async (s, e) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            order = await OrderService.GetOrderAsync(orderId);
            ShowOrder(order);
            await ConfigureActionsAsync(order);
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    private void ShowOrder(Order o)
    {
        TitleText.Text = $"Заказ {o.Number}";
        StatusText.Text = o.StatusName;
        StatusBadge.Classes.Set("work", o.IsWork);
        StatusBadge.Classes.Set("done", o.IsDone);
        StatusBadge.Classes.Set("cancel", o.IsCancelled);

        DateText.Text = o.DateText;
        ClientText.Text = $"{o.ClientName}, {o.Client?.PhoneText}";
        ZoneText.Text = $"{o.ZoneName} ({o.Zone?.RateText})";
        CargoText.Text = $"{o.WeightText}" + (o.IsUrgent ? ", срочная доставка" : ", обычная доставка");
        CourierText.Text = o.Courier == null ? "не назначен" : $"{o.CourierName} ({o.Courier.Transport.Name})";
        CommentText.Text = string.IsNullOrWhiteSpace(o.Comment) ? "—" : o.Comment;
        CostText.Text = o.DiscountPercent > 0
            ? $"{o.CostText} (скидка {o.DiscountPercent} %)"
            : o.CostText;

        PickupAddressText.Text = o.Pickup?.Address ?? "—";
        PickupContactText.Text = $"{o.Pickup?.ContactName}, {PhoneFormat.Pretty(o.Pickup?.ContactPhone)}";
        DeliveryAddressText.Text = o.Delivery?.Address ?? "—";
        DeliveryContactText.Text = $"{o.Delivery?.ContactName}, {PhoneFormat.Pretty(o.Delivery?.ContactPhone)}";
    }

    // Набор кнопок зависит от роли и статуса заказа
    private async Task ConfigureActionsAsync(Order o)
    {
        bool open = !o.IsDone && !o.IsCancelled;

        AssignPanel.IsVisible = Session.IsDispatcher && open;
        StatusPanel.IsVisible = Session.IsDispatcher && open;
        DeliveredButton.IsVisible = Session.IsCourier && o.IsWork;
        CancelButton.IsVisible = Session.IsClient && o.StatusName == OrderService.StatusNew;
        DeleteButton.IsVisible = Session.IsAdmin;

        if (AssignPanel.IsVisible)
        {
            var couriers = await StaffService.GetAvailableCouriersAsync();
            CourierBox.ItemsSource = couriers;
            CourierBox.SelectedItem = couriers.FirstOrDefault(c => c.CourierId == o.CourierId);

            var statuses = await OrderService.GetStatusesAsync();
            StatusBox.ItemsSource = statuses.Select(s => s.Name).ToList();
            StatusBox.SelectedItem = o.StatusName;
        }
    }

    private async Task RunAsync(Func<Task> action, string message)
    {
        try
        {
            await action();
            MainWindow.Instance.ShowMessage(message);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    private async void AssignClick(object? sender, RoutedEventArgs e)
    {
        if (CourierBox.SelectedItem is not Courier courier)
        {
            MainWindow.Instance.ShowWarning("Выберите курьера из списка.");
            return;
        }
        await RunAsync(() => OrderService.AssignCourierAsync(orderId, courier.CourierId),
            $"Курьер {courier.FullName} назначен, заказ переведён в статус «В работе».");
    }

    private async void StatusClick(object? sender, RoutedEventArgs e)
    {
        if (StatusBox.SelectedItem is not string status) return;
        await RunAsync(() => OrderService.ChangeStatusAsync(orderId, status), $"Статус изменён на «{status}».");
    }

    private async void DeliveredClick(object? sender, RoutedEventArgs e) =>
        await RunAsync(() => OrderService.ChangeStatusAsync(orderId, OrderService.StatusDone),
            "Доставка подтверждена. Заказ выполнен.");

    private async void CancelClick(object? sender, RoutedEventArgs e) =>
        await RunAsync(() => OrderService.CancelByClientAsync(orderId), "Заказ отменён.");

    private async void DeleteClick(object? sender, RoutedEventArgs e)
    {
        if (!deleteArmed)
        {
            deleteArmed = true;
            ConfirmText.IsVisible = true;
            return;
        }

        try
        {
            string? freed = await OrderService.DeleteOrderAsync(orderId);
            MainWindow.Instance.OpenPage(new OrdersView(), clearHistory: true);
            MainWindow.Instance.ShowMessage(freed == null
                ? $"Заказ № {orderId:D5} удалён."
                : $"Заказ № {orderId:D5} удалён, у курьера {freed} освободилось место.");
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    private void BackClick(object? sender, RoutedEventArgs e) => MainWindow.Instance.GoBack();
}
