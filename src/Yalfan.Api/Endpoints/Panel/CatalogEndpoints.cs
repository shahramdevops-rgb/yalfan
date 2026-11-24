using Microsoft.AspNetCore.Http.HttpResults;
using Yalfan.Api.Common.Endpoints;
using Yalfan.Api.Common.Errors;
using Yalfan.Api.Common.Security;
using Yalfan.Application.Catalog;
using Yalfan.Application.Catalog.AddSize;
using Yalfan.Application.Catalog.AddVariants;
using Yalfan.Application.Catalog.CreateCategory;
using Yalfan.Application.Catalog.CreateColour;
using Yalfan.Application.Catalog.CreateFabric;
using Yalfan.Application.Catalog.CreateProduct;
using Yalfan.Application.Catalog.GetProduct;
using Yalfan.Application.Catalog.ListCategories;
using Yalfan.Application.Catalog.ListColours;
using Yalfan.Application.Catalog.ListFabrics;
using Yalfan.Application.Catalog.RenameCategory;
using Yalfan.Application.Catalog.RenameColour;
using Yalfan.Application.Catalog.RenameFabric;
using Yalfan.Application.Catalog.RenameSize;
using Yalfan.Application.Catalog.ReorderSizes;
using Yalfan.Application.Catalog.SearchProducts;
using Yalfan.Application.Catalog.SetCategoryActive;
using Yalfan.Application.Catalog.SetColourActive;
using Yalfan.Application.Catalog.SetFabricActive;
using Yalfan.Application.Catalog.SetProductActive;
using Yalfan.Application.Catalog.SetVariantActive;
using Yalfan.Application.Catalog.UpdateProduct;
using Yalfan.Application.Common.Handlers;
using Yalfan.Application.Common.Paging;

namespace Yalfan.Api.Endpoints.Panel;

