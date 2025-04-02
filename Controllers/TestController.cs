using Microsoft.AspNetCore.Mvc;

namespace JwtAuthApp.Controllers
{
    public class TestController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
