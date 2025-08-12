namespace CohesionX.UserManagement.Abstractions.DTOs;

/// <summary>
/// Model for seeding users.
/// </summary>
public class UserModel
{
    /// <summary>
    /// Gets or sets Id.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets First name.
    /// </summary>
    public string FirstName { get; set; } = default!;

    /// <summary>
    /// Gets or sets last name.
    /// </summary>
    public string LastName { get; set; } = default!;

    /// <summary>
    /// Gets or sets email.
    /// </summary>
    public string Email { get; set; } = default!;

    /// <summary>
    /// Gets or sets password.
    /// </summary>
    public string Password { get; set; } = default!;

    /// <summary>
    /// Gets or sets phone.
    /// </summary>
    public string Phone { get; set; } = default!;

    /// <summary>
    /// Gets or sets id number.
    /// </summary>
    public string? IdNumber { get; set; }

    /// <summary>
    /// Gets or sets status.
    /// </summary>
    public string Status { get; set; } = default!;

    /// <summary>
    /// Gets or sets role.
    /// </summary>
    public string Role { get; set; } = default!;

    /// <summary>
    /// Gets or sets a value indicating whether user is professional.
    /// </summary>
    public bool IsProfessional { get; set; }
}
