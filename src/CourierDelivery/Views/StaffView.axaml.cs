using Avalonia.Controls;
using Avalonia.Interactivity;
using CourierDelivery.Services;

namespace CourierDelivery.Views;

public partial class StaffView : UserControl
{
    public StaffView()
    {
        InitializeComponent();
        RoleBox.SelectedIndex = 0;
        TransportBox.SelectedIndex = 0;
    }

    private string SelectedRole => (RoleBox.SelectedItem as ComboBoxItem)?.Content as string ?? Session.RoleCourier;

    // Транспорт указывается только для курьера
    private void RoleChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (TransportPanel != null)
            TransportPanel.IsVisible = SelectedRole == Session.RoleCourier;
    }

    private async void SaveClick(object? sender, RoutedEventArgs e)
    {
        SaveButton.IsEnabled = false;
        try
        {
            string role = SelectedRole;
            string? transport = (TransportBox.SelectedItem as ComboBoxItem)?.Content as string;

            await StaffService.CreateEmployeeAsync(LoginBox.Text ?? "", PasswordBox.Text ?? "",
                LastNameBox.Text ?? "", FirstNameBox.Text ?? "", PhoneBox.Text ?? "", role, transport);

            MainWindow.Instance.ShowMessage($"Сотрудник {LastNameBox.Text} {FirstNameBox.Text} создан (роль «{role}»).");
            MainWindow.Instance.OpenPage(role == Session.RoleCourier ? new CouriersView() : new StaffView(), true);
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
}
