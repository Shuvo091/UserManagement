using CohesionX.UserManagement.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.RequestResponseModels.UserManagement;

namespace CohesionX.UserManagement.Controllers;

/// <summary>
/// API controller for Elo rating updates and three-way resolution operations.
/// </summary>

#if !DEBUG || !SKIP_AUTH
	[Authorize(Roles = "Admin")]
#endif
[ApiController]
[Route("api/v1/elo-update")]
public class EloUpdateController : ControllerBase
{
    private readonly IEloService eloService;
    private readonly ILogger<EloUpdateController> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EloUpdateController"/> class.
    /// </summary>
    /// <param name="eloService">Service for Elo operations.</param>
    /// <param name="logger"> logger. </param>
    public EloUpdateController(IEloService eloService, ILogger<EloUpdateController> logger)
    {
        this.eloService = eloService;
        this.logger = logger;
    }

    /// <summary>
    /// Applies Elo updates based on the provided request.
    /// </summary>
    /// <param name="eloUpdateRequest">The Elo update request details.</param>
    /// <returns>The result of the Elo update operation.</returns>
    [HttpPost]
    public async Task<IActionResult> EloUpdate([FromBody] EloUpdateRequest eloUpdateRequest)
    {
        var resp = await this.eloService.ApplyEloUpdatesAsync(eloUpdateRequest);
        return this.Ok(resp);
    }

    /// <summary>
    /// Resolves a three-way Elo update scenario.
    /// </summary>
    /// <param name="twuReq">The three-way Elo update request details.</param>
    /// <returns>The result of the three-way resolution operation.</returns>
    [HttpPost("three-way-resolution")]
    public async Task<IActionResult> ThreeWayResolution([FromBody] ThreeWayEloUpdateRequest twuReq)
    {
        var resp = await this.eloService.ResolveThreeWay(twuReq);
        return this.Ok(resp);
    }
}
