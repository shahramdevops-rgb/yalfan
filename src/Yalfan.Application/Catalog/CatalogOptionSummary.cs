namespace Yalfan.Application.Catalog;

/// <summary>A category, fabric, colour or size as the panel shows it: its id and Persian name.</summary>
public sealed record CatalogOptionSummary(Guid Id, string Name);
