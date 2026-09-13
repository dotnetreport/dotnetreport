using ReportBuilder.Web.Jobs;
using ReportBuilder.Web.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var mvc = builder.Services.AddControllersWithViews();

// Recompile .cshtml on save while developing, so Hot Reload applies view changes without a restart.
if (builder.Environment.IsDevelopment()) mvc.AddRazorRuntimeCompilation();

var app = builder.Build();

JobScheduler.Start(); //<--- Add this line manually


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
