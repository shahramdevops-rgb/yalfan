namespace Yalfan.Application.Catalog;

/// <summary>
/// Hands out model numbers (101, 102, …) from a database sequence, so two people saving at the
/// same moment never get the same number. A number taken by a save that then fails is skipped;
/// that is harmless, because a model number is a label, not a legal document (ADR 0016).
/// </summary>
public interface IProductNumbers
{
    Task<int> NextAsync(CancellationToken cancellationToken);
}
