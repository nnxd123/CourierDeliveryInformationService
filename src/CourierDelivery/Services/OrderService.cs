using System.Data;
using CourierDelivery.Data;
using CourierDelivery.Models;
using Microsoft.EntityFrameworkCore;

namespace CourierDelivery.Services;

// Данные новой заявки, введённые на форме «Новый заказ».
public class OrderDraft
{
    public int ClientId { get; set; }
    public int ZoneId { get; set; }
    public decimal Weight { get; set; }
    public bool IsUrgent { get; set; }
    public string PickupAddress { get; set; } = "";
    public string PickupContact { get; set; } = "";
    public string PickupPhone { get; set; } = "";
    public string DeliveryAddress { get; set; } = "";
    public string DeliveryContact { get; set; } = "";
    public string DeliveryPhone { get; set; } = "";
    public string? Comment { get; set; }
}

// Бизнес-логика заказов: оформление, назначение курьера, статусы, удаление.
public static class OrderService
{
    // Названия статусов — точно как в таблице Statuses
    public const string StatusNew = "Новый";
    public const string StatusWork = "В работе";
    public const string StatusDone = "Выполнен";
    public const string StatusCancelled = "Отменён";

    // Типы адресов — как в ограничении CHECK таблицы OrderAddresses

    // Запрос заказов вместе со всеми связанными данными
    private static IQueryable<Order> WithDetails(DeliveryContext db) => db.Orders
        .Include(o => o.Client)
        .Include(o => o.Zone)
        .Include(o => o.Status)
        .Include(o => o.Courier).ThenInclude(c => c!.User)
        .Include(o => o.Courier).ThenInclude(c => c!.Transport)
        .Include(o => o.OrderAddresses)
        .AsNoTracking();

    /// <summary>
    /// Список заказов с учётом роли: клиент — свои, курьер — назначенные ему,
    /// оператор и администратор — все заказы.
    /// </summary>
    public static async Task<List<Order>> GetOrdersAsync()
    {
        Session.RequireLogin();
        using var db = Db.Create();
        var query = WithDetails(db);

        var user = Session.CurrentUser!;
        if (Session.IsClient)
            query = query.Where(o => o.ClientId == user.UserId);
        else if (Session.IsCourier)
            query = query.Where(o => o.Courier != null && o.Courier.UserId == user.UserId);

        return await query.OrderByDescending(o => o.OrderDate).ToListAsync();
    }

    /// <summary>
    /// Один заказ с проверкой права доступа.
    /// </summary>
    public static async Task<Order> GetOrderAsync(int orderId)
    {
        Session.RequireLogin();
        using var db = Db.Create();

        var order = await WithDetails(db).FirstOrDefaultAsync(o => o.OrderId == orderId)
                    ?? throw new AppException("Заказ не найден: возможно, он уже удалён.");

        var user = Session.CurrentUser!;
        if (Session.IsClient && order.ClientId != user.UserId)
            throw new AppException("Нет доступа к этому заказу.");
        if (Session.IsCourier && order.Courier?.UserId != user.UserId)
            throw new AppException("Заказ назначен другому курьеру.");

        return order;
    }

    /// <summary>
    /// Скидка клиента: 10 %, если за ПРЕДЫДУЩИЙ календарный месяц
    /// у него выполнено более 5 заказов.
    /// </summary>
    public static async Task<int> GetDiscountAsync(int clientId)
    {
        using var db = Db.Create();
        return await GetDiscountAsync(db, clientId);
    }

    private static async Task<int> GetDiscountAsync(DeliveryContext db, int clientId)
    {
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var prevMonthStart = monthStart.AddMonths(-1);

        int completed = await db.Orders.CountAsync(o =>
            o.ClientId == clientId &&
            o.Status.Name == StatusDone &&
            o.OrderDate >= prevMonthStart && o.OrderDate < monthStart);

        return PriceRules.DiscountFor(completed);
    }

