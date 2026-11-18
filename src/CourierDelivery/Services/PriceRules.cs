namespace CourierDelivery.Services;

// Бизнес-правила расчёта стоимости и ограничения службы доставки.
public static class PriceRules
{
    // Максимальный вес груза для стандартной доставки, кг
    public const decimal MaxWeight = 30m;

    // Коэффициент срочной доставки
    public const decimal UrgentFactor = 1.5m;

    // Скидка постоянному клиенту, %
    public const int RegularDiscount = 10;

    // Скидка даётся, если за прошлый месяц выполнено БОЛЕЕ 5 заказов
    public const int RegularOrdersThreshold = 5;

    // Курьер с 10 активными заказами больше не назначается
    public const int MaxActiveOrders = 10;

    /// <summary>
    /// Проверка веса груза: больше 0 и не больше 30 кг.
    /// </summary>
    public static void CheckWeight(decimal weight)
    {
        if (weight <= 0)
            throw new AppException("Вес груза должен быть больше 0 кг.");
        if (weight > MaxWeight)
            throw new AppException($"Вес груза не может превышать {MaxWeight:0} кг для стандартной доставки.");
    }

    /// <summary>
    /// Стоимость без скидки: тариф зоны × вес × коэффициент срочности.
    /// </summary>
    public static decimal BaseCost(decimal ratePerKg, decimal weight, bool urgent)
    {
        decimal factor = urgent ? UrgentFactor : 1m;
        return Math.Round(ratePerKg * weight * factor, 2);
    }

    /// <summary>
    /// Итоговая стоимость: P = T × m × k × (1 − d / 100).
    /// </summary>
    public static decimal Total(decimal ratePerKg, decimal weight, bool urgent, int discountPercent)
    {
        decimal cost = BaseCost(ratePerKg, weight, urgent);
        return Math.Round(cost * (100 - discountPercent) / 100m, 2);
    }

    /// <summary>
    /// Размер скидки по числу выполненных заказов клиента за прошлый месяц.
    /// </summary>
    public static int DiscountFor(int completedLastMonth) =>
        completedLastMonth > RegularOrdersThreshold ? RegularDiscount : 0;
}
