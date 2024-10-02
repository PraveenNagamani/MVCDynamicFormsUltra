using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using MVCDynamicFormsUltra;
using MVCDynamicFormsUltra.Controllers;
using MVCDynamicFormsUltra.DBContext;
using MVCDynamicFormsUltra.Maintainance;
using Oracle.EntityFrameworkCore;
using StackExchange.Redis;
using System.Formats.Asn1;


var builder = WebApplication.CreateBuilder(args);

// Inject HTTP Client 
builder.Services.AddHttpClient<LaunchAPI>();

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add service for In Memory session
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(5);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.ConfigureApplicationCookie(options => {
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        //options.LoginPath = "/Account/Login";  // Redirect to login page if not authenticated
       // options.LogoutPath = "/Account/Logout"; // Logout path
        options.ExpireTimeSpan = TimeSpan.FromMinutes(5); // Cookie expiration time
    });

//Adding DbContext and using oracle
builder.Services.AddDbContext<EFClasses>(OptionsBuilder => OptionsBuilder.UseOracle(builder.Configuration.GetConnectionString("OrclConnection")));
builder.Services.AddSingleton<IConnectionMultiplexer>(sp => { return ConnectionMultiplexer.Connect(ConfigurationOptions.Parse(builder.Configuration.GetConnectionString("RedisConnection"), true)); });

// Add Ilogging
var logFilePath = builder.Configuration["Logging:FileLogging:LogFilePath"];
var logFileDirectory = builder.Configuration["Logging:FileLogging:logFileDirectory"];
if (!Directory.Exists(logFileDirectory)) Directory.CreateDirectory(logFileDirectory);
builder.Logging.AddProvider(new FileLoggerProvider(logFileDirectory + "\\" + logFilePath));

builder.Services.AddTransient<DBConnect>();

var app = builder.Build();

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

// As per MS documentation, this is the order
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
