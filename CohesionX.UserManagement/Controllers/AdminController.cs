using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SharedLibrary.RequestResponseModels.UserManagement;
using CohesionX.UserManagement.Modules.Users.Application.Interfaces;

namespace CohesionX.UserManagement.Controllers
{
    [ApiController]
    [Route("api/v1/admin")]
    [Authorize(Roles = "admin")]
    public class AdminController : ControllerBase
    {
		private readonly IUserService _userService;

		public AdminController(IUserService userService)
		{
			    _userService = userService;
		}
		[HttpPost("users/{userId}/set-professional")]
        public async Task<IActionResult> SetProfessional([FromRoute] Guid userId, [FromBody] SetProfessionalRequest setProfessionalRequest)
        {
			try
			{
				var profile = await _userService.SetProfessional(userId, setProfessionalRequest);
				return Ok(profile);
			}
			catch (Exception e)
			{
				return StatusCode(500, new { error = "An error occurred while processing your request." });

			}
		}

        [HttpPut("config")]
        public IActionResult UpdateConfig([FromBody] object configRequest)
        {
            // TODO: Implement config update logic
            return Ok(new
            {
                requirementsUpdated = true,
                piiDataCollection = true,
                complianceMode = "POPIA_basic_validation",
                verificationLevel = "v1_field_validation",
                verificationSteps = new[] { "phone_verification", "email_verification", "id_format_check", "photo_presence_check" },
                roadmapEnhancements = new { v2_planned = "dha_automated_verification", v2_provider = "experian_or_similar" }
            });
        }

        [HttpPut("users/{userId}/verification-requirements")]
        public IActionResult SetVerificationRequirements([FromRoute] Guid userId, [FromBody] object verificationRequirements)
        {
            // TODO: Implement verification requirements logic
            return Ok(new
            {
                requirementsUpdated = true,
                piiDataCollection = true,
                complianceMode = "POPIA_basic_validation",
                verificationLevel = "v1_field_validation",
                verificationSteps = new[] { "phone_verification", "email_verification", "id_format_check", "photo_presence_check" },
                roadmapEnhancements = new { v2_planned = "dha_automated_verification", v2_provider = "experian_or_similar" }
            });
        }
    }
} 