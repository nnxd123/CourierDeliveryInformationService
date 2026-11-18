using System;
using System.Collections.Generic;

namespace CourierDelivery.Models;

public partial class OrderAddress
{
    public int AddressId { get; set; }

    public int OrderId { get; set; }

    public byte StopNumber { get; set; }

    public string Address { get; set; } = null!;

    public string ContactName { get; set; } = null!;

    public string ContactPhone { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;
}
