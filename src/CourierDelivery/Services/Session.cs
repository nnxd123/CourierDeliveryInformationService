using CourierDelivery.Data;
using CourierDelivery.Models;
using Microsoft.EntityFrameworkCore;

namespace CourierDelivery.Services;

// Session («сеанс») хранит пользователя, который сейчас работает с программой.
public static class Session
{
    // Названия ролей — точно как в таблице Roles
    public const string RoleAdmin = "Администратор";
    public const string RoleOperator = "Оператор";
    public const string RoleCourier = "Курьер";
    public const string RoleClient = "Клиент";

    // Текущий пользователь; null — гость (никто не вошёл)
    public static User? CurrentUser { get; private set; }

    public static string RoleName => CurrentUser?.Role?.RoleName ?? "";

    public static bool IsGuest => CurrentUser == null;
    public static bool IsAdmin => RoleName == RoleAdmin;
    public static bool IsOperator => RoleName == RoleOperator;
    public static bool IsCourier => RoleName == RoleCourier;
    public static bool IsClient => RoleName == RoleClient;

    // Диспетчеры: оператор и администратор (администратор наследует права оператора)
    public static bool IsDispatcher => IsAdmin || IsOperator;

    /// <summary>
    /// Вход по логину и паролю. В учебной версии пароль хранится открытым текстом.
    /// </summary>
    public static async Task LoginAsync(string login, string password)
    {
        using var db = Db.Create();
        string normalized = login.Trim().ToLower();

        var user = await db.Users
            .Include(u => u.Role)
            .Include(u => u.Courier).ThenInclude(c => c!.Transport)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Login.ToLower() == normalized);

        // Одинаковый текст для «нет логина» и «неверный пароль»
        if (user == null || user.Password != password)
            throw new AppException("Неверный логин или пароль.");

        CurrentUser = user;
    }

    public static void Logout() => CurrentUser = null;

    // ---------- Проверки прав (вызываются в сервисах перед изменением данных) ----------

    public static void RequireLogin()
    {
        if (IsGuest)
            throw new AppException("Войдите в систему, чтобы выполнить действие.");
    }

    public static void RequireDispatcher()
    {
        if (!IsDispatcher)
            throw new AppException("Недостаточно прав: нужна роль оператора или администратора.");
    }

    public static void RequireAdmin()
    {
        if (!IsAdmin)
            throw new AppException("Недостаточно прав: нужна роль администратора.");
    }
}
