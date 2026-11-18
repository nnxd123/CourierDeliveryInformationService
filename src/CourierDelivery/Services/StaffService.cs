using System.Text.RegularExpressions;
using CourierDelivery.Data;
using CourierDelivery.Models;
using Microsoft.EntityFrameworkCore;

namespace CourierDelivery.Services;

// Работа с пользователями: регистрация клиентов, сотрудники, загрузка курьеров.
public static class StaffService
{
    public static async Task<List<string>> GetTransportNamesAsync()
    {
        Session.RequireAdmin();
        using var db = Db.Create();
        return await db.Transports.OrderBy(t => t.TransportId).Select(t => t.Name).ToListAsync();
    }

    /// <summary>
    /// Курьеры с количеством активных заказов (для раздела «Курьеры»).
    /// </summary>
    public static async Task<List<Courier>> GetCouriersAsync()
    {
        Session.RequireDispatcher();
        using var db = Db.Create();

        var couriers = await db.Couriers
            .Include(c => c.Transport)
            .Include(c => c.User)
            .AsNoTracking()
            .OrderBy(c => c.User.LastName)
            .ToListAsync();

        // Считаем активные заказы одним запросом с группировкой
        var load = await db.Orders
            .Where(o => o.CourierId != null && o.Status.Name == OrderService.StatusWork)
            .GroupBy(o => o.CourierId!.Value)
            .Select(g => new { CourierId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CourierId, x => x.Count);

        foreach (var c in couriers)
            c.ActiveOrders = load.GetValueOrDefault(c.CourierId);

        return couriers;
    }

    /// <summary>
    /// Курьеры, которых можно назначить: активные и с загрузкой менее 10 заказов.
    /// </summary>
    public static async Task<List<Courier>> GetAvailableCouriersAsync()
    {
        var all = await GetCouriersAsync();
        return all.Where(c => c.IsActive && c.ActiveOrders < PriceRules.MaxActiveOrders).ToList();
    }

    /// <summary>
    /// Список клиентов для оформления заказа оператором.
    /// </summary>
    public static async Task<List<User>> GetClientsAsync()
    {
        using var db = Db.Create();
        return await db.Users
            .AsNoTracking()
            .Where(u => u.Role.RoleName == Session.RoleClient)
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .ToListAsync();
    }

    /// <summary>
    /// Проверка данных пользователя. Возвращает телефон из одних цифр.
    /// </summary>
    public static string Validate(string login, string password, string lastName, string firstName, string phoneText)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            throw new AppException("Логин и пароль обязательны.");
        if (!Regex.IsMatch(login.Trim(), "^[A-Za-z0-9_.]{3,50}$"))
            throw new AppException("Логин: от 3 до 50 латинских букв, цифр, точек или «_».");
        if (password.Length < 4)
            throw new AppException("Пароль должен содержать не менее 4 символов.");
        if (string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(firstName))
            throw new AppException("Укажите фамилию и имя.");

        string phone = new string(phoneText.Where(char.IsDigit).ToArray());
        if (phone.Length == 10) phone = "7" + phone;
        if (phone.Length == 11 && phone[0] == '8') phone = "7" + phone[1..];
        if (phone.Length != 11)
            throw new AppException("Телефон должен содержать 11 цифр, например +7 921 000-00-00.");
        return phone;
    }

    /// <summary>
    /// Регистрация клиента (доступна гостю на экране входа).
    /// </summary>
    public static Task<int> RegisterClientAsync(string login, string password, string lastName,
        string firstName, string phoneText)
    {
        string phone = Validate(login, password, lastName, firstName, phoneText);
        return CreateUserAsync(login, password, lastName, firstName, phone, Session.RoleClient, null);
    }

    /// <summary>
    /// Создание сотрудника (только администратор): оператор или курьер.
    /// </summary>
    public static Task<int> CreateEmployeeAsync(string login, string password, string lastName,
        string firstName, string phoneText, string roleName, string? transport)
    {
        Session.RequireAdmin();
        string phone = Validate(login, password, lastName, firstName, phoneText);
        if (roleName == Session.RoleCourier && string.IsNullOrWhiteSpace(transport))
            throw new AppException("Укажите вид транспорта курьера.");
        return CreateUserAsync(login, password, lastName, firstName, phone, roleName, transport);
    }

    private static async Task<int> CreateUserAsync(string login, string password, string lastName,
        string firstName, string phone, string roleName, string? transport)
    {
        using var db = Db.Create();
        login = login.Trim();

        string normalized = login.ToLower();
        if (await db.Users.AnyAsync(u => u.Login.ToLower() == normalized))
            throw new AppException("Такой логин уже занят.");

        var role = await db.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName)
                   ?? throw new AppException($"Роль «{roleName}» не найдена в базе данных.");

        // Пользователь и профиль курьера сохраняются в одной транзакции
        using var transaction = await db.Database.BeginTransactionAsync();

        var user = new User
        {
            Login = login,
            Password = password,       // учебная версия: пароль без хеширования
            LastName = lastName.Trim(),
            FirstName = firstName.Trim(),
            Phone = phone,
            RoleId = role.RoleId
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        if (roleName == Session.RoleCourier)
        {
            db.Couriers.Add(new Courier { UserId = user.UserId, TransportId = await db.Transports.Where(t => t.Name == transport).Select(t => t.TransportId).SingleAsync(), IsActive = true });
            await db.SaveChangesAsync();
        }

        await transaction.CommitAsync();
        return user.UserId;
    }

    /// <summary>
    /// Включить или отключить курьера (отключённого нельзя назначать на заказы).
    /// </summary>
    public static async Task SetCourierActiveAsync(int courierId, bool active)
    {
        Session.RequireAdmin();
        using var db = Db.Create();
        var courier = await db.Couriers.FindAsync(courierId)
                      ?? throw new AppException("Курьер не найден.");
        courier.IsActive = active;
        await db.SaveChangesAsync();
    }
}
