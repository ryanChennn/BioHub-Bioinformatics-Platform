using BIOHub.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using BIOHub.Models;
using BIOHub.Services;
using Microsoft.AspNetCore.DataProtection;
var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<BIOHubDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("BIOHubConnection")));


builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; 
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "BIOHub.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<IPasswordHasher<Users>, 
    PasswordHasher<Users>>();


// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddScoped<RNAseqAnalysisService>();

if (OperatingSystem.IsLinux())
{
    builder.Services.AddDataProtection()
        .SetApplicationName("BIOHub")
        .PersistKeysToFileSystem(
            new DirectoryInfo("/data/biohub-keys"));
}

var app = builder.Build();

// 專供 Docker Compose db-init 一次性初始化使用，不啟動網站
if (app.Configuration.GetValue<bool>("BIOHUB_INIT_DB"))
{
    await BioHubDemoInitializer.InitializeAsync(app.Services, app.Configuration);
    return;
}


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();



app.MapStaticAssets();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
