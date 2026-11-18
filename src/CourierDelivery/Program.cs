using Avalonia;

namespace CourierDelivery;

class Program
{
    // Точка входа. До вызова StartWithClassicDesktopLifetime нельзя
    // обращаться к Avalonia: платформа ещё не инициализирована.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Настройка Avalonia (используется и визуальным дизайнером).
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
