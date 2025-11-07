using Microsoft.AspNetCore.Mvc;

namespace StudyPlannerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        // GET: api/Home
        [HttpGet]
        public IActionResult Index()
        {
            return Ok(new
            {
                status = "OK",
                message = "Study Planner API is running",
                timestamp = DateTime.UtcNow,
                version = "1.0.0"
            });
        }

        // GET: api/Home/Health
        [HttpGet("Health")]
        public IActionResult Health()
        {
            return Ok(new
            {
                status = "Healthy",
                service = "Study Planner API",
                uptime = Environment.TickCount64 / 1000, // seconds since start
                timestamp = DateTime.UtcNow
            });
        }
    }
}