/// <summary>
/// Models, their variants, and the lists they are built from. Every staff role may use them,
/// because the operator enters the models (roadmap 1.6).
/// </summary>
public sealed class CatalogEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var catalog = endpoints.MapGroup("/panel/catalog").WithTags("Catalog").RequireAuthorization(Policies.Staff);

        MapCategories(catalog.MapGroup("/categories"));
        MapFabrics(catalog.MapGroup("/fabrics"));
        MapColours(catalog.MapGroup("/colours"));
        MapProducts(catalog.MapGroup("/products"));
    }

    private static void MapCategories(RouteGroupBuilder categories)
    {
        categories.MapGet("/", async Task<Results<Ok<IReadOnlyList<CatalogListItem>>, ProblemHttpResult>> (
                IQueryHandler<ListCategoriesQuery, IReadOnlyList<CatalogListItem>> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new ListCategoriesQuery(), cancellationToken)).ToOk());

        categories.MapPost("/", async Task<Results<Ok<CatalogListItem>, ProblemHttpResult>> (
                CreateCategoryCommand command,
                ICommandHandler<CreateCategoryCommand, CatalogListItem> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(command, cancellationToken)).ToOk());

        categories.MapPut("/{id:guid}", async Task<Results<Ok<CatalogListItem>, ProblemHttpResult>> (
                Guid id,
                RenameRequest request,
                ICommandHandler<RenameCategoryCommand, CatalogListItem> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new RenameCategoryCommand(id, request.Name), cancellationToken)).ToOk());

        // Not a delete: an inactive one is left out of the pickers for new work (ADR 0022).
        categories.MapPost("/{id:guid}/deactivate", async Task<Results<Ok<CatalogListItem>, ProblemHttpResult>> (
                Guid id,
                ICommandHandler<SetCategoryActiveCommand, CatalogListItem> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new SetCategoryActiveCommand(id, IsActive: false), cancellationToken)).ToOk());

        categories.MapPost("/{id:guid}/activate", async Task<Results<Ok<CatalogListItem>, ProblemHttpResult>> (
                Guid id,
                ICommandHandler<SetCategoryActiveCommand, CatalogListItem> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new SetCategoryActiveCommand(id, IsActive: true), cancellationToken)).ToOk());
    }

    private static void MapFabrics(RouteGroupBuilder fabrics)
    {
        fabrics.MapGet("/", async Task<Results<Ok<IReadOnlyList<CatalogListItem>>, ProblemHttpResult>> (
                IQueryHandler<ListFabricsQuery, IReadOnlyList<CatalogListItem>> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new ListFabricsQuery(), cancellationToken)).ToOk());

        fabrics.MapPost("/", async Task<Results<Ok<CatalogListItem>, ProblemHttpResult>> (
                CreateFabricCommand command,
                ICommandHandler<CreateFabricCommand, CatalogListItem> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(command, cancellationToken)).ToOk());

        fabrics.MapPut("/{id:guid}", async Task<Results<Ok<CatalogListItem>, ProblemHttpResult>> (
                Guid id,
                RenameRequest request,
                ICommandHandler<RenameFabricCommand, CatalogListItem> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new RenameFabricCommand(id, request.Name), cancellationToken)).ToOk());

        // Not a delete: an inactive one is left out of the pickers for new work (ADR 0022).
        fabrics.MapPost("/{id:guid}/deactivate", async Task<Results<Ok<CatalogListItem>, ProblemHttpResult>> (
                Guid id,
                ICommandHandler<SetFabricActiveCommand, CatalogListItem> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new SetFabricActiveCommand(id, IsActive: false), cancellationToken)).ToOk());

        fabrics.MapPost("/{id:guid}/activate", async Task<Results<Ok<CatalogListItem>, ProblemHttpResult>> (
                Guid id,
                ICommandHandler<SetFabricActiveCommand, CatalogListItem> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new SetFabricActiveCommand(id, IsActive: true), cancellationToken)).ToOk());
    }

    private static void MapColours(RouteGroupBuilder colours)
    {
        colours.MapGet("/", async Task<Results<Ok<IReadOnlyList<CatalogListItem>>, ProblemHttpResult>> (
                IQueryHandler<ListColoursQuery, IReadOnlyList<CatalogListItem>> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new ListColoursQuery(), cancellationToken)).ToOk());

        colours.MapPost("/", async Task<Results<Ok<CatalogListItem>, ProblemHttpResult>> (
                CreateColourCommand command,
                ICommandHandler<CreateColourCommand, CatalogListItem> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(command, cancellationToken)).ToOk());

        colours.MapPut("/{id:guid}", async Task<Results<Ok<CatalogListItem>, ProblemHttpResult>> (
                Guid id,
                RenameRequest request,
                ICommandHandler<RenameColourCommand, CatalogListItem> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new RenameColourCommand(id, request.Name), cancellationToken)).ToOk());

        // Not a delete: an inactive one is left out of the pickers for new work (ADR 0022).
        colours.MapPost("/{id:guid}/deactivate", async Task<Results<Ok<CatalogListItem>, ProblemHttpResult>> (
                Guid id,
                ICommandHandler<SetColourActiveCommand, CatalogListItem> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new SetColourActiveCommand(id, IsActive: false), cancellationToken)).ToOk());

        colours.MapPost("/{id:guid}/activate", async Task<Results<Ok<CatalogListItem>, ProblemHttpResult>> (
                Guid id,
                ICommandHandler<SetColourActiveCommand, CatalogListItem> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new SetColourActiveCommand(id, IsActive: true), cancellationToken)).ToOk());
    }

    private static void MapProducts(RouteGroupBuilder products)
    {
        products.MapGet("/", async Task<Results<Ok<PagedList<ProductListItem>>, ProblemHttpResult>> (
                string? search,
                int? page,
                int? pageSize,
                IQueryHandler<SearchProductsQuery, PagedList<ProductListItem>> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new SearchProductsQuery(search, page, pageSize), cancellationToken)).ToOk());

        products.MapPost("/", async Task<Results<Ok<CreateProductResponse>, ProblemHttpResult>> (
                CreateProductCommand command,
                ICommandHandler<CreateProductCommand, CreateProductResponse> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(command, cancellationToken)).ToOk());

        products.MapGet("/{id:guid}", async Task<Results<Ok<ProductDetails>, ProblemHttpResult>> (
                Guid id,
                IQueryHandler<GetProductQuery, ProductDetails> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new GetProductQuery(id), cancellationToken)).ToOk());

        products.MapPut("/{id:guid}", async Task<Results<Ok<ProductDetails>, ProblemHttpResult>> (
                Guid id,
                UpdateProductRequest request,
                ICommandHandler<UpdateProductCommand, ProductDetails> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(
                    new UpdateProductCommand(id, request.Name, request.Description, request.CategoryId),
                    cancellationToken)).ToOk());

        products.MapPost("/{id:guid}/deactivate", async Task<Results<Ok<ProductDetails>, ProblemHttpResult>> (
                Guid id,
                ICommandHandler<SetProductActiveCommand, ProductDetails> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new SetProductActiveCommand(id, IsActive: false), cancellationToken)).ToOk());

        products.MapPost("/{id:guid}/activate", async Task<Results<Ok<ProductDetails>, ProblemHttpResult>> (
                Guid id,
                ICommandHandler<SetProductActiveCommand, ProductDetails> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new SetProductActiveCommand(id, IsActive: true), cancellationToken)).ToOk());

        MapVariants(products.MapGroup("/{id:guid}/variants"));
        MapSizes(products.MapGroup("/{id:guid}/sizes"));
    }

    private static void MapVariants(RouteGroupBuilder variants)
    {
        variants.MapPost("/", async Task<Results<Ok<ProductDetails>, ProblemHttpResult>> (
                Guid id,
                AddVariantsRequest request,
                ICommandHandler<AddVariantsCommand, ProductDetails> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new AddVariantsCommand(id, request.Variants), cancellationToken)).ToOk());

        variants.MapPost("/{variantId:guid}/deactivate", async Task<Results<Ok<ProductDetails>, ProblemHttpResult>> (
                Guid id,
                Guid variantId,
                ICommandHandler<SetVariantActiveCommand, ProductDetails> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new SetVariantActiveCommand(id, variantId, IsActive: false), cancellationToken)).ToOk());

        variants.MapPost("/{variantId:guid}/activate", async Task<Results<Ok<ProductDetails>, ProblemHttpResult>> (
                Guid id,
                Guid variantId,
                ICommandHandler<SetVariantActiveCommand, ProductDetails> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new SetVariantActiveCommand(id, variantId, IsActive: true), cancellationToken)).ToOk());
    }

    private static void MapSizes(RouteGroupBuilder sizes)
    {
        sizes.MapPost("/", async Task<Results<Ok<ProductDetails>, ProblemHttpResult>> (
                Guid id,
                RenameRequest request,
                ICommandHandler<AddSizeCommand, ProductDetails> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new AddSizeCommand(id, request.Name), cancellationToken)).ToOk());

        sizes.MapPut("/order", async Task<Results<Ok<ProductDetails>, ProblemHttpResult>> (
                Guid id,
                ReorderSizesRequest request,
                ICommandHandler<ReorderSizesCommand, ProductDetails> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new ReorderSizesCommand(id, request.SizeIds), cancellationToken)).ToOk());

        sizes.MapPut("/{sizeId:guid}", async Task<Results<Ok<ProductDetails>, ProblemHttpResult>> (
                Guid id,
                Guid sizeId,
                RenameRequest request,
                ICommandHandler<RenameSizeCommand, ProductDetails> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new RenameSizeCommand(id, sizeId, request.Name), cancellationToken)).ToOk());
    }
}

/// <summary>The body of saving a model's name, description and category; its id is in the path.</summary>
public sealed record UpdateProductRequest(string? Name, string? Description, Guid? CategoryId);

/// <summary>The body of adding bags to a model; its id is in the path.</summary>
public sealed record AddVariantsRequest(IReadOnlyList<VariantToAdd>? Variants);

/// <summary>The body of putting a model's sizes in a new order: every size, once.</summary>
public sealed record ReorderSizesRequest(IReadOnlyList<Guid>? SizeIds);
