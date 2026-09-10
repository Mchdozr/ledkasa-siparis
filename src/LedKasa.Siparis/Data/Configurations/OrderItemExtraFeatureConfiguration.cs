using LedKasa.Siparis.Features.Orders.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedKasa.Siparis.Data.Configurations;

public sealed class OrderItemExtraFeatureConfiguration : IEntityTypeConfiguration<OrderItemExtraFeature>
{
    public void Configure(EntityTypeBuilder<OrderItemExtraFeature> builder)
    {
        builder.ToTable("OrderItemExtraFeatures");
        builder.HasKey(x => new { x.OrderItemId, x.ExtraFeatureId });

        builder.HasOne(x => x.ExtraFeature)
            .WithMany()
            .HasForeignKey(x => x.ExtraFeatureId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
