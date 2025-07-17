using Microsoft.AspNetCore.Mvc;

namespace CohesionX.UserManagement.Controllers
{
    [ApiController]
    [Route("api/v1/leaderboard")]
    public class LeaderboardController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetLeaderboard()
        {
            // TODO: Implement leaderboard logic
            return Ok(new
            {
                leaderboard = new object[] { },
                generatedAt = DateTime.UtcNow
            });
        }
    }
} 