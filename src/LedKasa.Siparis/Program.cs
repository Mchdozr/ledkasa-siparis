using System.Globalization;
using FluentValidation;
using LedKasa.Siparis.Components;
using LedKasa.Siparis.Data;
using LedKasa.Siparis.Features.Catalog;
using LedKasa.Siparis.Features.Dashboard;
using LedKasa.Siparis.Features.Notifications;
using LedKasa.Siparis.Features.Orders;
using LedKasa.Siparis.Features.Orders.Domain;
using LedKasa.Siparis.Features.Reports;
using LedKasa.Siparis.Identity;
using LedKasa.Siparis.Security;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

var culture = new CultureInfo("tr-TR");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Services.AddHttpContextAccessor();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddValidatorsFromAssemblyContaining<OrderDraftValidator>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection tanımlı değil.");

var serverVersion = ServerVersion.Parse("10.4.34-mariadb");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, serverVersion));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 1;
        options.Password.RequireDigit = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredUniqueChars = 0;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.User.RequireUniqueEmail = false;
        options.User.AllowedUserNameCharacters = string.Empty;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddClaimsPrincipalFactory<AppClaimsPrincipalFactory>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "ledkasa.siparis.auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/login";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(12);
});

builder.Services.AddAuthorization(options => options.AddAppPolicies());

builder.Services.Configure<TelegramOptions>(builder.Configuration.GetSection(TelegramOptions.Section));
builder.Services.AddHttpClient<ITelegramNotifier, TelegramNotifier>(client =>
{
    client.BaseAddress = new Uri("https://api.telegram.org/");
    client.Timeout = TimeSpan.FromSeconds(8);
});
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IExtraFeatureService, ExtraFeatureService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IUserAdminService, UserAdminService>();
builder.Services.AddSingleton<IExcelReportExporter, ExcelReportExporter>();
builder.Services.AddSingleton<IPdfReportExporter, PdfReportExporter>();

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

if (!app.Environment.IsDevelopment()
    && !string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true", StringComparison.OrdinalIgnoreCase))
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<InactiveUserMiddleware>();
app.UseAntiforgery();

app.MapPost("/account/login", LoginAsync).AllowAnonymous();

app.MapPost("/account/logout", async (SignInManager<ApplicationUser> signIn) =>
{
    await signIn.SignOutAsync();
    return Results.Redirect("/login");
}).RequireAuthorization();

app.MapGet("/exports/orders.xlsx", ExportExcelAsync).RequireAuthorization(Policies.ReportsView);
app.MapGet("/exports/orders.pdf", ExportPdfAsync).RequireAuthorization(Policies.ReportsView);

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

if (!app.Environment.IsEnvironment("Testing"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
    await IdentitySeeder.SeedAsync(app.Services, app.Configuration, app.Logger);
    await DemoOrderSeeder.SeedAsync(app.Services, app.Logger);
}

app.Run();

static async Task<IResult> LoginAsync(
    HttpContext http,
    SignInManager<ApplicationUser> signIn,
    UserManager<ApplicationUser> users)
{
    var form = await http.Request.ReadFormAsync();
    var login = form["login"].ToString();
    if (string.IsNullOrWhiteSpace(login))
        login = form["email"].ToString();
    var password = form["password"].ToString();
    var returnUrl = form["returnUrl"].ToString();

    var user = await users.FindByNameAsync(login)
        ?? await users.FindByEmailAsync(login);
    if (user is null || !user.IsActive)
        return Results.Redirect("/login?error=1");

    var result = await signIn.PasswordSignInAsync(user, password, isPersistent: true, lockoutOnFailure: true);
    if (result.IsLockedOut)
        return Results.Redirect("/login?locked=1");
    if (!result.Succeeded)
        return Results.Redirect("/login?error=1");

    var target = !string.IsNullOrWhiteSpace(returnUrl)
        && Uri.IsWellFormedUriString(returnUrl, UriKind.Relative)
        && returnUrl.StartsWith('/')
            ? returnUrl
            : "/";
    return Results.Redirect(target);
}

static async Task<IResult> ExportExcelAsync(
    IReportService reports,
    IExcelReportExporter exporter,
    ReportPeriod period = ReportPeriod.Daily,
    ReportDateField dateField = ReportDateField.OrderDate,
    DateOnly? anchor = null,
    DeliveryPlace? place = null,
    OrderStatus[]? status = null,
    string? customer = null)
{
    var result = await reports.GetAsync(new ReportRequest
    {
        Period = period,
        DateField = dateField,
        Anchor = anchor ?? LedKasa.Siparis.Common.TurkeyTime.Today,
        DeliveryPlace = place,
        Statuses = status ?? [],
        CustomerName = customer
    });
    var bytes = exporter.Export(result);
    return Results.File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        $"ledkasa-siparis-{result.From:yyyyMMdd}-{result.To:yyyyMMdd}.xlsx");
}

static async Task<IResult> ExportPdfAsync(
    IReportService reports,
    IPdfReportExporter exporter,
    ReportPeriod period = ReportPeriod.Daily,
    ReportDateField dateField = ReportDateField.OrderDate,
    DateOnly? anchor = null,
    DeliveryPlace? place = null,
    OrderStatus[]? status = null,
    string? customer = null)
{
    var result = await reports.GetAsync(new ReportRequest
    {
        Period = period,
        DateField = dateField,
        Anchor = anchor ?? LedKasa.Siparis.Common.TurkeyTime.Today,
        DeliveryPlace = place,
        Statuses = status ?? [],
        CustomerName = customer
    });
    var bytes = exporter.Export(result);
    return Results.File(bytes, "application/pdf",
        $"ledkasa-siparis-{result.From:yyyyMMdd}-{result.To:yyyyMMdd}.pdf");
}

public partial class Program;