    /// <summary>
    /// Оформление заказа. Стоимость пересчитывается на сервере по тарифу зоны,
    /// заказ и два адреса сохраняются одной транзакцией.
    /// </summary>
    public static async Task<int> CreateOrderAsync(OrderDraft draft)
    {
        Session.RequireLogin();
        if (Session.IsCourier)
            throw new AppException("Курьер не может оформлять заказы.");

        // Клиент оформляет заказ только на себя
        if (Session.IsClient)
            draft.ClientId = Session.CurrentUser!.UserId;
        if (draft.ClientId == 0)
            throw new AppException("Выберите клиента.");

        PriceRules.CheckWeight(draft.Weight);
        RequireText(draft.PickupAddress, "адрес отправления");
        RequireText(draft.PickupContact, "отправителя");
        RequireText(draft.DeliveryAddress, "адрес получения");
        RequireText(draft.DeliveryContact, "получателя");
        string pickupPhone = Digits(draft.PickupPhone, "отправителя");
        string deliveryPhone = Digits(draft.DeliveryPhone, "получателя");

        using var db = Db.Create();
        using var transaction = await db.Database.BeginTransactionAsync();

        var zone = await db.Zones.FindAsync(draft.ZoneId)
                   ?? throw new AppException("Выберите зону доставки.");
        int newStatusId = await StatusIdAsync(db, StatusNew);
        int discount = await GetDiscountAsync(db, draft.ClientId);

        var order = new Order
        {
            OrderDate = DateTime.Now,
            ClientId = draft.ClientId,
            ZoneId = zone.ZoneId,
            StatusId = newStatusId,
            Weight = draft.Weight,
            IsUrgent = draft.IsUrgent,
            DiscountPercent = discount,
            TotalCost = PriceRules.Total(zone.RatePerKg, draft.Weight, draft.IsUrgent, discount),
            Comment = string.IsNullOrWhiteSpace(draft.Comment) ? null : draft.Comment.Trim()
        };
        order.OrderAddresses.Add(new OrderAddress
        {
            StopNumber = 1,
            Address = draft.PickupAddress.Trim(),
            ContactName = draft.PickupContact.Trim(),
            ContactPhone = pickupPhone
        });
        order.OrderAddresses.Add(new OrderAddress
        {
            StopNumber = 2,
            Address = draft.DeliveryAddress.Trim(),
            ContactName = draft.DeliveryContact.Trim(),
            ContactPhone = deliveryPhone
        });

        db.Orders.Add(order);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return order.OrderId;
    }

