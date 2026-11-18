using System;
using System.Collections.Generic;
using CourierDelivery.Models;
using Microsoft.EntityFrameworkCore;

namespace CourierDelivery.Data;

// Контекст базы данных CourierDelivery (сгенерирован по существующей БД — Database First).
public partial class DeliveryContext : DbContext
{
    public DeliveryContext(DbContextOptions<DeliveryContext> options)
        : base(options)
    {
    }

    public DbSet<Transport> Transports => Set<Transport>();

    public virtual DbSet<Courier> Couriers { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderAddress> OrderAddresses { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Status> Statuses { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Zone> Zones { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transport>(entity =>
        {
            entity.Property(t => t.TransportId).HasColumnName("transport_id");
            entity.Property(t => t.Name).HasColumnName("name").HasMaxLength(50);
            entity.HasIndex(t => t.Name).IsUnique();
        });

        modelBuilder.Entity<Courier>(entity =>
        {
            entity.HasKey(e => e.CourierId);

            entity.HasIndex(e => e.UserId).IsUnique();

            entity.Property(e => e.CourierId).HasColumnName("courier_id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.TransportId).HasColumnName("transport_id");
            entity.HasOne(e => e.Transport).WithMany().HasForeignKey(e => e.TransportId);
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithOne(p => p.Courier)
                .HasForeignKey<Courier>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId);

            entity.HasIndex(e => new { e.CourierId, e.StatusId }, "ix_orders_courier");
            entity.HasIndex(e => e.OrderDate, "ix_orders_date");

            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.ClientId).HasColumnName("client_id");
            entity.Property(e => e.Comment)
                .HasMaxLength(500)
                .HasColumnName("comment");
            entity.Property(e => e.CourierId).HasColumnName("courier_id");
            entity.Property(e => e.DiscountPercent).HasColumnName("discount_percent");
            entity.Property(e => e.IsUrgent).HasColumnName("is_urgent");
            entity.Property(e => e.OrderDate)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("order_date");
            entity.Property(e => e.StatusId).HasColumnName("status_id");
            entity.Property(e => e.TotalCost)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("total_cost");
            entity.Property(e => e.Weight)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("weight");
            entity.Property(e => e.ZoneId).HasColumnName("zone_id");

            entity.HasOne(d => d.Client).WithMany(p => p.Orders)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Courier).WithMany(p => p.Orders)
                .HasForeignKey(d => d.CourierId);

            entity.HasOne(d => d.Status).WithMany(p => p.Orders)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Zone).WithMany(p => p.Orders)
                .HasForeignKey(d => d.ZoneId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<OrderAddress>(entity =>
        {
            entity.HasKey(e => e.AddressId);

            entity.HasIndex(e => new { e.OrderId, e.StopNumber }, "uq_order_address").IsUnique();

            entity.Property(e => e.AddressId).HasColumnName("address_id");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .HasColumnName("address");
            entity.Property(e => e.StopNumber)
                .HasColumnName("stop_number");
            entity.Property(e => e.ContactName)
                .HasMaxLength(150)
                .HasColumnName("contact_name");
            entity.Property(e => e.ContactPhone)
                .HasMaxLength(20)
                .HasColumnName("contact_phone");
            entity.Property(e => e.OrderId).HasColumnName("order_id");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderAddresses)
                .HasForeignKey(d => d.OrderId);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId);

            entity.HasIndex(e => e.RoleName).IsUnique();

            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.RoleName)
                .HasMaxLength(50)
                .HasColumnName("role_name");
        });

        modelBuilder.Entity<Status>(entity =>
        {
            entity.HasKey(e => e.StatusId);

            entity.HasIndex(e => e.Name).IsUnique();

            entity.Property(e => e.StatusId).HasColumnName("status_id");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId);

            entity.HasIndex(e => e.Login).IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.FirstName)
                .HasMaxLength(100)
                .HasColumnName("first_name");
            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .HasColumnName("last_name");
            entity.Property(e => e.Login)
                .HasMaxLength(50)
                .HasColumnName("login");
            entity.Property(e => e.Password)
                .HasMaxLength(50)
                .HasColumnName("password");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.RoleId).HasColumnName("role_id");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Zone>(entity =>
        {
            entity.HasKey(e => e.ZoneId);

            entity.HasIndex(e => e.Name).IsUnique();

            entity.Property(e => e.ZoneId).HasColumnName("zone_id");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.RatePerKg)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("rate_per_kg");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
