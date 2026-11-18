using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CourierDelivery.Models;
using CourierDelivery.Services;

namespace CourierDelivery.Views;

public partial class OrdersView : UserControl
{
    // Все заказы, загруженные из БД; фильтры работают по этому списку
    private List<Order> allOrders = new();

    public OrdersView()
    {
        InitializeComponent();
        StatusFilter.SelectedIndex = 0;
        SortBox.SelectedIndex = 0;
        ConfigureForRole();
        Loaded += async (s, e) => await LoadAsync();
    }

    // Заголовок и кнопки зависят от роли
    private void ConfigureForRole()
    {
        if (Session.IsClient)
        {
            TitleText.Text = "Мои заказы";
            SumLabel.Text = "Потрачено на доставку";
        }
        else if (Session.IsCourier)
        {
            TitleText.Text = "Мои доставки";
            SubtitleText.Text = "Заказы, назначенные вам. Откройте заказ, чтобы отметить доставку.";
            NewButton.IsVisible = false;
        }
        else
        {
            TitleText.Text = "Все заказы";
        }
    }

    private async Task LoadAsync()
    {
        try
        {
            allOrders = await OrderService.GetOrdersAsync();

            NewCount.Text = allOrders.Count(o => o.StatusName == OrderService.StatusNew).ToString();
            WorkCount.Text = allOrders.Count(o => o.IsWork).ToString();
            DoneCount.Text = allOrders.Count(o => o.IsDone).ToString();
            SumText.Text = $"{allOrders.Where(o => o.IsDone).Sum(o => o.TotalCost):N0} руб.";

            ApplyFilters();
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    /// <summary>
    /// Совместный поиск (номер ИЛИ ФИО клиента) + фильтр по статусу + сортировка по дате.
    /// </summary>
    private void ApplyFilters()
    {
        string text = (SearchBox.Text ?? "").Trim().TrimStart('№').Trim();
        var ic = StringComparison.OrdinalIgnoreCase;

        string? status = StatusFilter.SelectedIndex > 0
            ? (StatusFilter.SelectedItem as ComboBoxItem)?.Content as string
            : null;

        IEnumerable<Order> rows = allOrders.Where(o =>
            (status == null || o.StatusName == status) &&
            (text == "" ||
             o.OrderId.ToString().Contains(text) ||
             o.ClientName.Contains(text, ic)));

        rows = SortBox.SelectedIndex == 1
            ? rows.OrderBy(o => o.OrderDate)
            : rows.OrderByDescending(o => o.OrderDate);

        var list = rows.ToList();
        OrdersList.ItemsSource = list;
        EmptyText.IsVisible = list.Count == 0;
        CountText.Text = $"Показано: {list.Count} из {allOrders.Count}";
    }

    private void SearchChanged(object? sender, TextChangedEventArgs e) => ApplyFilters();

    private void FilterChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (OrdersList != null && SortBox != null) ApplyFilters();
    }

    private async void ReloadClick(object? sender, RoutedEventArgs e) => await LoadAsync();

    private void NewClick(object? sender, RoutedEventArgs e) =>
        MainWindow.Instance.OpenPage(new OrderCreateView());

    // Щелчок по строке — карточка заказа
    private void OrderOpened(object? sender, TappedEventArgs e)
    {
        if (OrdersList.SelectedItem is Order order)
            MainWindow.Instance.OpenPage(new OrderDetailsView(order.OrderId));
    }
}
