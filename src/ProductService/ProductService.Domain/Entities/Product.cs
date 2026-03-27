using Shared.Domain.BaseEntities;

namespace ProductService.Domain.Entities;

/// Ürün domain entity'si.
public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; } = true;

    public Product() { }


    public static Product Create(string name, string description, decimal price, int stock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (price < 0) throw new ArgumentException("Fiyat negatif olamaz.", nameof(price));
        if (stock < 0) throw new ArgumentException("Stok negatif olamaz.", nameof(stock));

        return new Product
        {
            Id          = Guid.NewGuid(),
            Name        = name,
            Description = description,
            Price       = price,
            Stock       = stock,
            IsActive    = true,
            CreatedAt   = DateTime.UtcNow
        };
    }
    public void Update(string name, string description, decimal price, int stock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (price < 0) throw new ArgumentException("Fiyat negatif olamaz.", nameof(price));
        if (stock < 0) throw new ArgumentException("Stok negatif olamaz.", nameof(stock));

        Name        = name;
        Description = description;
        Price       = price;
        Stock       = stock;
        UpdatedAt   = DateTime.UtcNow;
    }

    public void Deactivate() => IsActive = false;
    public void Activate()   => IsActive = true;
}
