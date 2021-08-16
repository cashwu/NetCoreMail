using System;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using MimeKit;
using MimeKit.Text;

namespace testSmtp.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IDataProtector _dataProtector;

        public HomeController(IHttpContextAccessor httpContextAccessor,
                              IDataProtectionProvider dataProtectionProvider)
        {
            _httpContextAccessor = httpContextAccessor;
            _dataProtector = dataProtectionProvider.CreateProtector("email");
        }

        [Route("/")]
        public IActionResult Index()
        {
            return Ok();
        }

        /// <summary>
        /// https://github.com/dotnet/aspnetcore/blob/bcfbd5cc47dde7f2be50a24721f24a020dc77356/src/Identity/UI/src/Areas/Identity/Pages/V5/Account/Register.cshtml.cs#L144
        /// https://github.com/dotnet/aspnetcore/blob/bcfbd5cc47dde7f2be50a24721f24a020dc77356/src/Identity/UI/src/Areas/Identity/Pages/V5/Account/ResendEmailConfirmation.cshtml.cs#L91
        /// </summary>
        /// <returns></returns>
        [Route("/email")]
        public IActionResult Mail()
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse("test@sabasports.com"));
            email.To.Add(MailboxAddress.Parse("cash@example.com"));
            email.Subject = "Test Email Subject";

            var base64String = GetToken("cc@cc.cc");
            Console.WriteLine(base64String);
            var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(base64String));
            Console.WriteLine(code);

            var callbackUrl = Url.Action("Valid", "Home", new { code }, _httpContextAccessor.HttpContext?.Request.Scheme);

            Console.WriteLine($"callback url - {callbackUrl}");
            Console.WriteLine($"callback url encoder - {HtmlEncoder.Default.Encode(callbackUrl)}");

            email.Body = new TextPart(TextFormat.Html)
            {
                Text = @$"<h1>Example HTML Message Body</h1> <a href={HtmlEncoder.Default.Encode(callbackUrl)} >click me</a>"
            };

            // send email
            using var smtp = new SmtpClient();
            smtp.Connect("192.168.90.89", 2525);

            // smtp.Authenticate("[USERNAME]", "[PASSWORD]");
            smtp.Send(email);
            smtp.Disconnect(true);

            return Ok(new { ok = 123 });
        }

        /// <summary>
        /// https://github.com/dotnet/aspnetcore/blob/bcfbd5cc47dde7f2be50a24721f24a020dc77356/src/Identity/UI/src/Areas/Identity/Pages/V5/Account/ConfirmEmail.cshtml.cs#L58
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        [Route("/valid")]
        public IActionResult Valid([FromQuery] string code)
        {
            Console.WriteLine(code);

            var base64String = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));

            Console.WriteLine(base64String);

            var verifyToken = VerifyToken(base64String, "cc@cc.cc");

            Console.WriteLine(verifyToken);

            return Ok();
        }

        /// <summary>
        /// https://github.com/dotnet/aspnetcore/blob/bcfbd5cc47dde7f2be50a24721f24a020dc77356/src/Identity/Core/src/DataProtectorTokenProvider.cs#L82
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        private string GetToken(string userId)
        {
            var ms = new MemoryStream();

            using (var writer = ms.CreateWriter())
            {
                writer.Write(DateTimeOffset.UtcNow);
                writer.Write(userId);
            }

            var protectedBytes = _dataProtector.Protect(ms.ToArray());

            return Convert.ToBase64String(protectedBytes);
        }

        /// <summary>
        /// https://github.com/dotnet/aspnetcore/blob/bcfbd5cc47dde7f2be50a24721f24a020dc77356/src/Identity/Core/src/DataProtectorTokenProvider.cs#L117
        /// </summary>
        /// <param name="base64String"></param>
        /// <param name="actualUserId"></param>
        /// <returns></returns>
        private bool VerifyToken(string base64String, string actualUserId)
        {
            try
            {
                var unprotectedData = _dataProtector.Unprotect(Convert.FromBase64String(base64String));
                var ms = new MemoryStream(unprotectedData);

                using var reader = ms.CreateReader();

                var creationTime = reader.ReadDateTimeOffset();

                Console.WriteLine($"creation time - {creationTime}");

                // var expirationTime = creationTime + TimeSpan.FromDays(1);
                var expirationTime = creationTime + TimeSpan.FromMinutes(10);

                Console.WriteLine($"expiration time - {expirationTime}");

                var utcNow = DateTimeOffset.UtcNow;
                
                Console.WriteLine($"utc now - {utcNow}");

                if (expirationTime < utcNow)
                {
                    return false;
                }

                var userId = reader.ReadString();

                Console.WriteLine($"user id - {userId}");

                if (userId != actualUserId)
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    internal static class StreamExtensions
    {
        private static readonly Encoding DefaultEncoding = new UTF8Encoding(false, true);

        public static BinaryReader CreateReader(this Stream stream)
        {
            return new BinaryReader(stream, DefaultEncoding, true);
        }

        public static BinaryWriter CreateWriter(this Stream stream)
        {
            return new BinaryWriter(stream, DefaultEncoding, true);
        }

        public static DateTimeOffset ReadDateTimeOffset(this BinaryReader reader)
        {
            return new DateTimeOffset(reader.ReadInt64(), TimeSpan.Zero);
        }

        public static void Write(this BinaryWriter writer, DateTimeOffset value)
        {
            writer.Write(value.UtcTicks);
        }
    }
}