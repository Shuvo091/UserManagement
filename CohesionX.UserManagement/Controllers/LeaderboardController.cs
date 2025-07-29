using Microsoft.AspNetCore.Mvc;

namespace CohesionX.UserManagement.Controllers
{
	/// <summary>
	/// API controller for retrieving leaderboard data.
	/// </summary>
	#if !DEBUG || !SKIP_AUTH
	[Authorize]
	#endif
	[ApiController]
	[Route("api/v1/leaderboard")]
	public class LeaderboardController : ControllerBase
	{
		/// <summary>
		/// Gets the current leaderboard.
		/// </summary>
		/// <returns>The leaderboard data and generation timestamp.</returns>
		[HttpGet]
		public IActionResult GetLeaderboard()
		{
			throw new NotImplementedException("not yet been implemented.");
		}
	}
}
