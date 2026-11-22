namespace Yalfan.Api.Endpoints.Panel;

/// <summary>The body of a rename of any named list item; the id is in the path.</summary>
public sealed record RenameRequest(string? Name);
