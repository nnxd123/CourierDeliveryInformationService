using Avalonia;
using Avalonia.Media;
using CourierDelivery.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CourierDelivery.Views;

// Вспомогательные методы интерфейса: цвета из App.axaml и тексты ошибок.
public static class UiSupport
{
    // Цвет из App.axaml по ключу, например Brush("DangerBrush")
    public static IBrush Brush(string key) => (IBrush)Application.Current!.Resources[key]!;

    // Превращает техническую ошибку в понятный пользователю текст
    public static string ErrorText(Exception error)
    {
        if (error is AppException)
            return error.Message;

        if (error is SqlException || error is DbUpdateException || error.InnerException is SqlException)
            return "Ошибка базы данных. Проверьте, что SQL Server запущен и строка в connection.txt верна.";

        if (error is IOException)
            return "Не найден файл connection.txt рядом с программой.";

        return "Операция не выполнена: " + error.Message;
    }

    // Число из поля NumericUpDown (null — поле пустое)
    public static decimal ValueOf(Avalonia.Controls.NumericUpDown box) => box.Value ?? 0m;
}
