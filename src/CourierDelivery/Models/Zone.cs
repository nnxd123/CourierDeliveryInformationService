using System;
using System.Collections.Generic;

namespace CourierDelivery.Models;

public partial class Zone
{
    public int ZoneId { get; set; }

    public string Name { get; set; } = null!;

    public decimal RatePerKg { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