    /// <summary>
    /// Назначение курьера (оператор, администратор). Загрузка курьера проверяется
    /// внутри транзакции Serializable, чтобы два оператора не превысили лимит.
    /// </summary>
    public static async Task AssignCourierAsync(int orderId, int courierId)
    {
        Session.RequireDispatcher();
        using var db = Db.Create();
        using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var order = await db.Orders.Include(o => o.Status).FirstOrDefaultAsync(o => o.OrderId == orderId)
                    ?? throw new AppException("Заказ не найден: возможно, он уже удалён.");
        if (order.Status.Name is StatusDone or StatusCancelled)
            throw new AppException($"Заказ в статусе «{order.Status.Name}»: курьера назначить нельзя.");

        var courier = await db.Couriers.Include(c => c.User).FirstOrDefaultAsync(c => c.CourierId == courierId)
                      ?? throw new AppException("Курьер не найден.");
        if (!courier.IsActive)
            throw new AppException("Курьер отключён и не принимает заказы.");

        int active = await db.Orders.CountAsync(o =>
            o.CourierId == courierId && o.OrderId != orderId && o.Status.Name == StatusWork);
        if (active >= PriceRules.MaxActiveOrders)
            throw new AppException($"У курьера {courier.User.FullName} уже {active} активных заказов.");

        order.CourierId = courierId;
        order.StatusId = await StatusIdAsync(db, StatusWork);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    /// <summary>
    /// Смена статуса. Оператор/администратор — любой статус по правилам,
    /// курьер — только «Выполнен» для своего заказа в работе.
    /// </summary>
    public static async Task ChangeStatusAsync(int orderId, string newStatus)
    {
        Session.RequireLogin();
        using var db = Db.Create();

        var order = await db.Orders
                        .Include(o => o.Status)
                        .Include(o => o.Courier)
                        .FirstOrDefaultAsync(o => o.OrderId == orderId)
                    ?? throw new AppException("Заказ не найден: возможно, он уже удалён.");
        string current = order.Status.Name;

        if (Session.IsCourier)
        {
            if (order.Courier?.UserId != Session.CurrentUser!.UserId)
                throw new AppException("Заказ назначен другому курьеру.");
            if (newStatus != StatusDone || current != StatusWork)
                throw new AppException("Курьер может только отметить заказ в работе как выполненный.");
        }
        else
        {
            Session.RequireDispatcher();
        }

        if (current == newStatus)
            throw new AppException($"Заказ уже в статусе «{current}».");
        if (current is StatusDone or StatusCancelled)
            throw new AppException($"Заказ в статусе «{current}» больше не изменяется.");
        if (newStatus is StatusWork or StatusDone && order.CourierId == null)
            throw new AppException("Сначала назначьте курьера.");
        if (newStatus == StatusNew)
            order.CourierId = null; // возврат в «Новый» снимает назначение

        order.StatusId = await StatusIdAsync(db, newStatus);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Отмена заказа клиентом — только пока заказ не подтверждён (статус «Новый»).
    /// </summary>
    public static async Task CancelByClientAsync(int orderId)
    {
        Session.RequireLogin();
        using var db = Db.Create();

        var order = await db.Orders.Include(o => o.Status).FirstOrDefaultAsync(o => o.OrderId == orderId)
                    ?? throw new AppException("Заказ не найден.");
        if (order.ClientId != Session.CurrentUser!.UserId)
            throw new AppException("Нет доступа к этому заказу.");
        if (order.Status.Name != StatusNew)
            throw new AppException("Заказ уже подтверждён: для отмены обратитесь к оператору.");

        order.StatusId = await StatusIdAsync(db, StatusCancelled);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Удаление заказа (только администратор). Курьер освобождается автоматически:
    /// загрузка считается по существующим заказам. Повторное удаление того же
    /// заказа ничего не освобождает — заказа уже нет.
    /// Возвращает ФИО курьера, у которого освободилось место (или null).
    /// </summary>
    public static async Task<string?> DeleteOrderAsync(int orderId)
    {
        Session.RequireAdmin();
        using var db = Db.Create();

        var order = await db.Orders
            .Include(o => o.Status)
            .Include(o => o.Courier).ThenInclude(c => c!.User)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
            throw new AppException("Заказ уже удалён: повторное удаление не выполняется.");

        string? freed = order.Status.Name == StatusWork ? order.Courier?.User.FullName : null;

        db.Orders.Remove(order); // адреса удаляются каскадно (ON DELETE CASCADE)
        await db.SaveChangesAsync();
        return freed;
    }

    public static async Task<List<Status>> GetStatusesAsync()
    {
        using var db = Db.Create();
        return await db.Statuses.AsNoTracking().OrderBy(s => s.StatusId).ToListAsync();
    }

    // ---------- вспомогательные методы ----------

    private static async Task<int> StatusIdAsync(DeliveryContext db, string name) =>
        await db.Statuses.Where(s => s.Name == name).Select(s => s.StatusId).FirstOrDefaultAsync() is int id and > 0
            ? id
            : throw new AppException($"Статус «{name}» не найден в базе данных.");

    private static void RequireText(string value, string what)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new AppException($"Укажите {what}.");
    }

    private static string Digits(string phone, string who)
    {
        string digits = new string((phone ?? "").Where(char.IsDigit).ToArray());
        if (digits.Length == 10) digits = "7" + digits;
        if (digits.Length == 11 && digits[0] == '8') digits = "7" + digits[1..];
        if (digits.Length != 11)
            throw new AppException($"Телефон {who} должен содержать 11 цифр.");
        return digits;
    }
}
