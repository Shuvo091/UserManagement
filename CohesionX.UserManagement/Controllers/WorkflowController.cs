using Microsoft.AspNetCore.Mvc;

namespace CohesionX.UserManagement.Controllers
{
    [ApiController]
    [Route("api/v1/workflow/notify-elo-updated")]
    public class WorkflowController : ControllerBase
    {
        [HttpPost]
        public IActionResult NotifyEloUpdated([FromBody] object notifyRequest)
        {
            // TODO: Implement workflow notification logic
            return Accepted(new
            {
                acknowledged = true,
                workflowAction = "job_finalization_scheduled"
            });
        }
    }
} 