using Yalfan.Domain.Common;

namespace Yalfan.Domain.Catalog;

public static class CatalogErrors
{
    public static readonly Error NameRequired = Error.Validation("catalog.name_required");

    public static readonly Error NameTooLong = Error.Validation("catalog.name_too_long");

    public static readonly Error DescriptionTooLong = Error.Validation("catalog.description_too_long");

    /// <summary>Two sizes of one model with the same name.</summary>
    public static readonly Error SizeNameDuplicate = Error.Validation("catalog.size_name_duplicate");

    /// <summary>
    /// Another model, category, fabric or colour already has this Persian name. The screen the
    /// request came from says which one.
    /// </summary>
    public static readonly Error NameTaken = Error.Conflict("catalog.name_taken");

    /// <summary>The model already has a variant with this fabric, colour and size.</summary>
    public static readonly Error VariantExists = Error.Conflict("catalog.variant_exists");

    /// <summary>The model has sizes, so every variant must name one of them.</summary>
    public static readonly Error SizeRequired = Error.Rule("catalog.size_required");

    /// <summary>The model has no sizes, so a variant cannot name one.</summary>
    public static readonly Error SizeNotAllowed = Error.Rule("catalog.size_not_allowed");

    public static readonly Error ProductNotFound = Error.NotFound("catalog.product_not_found");

    public static readonly Error CategoryNotFound = Error.NotFound("catalog.category_not_found");

    public static readonly Error FabricNotFound = Error.NotFound("catalog.fabric_not_found");

    public static readonly Error ColourNotFound = Error.NotFound("catalog.colour_not_found");

    /// <summary>A new model in a deactivated category (ADR 0022).</summary>
    public static readonly Error CategoryInactive = Error.Rule("catalog.category_inactive");

    /// <summary>A new variant in a deactivated fabric; the variants already in it stay (ADR 0022).</summary>
    public static readonly Error FabricInactive = Error.Rule("catalog.fabric_inactive");

    /// <summary>A new variant in a deactivated colour; the variants already in it stay (ADR 0022).</summary>
    public static readonly Error ColourInactive = Error.Rule("catalog.colour_inactive");

    /// <summary>The size is not one of this model's sizes.</summary>
    public static readonly Error SizeNotFound = Error.NotFound("catalog.size_not_found");
}
