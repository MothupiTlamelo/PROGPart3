using Microsoft.AspNetCore.Mvc;

namespace SystemClaim.Controllers
{
    public class UserController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
