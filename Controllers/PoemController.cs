using Microsoft.AspNetCore.Mvc;

namespace StudyPlannerApi.Controllers
{
    public class PoemController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
