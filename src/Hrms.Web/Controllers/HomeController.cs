using Microsoft.AspNetCore.Mvc;

namespace Hrms.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return RedirectToAction("Login", "Account");
        }

        return View();
    }
}
