namespace Yalfan.Domain.Identity;

/// <summary>
/// What a person may do. Each user has exactly one role. Contractor and customer roles are
/// added with their phases (10, 12); finer permissions sit on top of the role later.
/// </summary>
public enum Role
{
    /// <summary>The workshop owner: everything, including managing staff users.</summary>
    Owner,

    /// <summary>The workshop manager.</summary>
    Manager,

    /// <summary>The office operator who enters the daily work.</summary>
    Operator,
}
