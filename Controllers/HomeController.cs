using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReportBuilder.Web.Models;
using System.Security.Claims;
using System.Text.Json;

namespace ReportBuilder.Web.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IConfigurationRoot _configuration;

        public HomeController()
        {
            var builder = new ConfigurationBuilder()
                 .SetBasePath(Directory.GetCurrentDirectory())
                 .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            _configuration = builder.Build();

        }
        public IActionResult Index()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        private async Task LoginUser(string email, string contact, bool dotnetAdmin)
        {
            var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Email, email),
                        new Claim(ClaimTypes.NameIdentifier, contact),
                        new Claim("http://schemas.microsoft.com/accesscontrolservice/2010/07/claims/identityprovider", "AspNet.Identity"),
                        new Claim(ClaimTypes.Name, contact)
                    };

            if (dotnetAdmin)
            {
                claims.Add(
                    new Claim(ClaimTypes.Role, DotNetReportRoles.DotNetReportAdmin)
                );
            }
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                
                // Configure additional properties as needed
            };

            await HttpContext.SignInAsync(
                IdentityConstants.ApplicationScheme,
                new ClaimsPrincipal(claimsIdentity), authProperties);

        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string? returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var settings = new DotNetReportSettings
            {
                ApiUrl = _configuration.GetValue<string>("dotNetReport:accountapiurl"),
            };
            using (var client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(new[]
                {
                         new KeyValuePair<string, string>("Email", model.Email),
                         new KeyValuePair<string, string>("Password", model.Password)
                });
                var response = await client.PostAsync(new Uri(settings.ApiUrl + "/account/login"), content);
                var stringContent = await response.Content.ReadAsStringAsync();
                var loginResult = JsonConvert.DeserializeObject<LoginResult>(stringContent) ?? new LoginResult
                {
                    Success = false,
                    Message = "Could not Login, please try again."
                };

                if (loginResult.Success)
                {
                    await LoginUser(model.Email, loginResult.PrimaryContact, true);

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }
                
                ModelState.AddModelError(string.Empty, loginResult.Message);
                return View(model);
            }
        }

        [HttpPost]
        public async Task<ActionResult> LogOut()
        {
            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);

            return RedirectToAction("Index", "Home");
        }


        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            model.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var settings = new DotNetReportSettings
            {
                ApiUrl = _configuration.GetValue<string>("dotNetReport:accountapiurl"),
            };
            using (var client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(new[]
                {
                         new KeyValuePair<string, string>("PrimaryContact", model.PrimaryContact),
                         new KeyValuePair<string, string>("Email", model.Email),
                         new KeyValuePair<string, string>("IpAddress", model.IpAddress)
                });
                var response = await client.PostAsync(new Uri(settings.ApiUrl + "/account/register"), content);
                var stringContent = await response.Content.ReadAsStringAsync();
                using (JsonDocument document = JsonDocument.Parse(stringContent))
                {
                    JsonElement root = document.RootElement;
                    if (root.TryGetProperty("success", out JsonElement successElement) && successElement.ValueKind == JsonValueKind.True)
                    {
                        string accountApiKey = root.GetProperty("AccountApiKey").GetString();
                        string privateApiKey = root.GetProperty("PrivateApiKey").GetString();
                        string dataConnectKey = root.GetProperty("DataConnectKey").GetString();
                        UpdateConfigurationFile(accountApiKey, privateApiKey, dataConnectKey);


                        return RedirectToAction("Index", "Home");

                    }
                }

                ModelState.AddModelError(string.Empty, "Account Could Not Created");
                return View(model);
            }
        }

        public void UpdateConfigurationFile(string accountApiKey, string privateApiKey, string dataConnectKey)
        {
            var _configFileName = "appsettings.json";
            var _configFilePath = Path.Combine(Directory.GetCurrentDirectory(), _configFileName);

            JObject existingConfig;
            if (System.IO.File.Exists(_configFilePath))
            {
                existingConfig = JObject.Parse(System.IO.File.ReadAllText(_configFilePath));
                if (existingConfig["dotNetReport"] is JObject dotNetReportObject)
                {
                    dotNetReportObject["accountApiToken"] = accountApiKey;
                    dotNetReportObject["privateApiToken"] = privateApiKey;
                    dotNetReportObject["dataconnectApiToken"] = dataConnectKey;
                }
                System.IO.File.WriteAllText(_configFilePath, existingConfig.ToString(Newtonsoft.Json.Formatting.Indented));
            }
        }

    }
}
