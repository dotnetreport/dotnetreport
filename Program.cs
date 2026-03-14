using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using ReportBuilder.Web.Controllers;
using ReportBuilder.Web.Jobs;
using ReportBuilder.Web.Models;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// Also load the per-user dotnetreport config so that the DI IConfiguration
// (used in controllers) contains the same tokens as DotNetReportHelper.StaticConfig.
// In Electron mode the file lives in %AppData%\DotNetReport; otherwise in the CWD.
{
    var dotnetReportConfigDir = Environment.GetEnvironmentVariable("ELECTRON_APP") == "true"
        ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DotNetReport")
        : Directory.GetCurrentDirectory();
    var dotnetReportConfigPath = Path.Combine(dotnetReportConfigDir, "appsettings.dotnetreport.json");
    builder.Configuration.AddJsonFile(dotnetReportConfigPath, optional: true, reloadOnChange: true);
}

// Add Identity services to the services container.
services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    // Identity options configuration, like password strength, lockout duration, etc.
})
.AddUserStore<DotNetReportUserStore>()
.AddRoleStore<DotNetReportRoleStore>()
.AddDefaultTokenProviders();

services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Home/Login";
    options.LogoutPath = "/Home/Logout";
    options.AccessDeniedPath = "/Home/Index";
    options.ExpireTimeSpan = TimeSpan.FromDays(365); 
    options.SlidingExpiration = true;
});
services.AddHttpClient();
services.AddHttpContextAccessor();

// Persist Data Protection keys to a stable per-user directory so auth cookies survive app
// restarts. Without this ASP.NET Core generates in-memory keys on every startup, making every
// existing cookie unreadable and forcing users to log in again after each restart.
var dpKeysPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "DotNetReport", "DataProtection-Keys");
Directory.CreateDirectory(dpKeysPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dpKeysPath))
    .SetApplicationName("DotNetReport");

builder.Services.AddControllersWithViews();

// Configure authentication
services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
}).AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/Home/Login";
    options.LogoutPath = "/Home/Logout";
    options.AccessDeniedPath = "/Home/Index";
    options.ExpireTimeSpan = TimeSpan.FromDays(365);
    options.SlidingExpiration = true;
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowAnyOrigin();
    });
});
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(365); // Session timeout (adjust as needed)
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddScoped<DotNetUserApiController>();
var app = builder.Build();

JobScheduler.Start(); //<--- Add this line manually

// Desktop (Electron)

var isElectron = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ELECTRON_APP"));

// Configure the HTTP request pipeline.
// In Electron mode always show the full developer exception page — the app is a local desktop
// app so there is no risk of leaking internals to end-users, and it makes diagnosing startup
// errors far easier than a silent empty-body 500.
if (app.Environment.IsDevelopment() || isElectron)
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Skip HTTPS redirect when running inside Electron — it only binds HTTP on localhost.
if (!isElectron)
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();

app.UseRouting();
app.UseCors();

app.UseAuthentication(); 
app.UseAuthorization();
app.UseSession();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

    app.Run();
