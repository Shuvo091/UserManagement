using CohesionX.UserManagement.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.RequestResponseModels.UserManagement;

namespace CohesionX.UserManagement.Controllers
{
	[ApiController]
    [Route("api/v1/elo-update")]
    public class EloUpdateController : ControllerBase
    {
		private readonly IEloService _eloService;
		public EloUpdateController(IEloService eloService)
		{
			_eloService = eloService;
		}
		[HttpPost]
        public async Task<IActionResult> EloUpdate([FromBody] EloUpdateRequest eloUpdateRequest)
        {
            try
            {
				var resp = await _eloService.ApplyEloUpdatesAsync(eloUpdateRequest);
				return Ok(resp);
			}
            catch(Exception ex)
			{
				// Log the exception (not shown here for brevity)
				return StatusCode(500, new { error = "An error occurred while processing your request." });
			}
        }

		[HttpPost("three-way-resolution")]
		public async Task<IActionResult> ThreeWayResolution([FromBody] ThreeWayEloUpdateRequest twuReq)
		{
			try
			{
				var resp = await _eloService.ResolveThreeWay(twuReq);
				return Ok(resp);
			}
			catch (Exception ex)
			{
				// Log the exception (not shown here for brevity)
				return StatusCode(500, new { error = "An error occurred while processing your request." });
			}
		}
	}
} 