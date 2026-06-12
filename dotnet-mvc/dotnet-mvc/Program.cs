using dotnet_mvc.Security;
using dotnet_mvc.Services.Academic;
using dotnet_mvc.Services.Admin;
using dotnet_mvc.Services.Dashboard;
using dotnet_mvc.Services.Finance;
using dotnet_mvc.Services.Scenarios;
using Microsoft.EntityFrameworkCore;
using dotnet_mvc.Data;
using dotnet_mvc.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("A connection string 'DefaultConnection' não está configurada.");
var serverVersion = new MySqlServerVersion(new Version(8, 0, 36));

// Disponibilizar a connection string às classes CRUD (padrão DriverDB, igual a Program.DbCon)
dotnet_mvc.Models.DAL.DriverDB.ConnectionString = connectionString;

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connectionString,
        serverVersion
    )
);

// Add services to the container.
builder.Services
    .AddControllersWithViews(options =>
    {
        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IAccessCodeService, AccessCodeService>();
builder.Services.AddScoped<IAcademicManagementService, AcademicManagementService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<IMonitoringService, MonitoringService>();
builder.Services.AddScoped<IStudentDashboardService, StudentDashboardService>();
builder.Services.AddScoped<IScenarioService, ScenarioService>();
builder.Services.AddScoped<IFinancialEntryService, FinancialEntryService>();
builder.Services.AddScoped<ISavingsGoalService, SavingsGoalService>();
builder.Services.AddScoped<ISimulationHistoryService, SimulationHistoryService>();
builder.Services.AddHttpClient();

// Cliente HTTP tipado para o microserviço de cálculo financeiro (API Django).
// O portal NÃO calcula localmente — delega na API, conforme a arquitetura do sistema.
var financeApiBaseUrl = builder.Configuration["FinanceApi:BaseUrl"]
    ?? throw new InvalidOperationException("A configuração 'FinanceApi:BaseUrl' não está definida.");
builder.Services.AddHttpClient<IFinanceApiClient, FinanceApiClient>(client =>
{
    client.BaseAddress = new Uri(financeApiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(15);
    // Forçar HTTP/1.1 — o Nginx local não anuncia HTTP/2 no ALPN.
    client.DefaultRequestVersion = System.Net.HttpVersion.Version11;
    client.DefaultVersionPolicy = System.Net.Http.HttpVersionPolicy.RequestVersionExact;
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    // Certificado auto-assinado do Nginx local.
    ServerCertificateCustomValidationCallback =
        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});

builder.Services
    .AddAuthentication(AuthConstants.AuthenticationScheme)
    .AddCookie(AuthConstants.AuthenticationScheme, options =>
    {
        options.Cookie.Name = "dec.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = AuthConstants.SessionLifetime;
        options.SlidingExpiration = false;
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}
await SeedData.SeedDemoAsync(app.Services);

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run();
