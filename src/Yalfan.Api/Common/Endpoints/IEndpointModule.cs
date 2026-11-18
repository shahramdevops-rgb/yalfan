namespace Yalfan.Api.Common.Endpoints;

/// <summary>
/// The endpoints of one module (Catalog, Production, …). Each module maps its own routes, so
/// Program.cs stays a short list and adding a module does not touch it.
/// </summary>
public interface IEndpointModule
{
    void MapEndpoints(IEndpointRouteBuilder endpoints);
}
