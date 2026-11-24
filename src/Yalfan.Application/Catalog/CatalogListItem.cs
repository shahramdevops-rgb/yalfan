namespace Yalfan.Application.Catalog;

/// <summary>
/// A category, fabric or colour in its own list screen: its id, Persian name and whether it is
/// active (ADR 0022). Separate from <see cref="CatalogOptionSummary"/>, which also describes a
/// model's sizes, and sizes have no such flag.
/// </summary>
public sealed record CatalogListItem(Guid Id, string Name, bool IsActive);
