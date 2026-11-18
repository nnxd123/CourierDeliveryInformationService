using Avalonia.Controls;
using Avalonia.Interactivity;
using CourierDelivery.Services;

namespace CourierDelivery.Views;

public partial class RegistrationView : UserControl
{
    public RegistrationView()
    {
        InitializeComponent();
    }

    private async void RegisterClick(object? sender, RoutedEventArgs e)
    {
        SaveButton.IsEnabled = false;
        try
        {
            string login = LoginBox.Text ?? "";
            string password = PasswordBox.Text ?? "";

            await StaffService.RegisterClientAsync(login, password,
                LastNameBox.Text ?? "", FirstNameBox.Text ?? "", PhoneBox.Text ?? "");

            // Сразу входим под новой учётной записью
            await Session.LoginAsync(login, password);
            MainWindow.Instance.OpenPage(new OrdersView(), clearHistory: true);
            MainWindow.Instance.ShowMessage("Регистрация выполнена. Теперь можно оформить заказ.");
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
