namespace CourierDelivery.Services;

// Ошибка «для пользователя»: её текст можно сразу показать в строке сообщений.
public class AppException : Exception
{
    public AppException(string message) : base(message)
    {
    }
}
