using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Merkatto.Infrastructure.Persistence.Converters;

/// <summary>
/// Stores decimal values as long integers scaled by 10,000 (4 fixed decimal places).
/// This makes SQL aggregations (SUM, GROUP BY, ORDER BY) work correctly on both
/// PostgreSQL (BIGINT) and SQLite (INTEGER), avoiding SQLite's lack of a native decimal type.
///
/// Examples:
///   S/ 1.50  → 15_000   |  0.0350 rate → 350  |  5.000 units → 50_000
/// </summary>
public sealed class FixedPoint4Converter() : ValueConverter<decimal, long>(
    v => (long)Math.Round(v * 10_000m, MidpointRounding.AwayFromZero),
    v => v / 10_000m);
