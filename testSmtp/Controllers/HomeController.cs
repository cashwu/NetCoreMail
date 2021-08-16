using Microsoft.AspNetCore.Mvc;

namespace testSmtp.Controllers
{
    public class HomeController : Controller
    {
        [Route("/")]
        public IActionResult Index()
        {
            return Ok();
        }

        [Route("/mail")]
        public IActionResult Mail()
        {
            return Ok(new { ok = 123 });
        }
    }
}