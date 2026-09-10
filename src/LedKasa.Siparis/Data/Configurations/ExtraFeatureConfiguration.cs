using LedKasa.Siparis.Features.Orders.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedKasa.Siparis.Data.Configurations;

public sealed class ExtraFeatureConfiguration : IEntityTypeConfiguration<ExtraFeature>
{
    public void Configure(EntityTypeBuilder<ExtraFeature> builder)
    {
        builder.ToTable("ExtraFeatures");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
        builder.Property(x => x.IsActive).HasDefaultValue(true);

        builder.HasData(
            new { Id = 1, Name = "Köşe kesim", IsActive = true, SortOrder = 1 },
            new { Id = 2, Name = "Delik", IsActive = true, SortOrder = 2 },
            new { Id = 3, Name = "Askı aparatı", IsActive = true, SortOrder = 3 },
            new { Id = 4, Name = "Su oluğu", IsActive = true, SortOrder = 4 },
            new { Id = 5, Name = "Özel renk", IsActive = true, SortOrder = 5 },
            new { Id = 6, Name = "Modül yönü değişikliği", IsActive = true, SortOrder = 6 });
    }
}
