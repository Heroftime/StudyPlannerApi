using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudyPlannerApi.Data;
using StudyPlannerApi.Models;

namespace StudyPlannerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SchoolController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SchoolController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<School>>> GetSchools()
        {
            return await _context.Schools.ToListAsync();
        }
    }
}
