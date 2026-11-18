using System;
using System.Collections.Generic;

namespace CourierDelivery.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public DateTime OrderDate { get; set; }

    public int ClientId { get; set; }

    public int ZoneId { get; set; }

    public int StatusId { get; set; }

    public int? CourierId { get; set; }

    public decimal Weight { get; set; }

    public bool IsUrgent { get; set; }

    public int DiscountPercent { get; set; }

    public decimal TotalCost { get; set; }

    public string? Comment { get; set; }

    public virtual User Client { get; set; } = null!;

    public virtual Courier? Courier { get; set; }

    public virtual ICollection<OrderAddress> OrderAddresses { get; set; } = new List<OrderAddress>();

    public virtual Status Status { get; set; } = null!;

    public virtual Zone Zone { get; set; } = null!;
}
