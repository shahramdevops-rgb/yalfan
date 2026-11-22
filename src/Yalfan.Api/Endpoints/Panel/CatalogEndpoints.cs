using Microsoft.AspNetCore.Http.HttpResults;
using Yalfan.Api.Common.Endpoints;
using Yalfan.Api.Common.Errors;
using Yalfan.Api.Common.Security;
using Yalfan.Application.Catalog;
using Yalfan.Application.Catalog.AddVariant;
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
using Yalfan.Application.Catalog.SearchProducts;
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
        categories.MapGet("/", async Task<Results<Ok<IReadOnlyList<CatalogOptionSummary>>, ProblemHttpResult>> (
                IQueryHandler<ListCategoriesQuery, IReadOnlyList<CatalogOptionSummary>> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new ListCategoriesQuery(), cancellationToken)).ToOk());

        categories.MapPost("/", async Task<Results<Ok<CatalogOptionSummary>, ProblemHttpResult>> (
                CreateCategoryCommand command,
                ICommandHandler<CreateCategoryCommand, CatalogOptionSummary> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(command, cancellationToken)).ToOk());

        categories.MapPut("/{id:guid}", async Task<Results<Ok<CatalogOptionSummary>, ProblemHttpResult>> (
                Guid id,
                RenameRequest request,
                ICommandHandler<RenameCategoryCommand, CatalogOptionSummary> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new RenameCategoryCommand(id, request.Name), cancellationToken)).ToOk());
    }

    private static void MapFabrics(RouteGroupBuilder fabrics)
    {
        fabrics.MapGet("/", async Task<Results<Ok<IReadOnlyList<CatalogOptionSummary>>, ProblemHttpResult>> (
                IQueryHandler<ListFabricsQuery, IReadOnlyList<CatalogOptionSummary>> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new ListFabricsQuery(), cancellationToken)).ToOk());

        fabrics.MapPost("/", async Task<Results<Ok<CatalogOptionSummary>, ProblemHttpResult>> (
                CreateFabricCommand command,
                ICommandHandler<CreateFabricCommand, CatalogOptionSummary> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(command, cancellationToken)).ToOk());

        fabrics.MapPut("/{id:guid}", async Task<Results<Ok<CatalogOptionSummary>, ProblemHttpResult>> (
                Guid id,
                RenameRequest request,
                ICommandHandler<RenameFabricCommand, CatalogOptionSummary> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new RenameFabricCommand(id, request.Name), cancellationToken)).ToOk());
    }

    private static void MapColours(RouteGroupBuilder colours)
    {
        colours.MapGet("/", async Task<Results<Ok<IReadOnlyList<CatalogOptionSummary>>, ProblemHttpResult>> (
                IQueryHandler<ListColoursQuery, IReadOnlyList<CatalogOptionSummary>> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new ListColoursQuery(), cancellationToken)).ToOk());

        colours.MapPost("/", async Task<Results<Ok<CatalogOptionSummary>, ProblemHttpResult>> (
                CreateColourCommand command,
                ICommandHandler<CreateColourCommand, CatalogOptionSummary> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(command, cancellationToken)).ToOk());

        colours.MapPut("/{id:guid}", async Task<Results<Ok<CatalogOptionSummary>, ProblemHttpResult>> (
                Guid id,
                RenameRequest request,
                ICommandHandler<RenameColourCommand, CatalogOptionSummary> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new RenameColourCommand(id, request.Name), cancellationToken)).ToOk());
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

        products.MapPost("/{id:guid}/variants", async Task<Results<Ok<AddVariantResponse>, ProblemHttpResult>> (
                Guid id,
                AddVariantRequest request,
                ICommandHandler<AddVariantCommand, AddVariantResponse> handler,
                CancellationToken cancellationToken) =>
                (await handler.Handle(new AddVariantCommand(id, request.FabricId, request.ColourId, request.SizeId), cancellationToken)).ToOk());
    }
}

/// <summary>The body of adding a variant; the model's id is in the path.</summary>
public sealed record AddVariantRequest(Guid? FabricId, Guid? ColourId, Guid? SizeId);
