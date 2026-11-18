using CourierDelivery.Data;
using CourierDelivery.Models;
using Microsoft.EntityFrameworkCore;

namespace CourierDelivery.Services;

// Справочник зон доставки и тарифов.
public static class ZoneService
{
    public static async Task<List<Zone>> GetZonesAsync()
    {
        using var db = Db.Create();
        return await db.Zones.AsNoTracking().OrderBy(z => z.RatePerKg).ToListAsync();
    }

    /// <summary>
    /// Добавить (zoneId = 0) или изменить зону. Только администратор.
    /// </summary>
    public static async Task SaveZoneAsync(int zoneId, string name, decimal rate, string? description)
    {
        Session.RequireAdmin();

        name = (name ?? "").Trim();
        if (name == "")
            throw new AppException("Укажите название зоны.");
        if (rate <= 0)
            throw new AppException("Тариф за 1 кг должен быть больше нуля.");

        using var db = Db.Create();

        if (await db.Zones.AnyAsync(z => z.Name == name && z.ZoneId != zoneId))
            throw new AppException("Зона с таким названием уже есть.");

        Zone zone;
        if (zoneId == 0)
        {
            zone = new Zone();
            db.Zones.Add(zone);
        }
        else
        {
            zone = await db.Zones.FindAsync(zoneId) ?? throw new AppException("Зона не найдена.");
        }

        zone.Name = name;
        zone.RatePerKg = rate;
        zone.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        // Новый тариф действует только для новых заказов: в заказах хранится
        // уже рассчитанная стоимость (total_cost), она не пересчитывается.
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Удалить зону, если на неё нет заказов.
    /// </summary>
    public static async Task DeleteZoneAsync(int zoneId)
    {
        Session.RequireAdmin();
        using var db = Db.Create();

        if (await db.Orders.AnyAsync(o => o.ZoneId == zoneId))
            throw new AppException("Нельзя удалить зону: на неё оформлены заказы.");

        var zone = await db.Zones.FindAsync(zoneId) ?? throw new AppException("Зона уже удалена.");
        db.Zones.Remove(zone);
        await db.SaveChangesAsync();
    }
}
