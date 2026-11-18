using System;
using System.Collections.Generic;

namespace CourierDelivery.Models;

public partial class Courier
{
    public int CourierId { get; set; }

    public int UserId { get; set; }

    public int TransportId { get; set; }

    public Transport Transport { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual User User { get; set; } = null!;
}
