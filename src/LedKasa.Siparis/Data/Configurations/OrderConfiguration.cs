using LedKasa.Siparis.Features.Orders.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedKasa.Siparis.Data.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrderNumber).HasMaxLength(24).IsRequired();
        builder.HasIndex(x => x.OrderNumber).IsUnique();

        builder.Property(x => x.CustomerName).HasMaxLength(160).IsRequired();
        builder.HasIndex(x => x.CustomerName);

        builder.Property(x => x.OrderDate).IsRequired();
        builder.Property(x => x.DeliveryDate).IsRequired();
        builder.HasIndex(x => x.OrderDate);
        builder.HasIndex(x => x.DeliveryDate);
        builder.HasIndex(x => new { x.OrderDate, x.DeliveryPlace });

        builder.Property(x => x.DeliveryPlace).HasConversion<int>().IsRequired();
        builder.Property(x => x.Currency).HasConversion<int>().IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .HasField("_status")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .IsRequired();
        builder.HasIndex(x => x.Status);

        builder.Property(x => x.GrandTotal).HasPrecision(18, 2);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.CreatedByUserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.UpdatedByUserId).HasMaxLength(450);

        builder.Property(x => x.RowVersion)
            .IsConcurrencyToken()
            .ValueGeneratedOnAddOrUpdate();

        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Items)
            .HasField("_items")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
