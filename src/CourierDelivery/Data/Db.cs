using Microsoft.EntityFrameworkCore;

namespace CourierDelivery.Data;

// Db — фабрика подключений. В любом месте программы пишем
//     using var db = Db.Create();
// и получаем готовый к работе контекст DeliveryContext.
public static class Db
{
    public static DeliveryContext Create()
    {
        // Файл connection.txt копируется рядом с .exe (настройка в CourierDelivery.csproj)
        string path = Path.Combine(AppContext.BaseDirectory, "connection.txt");

        // Читаем строку подключения; Trim убирает пробелы и переводы строк
        string connection = File.ReadAllText(path).Trim();

        // Работаем с Microsoft SQL Server по этой строке подключения
        var options = new DbContextOptionsBuilder<DeliveryContext>()
            .UseSqlServer(connection)
            .Options;

        return new DeliveryContext(options);
    }
}
