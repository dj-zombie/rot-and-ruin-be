using RotAndRuin.Domain.Common;

namespace RotAndRuin.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid? ParentCategoryId { get; set; }
    public Category? ParentCategory { get; set; }


    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;


    public ICollection<Category> SubCategories { get; set; } = new List<Category>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
}