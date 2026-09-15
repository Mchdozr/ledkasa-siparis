using LedKasa.Siparis.Features.Orders.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedKasa.Siparis.Data.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProductId).IsRequired();
        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.WidthCm).IsRequired();
        builder.Property(x => x.HeightCm).IsRequired();
        builder.Property(x => x.DepthCm).IsRequired();
        builder.Property(x => x.Side).IsRequired();
        builder.Property(x => x.Note).HasMaxLength(400);
    }
}
