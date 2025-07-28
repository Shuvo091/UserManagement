using CohesionX.UserManagement.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.RequestResponseModels.UserManagement;

namespace CohesionX.UserManagement.Controllers
{
	/// <summary>
	/// API controller for Elo rating updates and three-way resolution operations.
	/// </summary>
	[ApiController]
	[Route("api/v1/elo-update")]
	public class EloUpdateController : ControllerBase
	{
		private readonly IEloService _eloService;

		/// <summary>
		/// Initializes a new instance of the <see cref="EloUpdateController"/> class.
		/// </summary>
		/// <param name="eloService">Service for Elo operations.</param>
		public EloUpdateController(IEloService eloService)
		{
			_eloService = eloService;
		}

		/// <summary>
		/// Applies Elo updates based on the provided request.
		/// </summary>
		/// <param name="eloUpdateRequest">The Elo update request details.</param>
		/// <returns>The result of the Elo update operation.</returns>
		[HttpPost]
		public async Task<IActionResult> EloUpdate([FromBody] EloUpdateRequest eloUpdateRequest)
		{
			try
			{
				var resp = await _eloService.ApplyEloUpdatesAsync(eloUpdateRequest);
				return Ok(resp);
			}
			catch (Exception)
			{
				// Log the exception (not shown here for brevity)
				return StatusCode(500, new { error = "An error occurred while processing your request." });
			}
		}

		/// <summary>
		/// Resolves a three-way Elo update scenario.
		/// </summary>
		/// <param name="twuReq">The three-way Elo update request details.</param>
		/// <returns>The result of the three-way resolution operation.</returns>
		[HttpPost("three-way-resolution")]
		public async Task<IActionResult> ThreeWayResolution([FromBody] ThreeWayEloUpdateRequest twuReq)
		{
			try
			{
				var resp = await _eloService.ResolveThreeWay(twuReq);
				return Ok(resp);
			}
			catch (Exception)
			{
				// Log the exception (not shown here for brevity)
				return StatusCode(500, new { error = "An error occurred while processing your request." });
			}
		}
	}
}
