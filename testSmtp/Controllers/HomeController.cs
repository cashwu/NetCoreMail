using System;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using MimeKit.Text;

namespace testSmtp.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HomeController(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        
        [Route("/")]
        public IActionResult Index()
        {
            return Ok();
        }

        [Route("/mail")]
        public IActionResult Mail()
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse("test@sabasports.com"));
            email.To.Add(MailboxAddress.Parse("cash@example.com"));
            email.Subject = "Test Email Subject";

            var link = Url.Action("Valid", "Home", new { code = "abc123" }, _httpContextAccessor.HttpContext?.Request.Scheme);
            
            Console.WriteLine(link);

            email.Body = new TextPart(TextFormat.Html) { Text = @$"<h1>Example HTML Message Body</h1>
<a href={link} >click me</a>" };

            // send email
            using var smtp = new SmtpClient();
            smtp.Connect("192.168.90.89", 2525);
            // smtp.Authenticate("[USERNAME]", "[PASSWORD]");
            smtp.Send(email);
            smtp.Disconnect(true);

            return Ok(new { ok = 123 });
        }
        
        [Route("/valid")]
        public IActionResult Valid([FromQuery]string code)
        {
            Console.WriteLine(code);
            
            return Ok();
        }
        
    }
}