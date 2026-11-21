namespace Yalfan.Domain.Common;

/// <summary>
/// A language that catalog text can be stored in. The panels use only <see cref="Fa"/>; the store
/// shows all three (ARCHITECTURE.md 10, ADR 0006).
/// </summary>
public enum Locale
{
    Fa,
    Ar,
    En,
}
