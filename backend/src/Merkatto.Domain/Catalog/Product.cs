using Merkatto.Domain.Common;

namespace Merkatto.Domain.Catalog;

/// <summary>
/// A product is bought in a purchase unit (e.g. "paquete") that contains
/// <see cref="UnitsPerPurchaseUnit"/> sale units (e.g. 6 "unidad"), and sold by the sale unit.
///
/// Example (galleta soda): bought at S/6 per paquete of 6 → UnitCost = S/1; sold at S/1.50 →
/// Margin = S/0.50 per unidad. All stock fields are expressed in <see cref="SaleUnit"/>.
/// </summary>
public class Product : BaseEntity
{
    public required string Name { get; set; }
    public string? InternalCode { get; set; }

    public long CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public long? BrandId { get; set; }
    public Brand? Brand { get; set; }

    // --- Purchase side ---
    /// <summary>Unit the product is bought in, e.g. "paquete", "caja", "fardo", "maple".</summary>
    public required string PurchaseUnit { get; set; }
    /// <summary>Cost of one whole purchase unit (e.g. S/6 per paquete).</summary>
    public decimal LastPurchaseCost { get; set; }
    /// <summary>How many sale units a purchase unit contains (e.g. 6). Must be &gt;= 1.</summary>
    public int UnitsPerPurchaseUnit { get; set; } = 1;

    // --- Sale side ---
    /// <summary>Unit the product is sold in, e.g. "unidad".</summary>
    public required string SaleUnit { get; set; }
    /// <summary>Sale price per <see cref="SaleUnit"/>.</summary>
    public decimal SalePrice { get; set; }

    // --- Inventory (in sale units) ---
    public decimal WarehouseStock { get; set; }
    public decimal CounterStock { get; set; }
    public decimal MinStock { get; set; }

    public bool IsActive { get; set; } = true;

    // --- Derived (not mapped) ---
    public decimal UnitCost => UnitsPerPurchaseUnit > 0 ? LastPurchaseCost / UnitsPerPurchaseUnit : 0m;
    public decimal Margin => SalePrice - UnitCost;
    public decimal MarginRate => SalePrice > 0 ? Margin / SalePrice : 0m;
    public decimal TotalStock => WarehouseStock + CounterStock;
    public bool IsLowStock => TotalStock <= MinStock;
}
