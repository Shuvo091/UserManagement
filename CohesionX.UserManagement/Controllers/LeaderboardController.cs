using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CohesionX.UserManagement.Controllers;

/// <summary>
/// API controller for retrieving leaderboard data.
/// </summary>
[Authorize]
[ApiController]
[Route("api/v1/leaderboard")]
public class LeaderboardController : ControllerBase
{
    private readonly ILogger<LeaderboardController> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LeaderboardController"/> class.
    /// </summary>
    /// <param name="logger"> logger.</param>
    public LeaderboardController(ILogger<LeaderboardController> logger)
    {
        this.logger = logger;
    }

    /// <summary>
    /// Gets the current leaderboard.
    /// </summary>
    /// <returns>The leaderboard data and generation timestamp.</returns>
    [HttpGet]
    public IActionResult GetLeaderboard()
    {
        // No specification found in the requirement doc
        throw new NotImplementedException("not yet been implemented.");
    }
}
