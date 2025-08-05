// <copyright file="VerificationRequirementService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace CohesionX.UserManagement.Application.Services;

using CohesionX.UserManagement.Abstractions.DTOs.Options;
using CohesionX.UserManagement.Abstractions.Services;
using CohesionX.UserManagement.Database.Abstractions.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

/// <summary>
/// Provides operations for retrieving user verification requirements.
/// </summary>
public class VerificationRequirementService : IVerificationRequirementService
{
    private readonly IVerificationRequirementRepository repo;
    private readonly IOptions<ValidationOptions> validationOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="VerificationRequirementService"/> class.
    /// </summary>
    /// <param name="validationOptions">Validation configuration used to validate user verification.</param>
    /// <param name="repo">The repository for verification requirements.</param>
    public VerificationRequirementService(IVerificationRequirementRepository repo, IOptions<ValidationOptions> validationOptions)
    {
        this.repo = repo;
        this.validationOptions = validationOptions;
    }

    /// <summary>
    /// Gets user-specific verification requirements if present,
    /// otherwise falls back to default global validation options.
    /// </summary>
    /// <param name="userId"> Id of user to get specific requirements. </param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<ValidationOptions> GetEffectiveValidationOptionsAsync(Guid userId)
    {
        var requirement = await this.repo.GetVerificationRequirement(userId);

        if (requirement != null)
        {
            return new ValidationOptions
            {
                RequireIdDocument = requirement.RequireIdDocument,
                RequirePhotoUpload = requirement.RequirePhotoUpload,
                RequirePhoneVerification = requirement.RequirePhoneVerification,
                RequireEmailVerification = requirement.RequireEmailVerification,
                VerificationLevel = requirement.VerificationLevel,
                Reason = requirement.Reason,
                ValidationRules = ConvertToRules(requirement.ValidationRules),
            };
        }

        return this.validationOptions.Value;
    }

    private static ValidationRules ConvertToRules(Dictionary<string, string> raw)
    {
        return new ValidationRules
        {
            IdNumber = raw.TryGetValue(nameof(ValidationRules.IdNumber), out var id) ? id : string.Empty,
            Photo = raw.TryGetValue(nameof(ValidationRules.Photo), out var photo) ? photo : string.Empty,
            Note = raw.TryGetValue(nameof(ValidationRules.Note), out var note) ? note : string.Empty,
        };
    }
}
