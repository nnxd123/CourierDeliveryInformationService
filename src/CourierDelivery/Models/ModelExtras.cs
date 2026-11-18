using System.ComponentModel.DataAnnotations.Schema;
using CourierDelivery.Services;

namespace CourierDelivery.Models;

// Дополнительные свойства для отображения данных в интерфейсе.
// Классы partial: вторая часть класса сгенерирована по базе данных.

public partial class User
{
    // «Иванова Мария»
    public string FullName => $"{LastName} {FirstName}".Trim();

    // Телефон в читаемом виде: +7 (921) 000-00-06
    public string PhoneText => PhoneFormat.Pretty(Phone);

    public override string ToString() => $"{FullName} ({Login})";
}

public partial class Zone
{
    public string RateText => $"{RatePerKg:N0} руб./кг";

    public override string ToString() => $"{Name} — {RatePerKg:N0} руб./кг";
}

public partial class Status
{
    public override string ToString() => Name;
}

public partial class Courier
{
    // Количество активных заказов (статус «В работе»). Заполняет сервис при загрузке.
    [NotMapped]
    public int ActiveOrders { get; set; }

    public string FullName => User?.FullName ?? "Курьер";

    // Первая буква фамилии для круглого значка
    public string Initial => FullName.Length > 0 ? FullName[..1] : "?";

    public string LoadText => $"{ActiveOrders} из {PriceRules.MaxActiveOrders}";

    public bool IsFull => ActiveOrders >= PriceRules.MaxActiveOrders;

    public override string ToString() => $"{FullName} · {Transport} · загрузка {LoadText}";
}

public partial class Order
{
    public string Number => $"№ {OrderId:D5}";

    public string StatusName => Status?.Name ?? "";

    public string ClientName => Client?.FullName ?? "";

    public string CourierName => Courier?.User?.FullName ?? "не назначен";

    public string ZoneName => Zone?.Name ?? "";

    public string WeightText => $"{Weight:0.##} кг";

    public string DateText => $"{OrderDate:dd.MM.yyyy HH:mm}";

    public string CostText => $"{TotalCost:N2} руб.";

    public OrderAddress? Pickup =>
        OrderAddresses.FirstOrDefault(a => a.AddressType == OrderService.AddressPickup);

    public OrderAddress? Delivery =>
        OrderAddresses.FirstOrDefault(a => a.AddressType == OrderService.AddressDelivery);

    public string Route => $"{Pickup?.Address ?? "—"}  →  {Delivery?.Address ?? "—"}";

    // Флаги для цвета плашки статуса (Classes.work="{Binding IsWork}")
    public bool IsWork => StatusName == OrderService.StatusWork;
    public bool IsDone => StatusName == OrderService.StatusDone;
    public bool IsCancelled => StatusName == OrderService.StatusCancelled;
}

// Форматирование телефона: в БД хранятся только цифры.
public static class PhoneFormat
{
    public static string Pretty(string? digits)
    {
        if (string.IsNullOrEmpty(digits) || digits.Length != 11)
            return digits ?? "";
        return $"+{digits[0]} ({digits.Substring(1, 3)}) {digits.Substring(4, 3)}-{digits.Substring(7, 2)}-{digits.Substring(9, 2)}";
    }
}
