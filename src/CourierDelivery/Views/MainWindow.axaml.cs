using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CourierDelivery.Services;

namespace CourierDelivery.Views;

public partial class MainWindow : Window
{
    public static MainWindow Instance { get; private set; } = null!;

    // История открытых страниц для кнопки «Назад»
    private readonly Stack<UserControl> history = new();

    // Таймер, который возвращает строку сообщений в исходное состояние
    private readonly DispatcherTimer statusTimer = new() { Interval = TimeSpan.FromSeconds(8) };

    public MainWindow()
    {
        InitializeComponent();
        Instance = this;
        statusTimer.Tick += (s, e) => ResetStatus();

        // Стартовая страница — тарифы (доступна гостю)
        OpenPage(new TariffsView(), clearHistory: true);
    }

    /// <summary>
    /// Открыть страницу. clearHistory: true — начать историю заново (пункт меню, вход, выход).
    /// </summary>
    public void OpenPage(UserControl page, bool clearHistory = false)
    {
        if (clearHistory)
            history.Clear();
        else if (PageHost.Content is UserControl current)
            history.Push(current);

        ShowPage(page);
    }

    /// <summary>
    /// Вернуться на предыдущую страницу.
    /// </summary>
    public void GoBack()
    {
        if (history.Count > 0)
            ShowPage(history.Pop());
        else
            OpenPage(Session.IsGuest ? new TariffsView() : new OrdersView(), clearHistory: true);
    }

    private void ShowPage(UserControl page)
    {
        PageHost.Content = page;

        // Заголовок окна берётся из свойства Tag страницы
        string title = page.Tag as string ?? "";
        Title = "КурьерЭкспресс — " + title;
        PageText.Text = title;

        UpdateHeader(page);
    }

    // Шапка перестраивается под роль пользователя
    private void UpdateHeader(UserControl page)
    {
        if (Session.CurrentUser is { } user)
        {
            UserText.Text = user.FullName;
            RoleText.Text = Session.RoleName;
            AuthButton.Content = "Выйти";
            AuthButton.Classes.Remove("amber");
            AuthButton.Classes.Add("outline");
        }
        else
        {
            UserText.Text = "Гость";
            RoleText.Text = "вход не выполнен";
            AuthButton.Content = "Войти";
            AuthButton.Classes.Remove("outline");
            AuthButton.Classes.Add("amber");
        }

        OrdersTab.IsVisible = !Session.IsGuest;
        OrdersTab.Content = Session.IsClient ? "Мои заказы"
                          : Session.IsCourier ? "Мои доставки"
                          : "Заказы";
        NewOrderTab.IsVisible = Session.IsClient || Session.IsDispatcher;
        CouriersTab.IsVisible = Session.IsDispatcher;
        ZonesTab.IsVisible = Session.IsAdmin;
        StaffTab.IsVisible = Session.IsAdmin;

        SetActive(TariffsTab, page is TariffsView or LoginView or RegistrationView);
        SetActive(OrdersTab, page is OrdersView or OrderDetailsView);
        SetActive(NewOrderTab, page is OrderCreateView);
        SetActive(CouriersTab, page is CouriersView);
        SetActive(ZonesTab, page is ZonesView);
        SetActive(StaffTab, page is StaffView);
    }

    private static void SetActive(Button tab, bool active)
    {
        if (active && !tab.Classes.Contains("active"))
            tab.Classes.Add("active");
        else if (!active)
            tab.Classes.Remove("active");
    }

    // ---------- СТРОКА СООБЩЕНИЙ ----------

    public void ShowMessage(string text) => SetStatus(text, "DoneBrush");

    public void ShowWarning(string text) => SetStatus(text, "WorkBrush");

    public void ShowError(Exception error)
    {
        System.Diagnostics.Trace.WriteLine(error);
        SetStatus(UiSupport.ErrorText(error), "DangerBrush");
    }

    private void SetStatus(string text, string brushKey)
    {
        StatusText.Text = text;
        StatusText.Foreground = UiSupport.Brush(brushKey == "DoneBrush" ? "InkBrush" : brushKey);
        StatusDot.Background = UiSupport.Brush(brushKey);
        statusTimer.Stop();
        statusTimer.Start();
    }

    private void ResetStatus()
    {
        statusTimer.Stop();
        StatusText.Text = "Готово к работе";
        StatusText.Foreground = UiSupport.Brush("InkBrush");
        StatusDot.Background = UiSupport.Brush("DoneBrush");
    }

    // ---------- ВКЛАДКИ ----------

    private void TariffsClick(object? sender, RoutedEventArgs e) => OpenPage(new TariffsView(), true);
    private void OrdersClick(object? sender, RoutedEventArgs e) => OpenPage(new OrdersView(), true);
    private void NewOrderClick(object? sender, RoutedEventArgs e) => OpenPage(new OrderCreateView(), true);
    private void CouriersClick(object? sender, RoutedEventArgs e) => OpenPage(new CouriersView(), true);
    private void ZonesClick(object? sender, RoutedEventArgs e) => OpenPage(new ZonesView(), true);
    private void StaffClick(object? sender, RoutedEventArgs e) => OpenPage(new StaffView(), true);

    // «Войти» для гостя, «Выйти» для остальных
    private void AuthClick(object? sender, RoutedEventArgs e)
    {
        if (Session.IsGuest)
        {
            OpenPage(new LoginView());
        }
        else
        {
            Session.Logout();
            OpenPage(new TariffsView(), clearHistory: true);
            ShowMessage("Вы вышли из системы.");
        }
    }
}
