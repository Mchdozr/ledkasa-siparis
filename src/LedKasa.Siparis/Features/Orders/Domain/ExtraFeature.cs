namespace LedKasa.Siparis.Features.Orders.Domain;

public sealed class ExtraFeature
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public int SortOrder { get; private set; }

    private ExtraFeature()
    {
    }

    public static ExtraFeature Create(string name, int sortOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Özellik adı zorunludur.");

        return new ExtraFeature
        {
            Name = name.Trim(),
            IsActive = true,
            SortOrder = sortOrder
        };
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Özellik adı zorunludur.");
        Name = name.Trim();
    }

    public void SetActive(bool isActive) => IsActive = isActive;

    public void SetSortOrder(int sortOrder) => SortOrder = sortOrder;
}
