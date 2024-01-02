using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MySqlX.XDevAPI.Relational;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReportBuilder.Web.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using static ReportBuilder.Web.Controllers.DotNetReportApiController;

namespace ReportBuilder.Web.Controllers
{
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
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var settings = new DotNetReportSettings
            {
                ApiUrl = _configuration.GetValue<string>("dotNetReport:apiUrl"),
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
                using (JsonDocument document = JsonDocument.Parse(stringContent))
                {
                    JsonElement root = document.RootElement;
                    if (root.TryGetProperty("success", out JsonElement successElement) && successElement.ValueKind == JsonValueKind.True)
                    {
                        HttpContext.Session.SetString("UserEmail", model.Email);
                        return RedirectToAction("Index", "DotNetSetup");
                    }
                }
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }
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
                ApiUrl = _configuration.GetValue<string>("dotNetReport:apiUrl"),
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
                        HttpContext.Session.SetString("UserEmail", model.Email);
                        return RedirectToAction("Index", "DotNetSetup");

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
