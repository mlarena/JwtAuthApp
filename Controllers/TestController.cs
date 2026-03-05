using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace JwtAuthApp.Controllers
{
 
 
    [Authorize]
     public class TestController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
