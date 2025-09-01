using Microsoft.AspNetCore.Mvc;

namespace IYSIntegration.Proxy.API.Controllers
{
    public class SFProxyController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
