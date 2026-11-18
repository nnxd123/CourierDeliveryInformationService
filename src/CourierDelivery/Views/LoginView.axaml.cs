using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CourierDelivery.Services;

namespace CourierDelivery.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();

        // Enter в любом поле равносилен нажатию «Войти»
        LoginBox.KeyDown += FieldKeyDown;
        PasswordBox.KeyDown += FieldKeyDown;
        Loaded += (s, e) => LoginBox.Focus();
    }

    private void FieldKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            LoginClick(sender, new RoutedEventArgs());
    }

    private async void LoginClick(object? sender, RoutedEventArgs e)
    {
        ErrorText.IsVisible = false;
        string login = (LoginBox.Text ?? "").Trim();
        string password = PasswordBox.Text ?? "";

        if (login == "" || password == "")
        {
            ShowError("Введите логин и пароль.");
            return;
        }

        LoginButton.IsEnabled = false;
        try
        {
            await Session.LoginAsync(login, password);

            // После входа открываем список заказов: шапка перестроится под роль
            MainWindow.Instance.OpenPage(new OrdersView(), clearHistory: true);
            MainWindow.Instance.ShowMessage($"Добро пожаловать, {Session.CurrentUser!.FirstName}! Роль: {Session.RoleName}.");
        }
        catch (Exception ex)
        {
            ShowError(UiSupport.ErrorText(ex));
        }
        finally
        {
            LoginButton.IsEnabled = true;
        }
    }

    private void ShowError(string text)
    {
        ErrorText.Text = text;
        ErrorText.IsVisible = true;
        MainWindow.Instance.ShowWarning(text);
    }

    private void BackClick(object? sender, RoutedEventArgs e) => MainWindow.Instance.GoBack();

    private void RegisterClick(object? sender, RoutedEventArgs e) =>
        MainWindow.Instance.OpenPage(new RegistrationView());
}
