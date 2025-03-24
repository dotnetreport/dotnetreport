using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ReportBuilder.Web.Models;
using System.Security.Claims;
using System.Text.Json;

namespace ReportBuilder.Web.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IConfigurationRoot _configuration;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public HomeController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        
            var builder = new ConfigurationBuilder()
                 .SetBasePath(Directory.GetCurrentDirectory())
                 .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            _configuration = builder.Build();

        }
        public IActionResult Index()
        {
            ViewBag.NewAccount = TempData["IsNewAccount"] != null && (bool)TempData["IsNewAccount"];
            TempData["IsNewAccount"] = false;
            return View();
        }

        [AllowAnonymous]
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        private async Task LoginUser(string email, string contact, bool dotnetAdmin, List<ClaimInfo> userclaims=null, List<string> roles=null)
        {
            var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Email, email),
                        new Claim(ClaimTypes.NameIdentifier, contact),
                        new Claim("http://schemas.microsoft.com/accesscontrolservice/2010/07/claims/identityprovider", "AspNet.Identity"),
                        new Claim(ClaimTypes.Name, contact)
                    };

            if (userclaims != null)
            {
                claims.AddRange(userclaims.Select(uc => new Claim(uc.Type, uc.Value)));
            }
            if (roles != null)
            {
                claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
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
                // try to login with dotnet report account first
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
                    await LoginUser(model.Email, loginResult.PrimaryContact, true,loginResult.User.Claims,loginResult.User.Roles);
                    DotNetReportHelper.UpdateConfigurationFile(loginResult.AccountKey, loginResult.PrivateKey, loginResult.DataConnect, true);
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home");
                    }
                } 
                else
                {
                    // check if we can login with local account
                    var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, isPersistent: false, lockoutOnFailure: false);
                    if (result.Succeeded)
                    {
                        // Redirect or return success response
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
            var errors = new List<string>();

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
                    if (root.TryGetProperty("Success", out JsonElement successElement) && successElement.ValueKind == JsonValueKind.True)
                    {
                        string accountApiKey = root.GetProperty("AccountApiKey").GetString();
                        string privateApiKey = root.GetProperty("PrivateApiKey").GetString();
                        string dataConnectKey = root.GetProperty("DataConnectKey").GetString();
                        DotNetReportHelper.UpdateConfigurationFile(accountApiKey, privateApiKey, dataConnectKey);

                        await LoginUser(model.Email, model.PrimaryContact, true);

                        TempData["IsNewAccount"] = true;
                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        JsonElement errorsElement;
                        if (root.TryGetProperty("Errors", out errorsElement) && errorsElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (JsonElement error in errorsElement.EnumerateArray())
                            {
                                errors.Add(error.GetString());
                            }
                        }
                    }
                }

                ModelState.AddModelError(string.Empty, "Account could not be created. Please try again");
                errors.ForEach(e => ModelState.AddModelError(string.Empty, e));
                return View(model);
            }
        }

    }
}
