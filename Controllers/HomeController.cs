using Microsoft.AspNetCore.Mvc;

namespace ABCretailStorageApp.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index() => View();
        public IActionResult Privacy() => View();
    }
}
