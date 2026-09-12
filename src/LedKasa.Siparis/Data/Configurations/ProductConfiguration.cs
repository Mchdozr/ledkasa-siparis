using LedKasa.Siparis.Features.Orders.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedKasa.Siparis.Data.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
        builder.Property(x => x.IsActive).HasDefaultValue(true);

        builder.HasData(
            new { Id = 1, Name = "CNC LED Kasa", IsActive = true, SortOrder = 1 },
            new { Id = 2, Name = "Kapaksız LED Kabinet", IsActive = true, SortOrder = 2 },
            new { Id = 3, Name = "Rental LED Kabinet", IsActive = true, SortOrder = 3 },
            new { Id = 4, Name = "Poster LED Kasa", IsActive = true, SortOrder = 4 },
            new { Id = 5, Name = "Katlanabilir Poster LED Kasa", IsActive = true, SortOrder = 5 });
    }
}
