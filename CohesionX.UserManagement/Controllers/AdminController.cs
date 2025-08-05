using CohesionX.UserManagement.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Contracts.Usermanagement.Requests;

namespace CohesionX.UserManagement.Controllers;

/// <summary>
/// API controller for administrative operations such as setting professional status and updating configuration.
/// </summary>
[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/v1/admin")]
public class AdminController : ControllerBase
{
    private readonly IUserService userService;
    private readonly ILogger<AdminController> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminController"/> class.
    /// </summary>
    /// <param name="userService">Service for user management operations.</param>
    /// <param name="logger"> loger. </param>
    public AdminController(IUserService userService, ILogger<AdminController> logger)
    {
        this.userService = userService;
        this.logger = logger;
    }

    /// <summary>
    /// Sets the professional status for a user.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="setProfessionalRequest">The request containing professional status details.</param>
    /// <returns>The updated professional profile or error details.</returns>
    [HttpPost("users/{userId}/set-professional")]
    public async Task<IActionResult> SetProfessional([FromRoute] Guid userId, [FromBody] SetProfessionalRequest setProfessionalRequest)
    {
        var profile = await this.userService.SetProfessional(userId, setProfessionalRequest);
        return this.Ok(profile);
    }

    /// <summary>
    /// Updates the global configuration for verification and compliance.
    /// </summary>
    /// <param name="configRequest">The configuration update request object.</param>
    /// <returns>Result of the configuration update operation.</returns>
    [HttpPut("config")]
    public IActionResult UpdateConfig([FromBody] object configRequest)
    {
        throw new NotImplementedException("not yet been implemented.");
    }

    /// <summary>
    /// Sets verification requirements for a specific user.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="verificationRequirements">The verification requirements object.</param>
    /// <returns>Result of the verification requirements update operation.</returns>
    [HttpPut("users/{userId}/verification-requirements")]
    public IActionResult SetVerificationRequirements([FromRoute] Guid userId, [FromBody] object verificationRequirements)
    {
        throw new NotImplementedException("not yet been implemented.");
    }
}
