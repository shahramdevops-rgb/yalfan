using Yalfan.Domain.Common;

namespace Yalfan.Domain.Parties;

/// <summary>
/// The checks a contractor's and a worker's profile share, so the two forms refuse the same name
/// or notes with the same code. Not a base class: from Phase 4 and 5 workers get attendance and
/// pay terms that contractors never have (ADR 0020).
/// </summary>
internal static class PartyProfile
{
    public const int FullNameMaxLength = 100;

    public const int NotesMaxLength = 500;

    /// <summary>The name normalized like catalog names, so a search finds it whichever keyboard typed it.</summary>
    public static Result<string> CheckFullName(string? fullName)
    {
        var name = PersianText.Normalize(fullName);
        if (name.Length == 0)
        {
            return PartiesErrors.FullNameRequired;
        }

        if (name.Length > FullNameMaxLength)
        {
            return PartiesErrors.FullNameTooLong;
        }

        return name;
    }

    /// <summary>Trimmed; empty notes are no notes.</summary>
    public static Result<string?> CheckNotes(string? notes)
    {
        var trimmed = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        if (trimmed?.Length > NotesMaxLength)
        {
            return PartiesErrors.NotesTooLong;
        }

        return Result.Success(trimmed);
    }
}
