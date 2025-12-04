using CSE325_visioncoders.Components;
using CSE325_visioncoders.Models;
using CSE325_visioncoders.Services;
using System.Net.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/login";
        o.LogoutPath = "/logout";
        o.ExpireTimeSpan = TimeSpan.FromDays(7);
        o.SlidingExpiration = true;
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Lax;
        o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });
builder.Services.AddAuthorization();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register existing app services
builder.Services.AddSingleton<MealService>();
builder.Services.AddSingleton<CalendarService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddSingleton<IOrderSettingsService, OrderSettingsService>(); 

// MongoDB settings + UserService for auth
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));

builder.Services.AddSingleton<UserService>();

builder.Services.AddScoped<AuthService>();
builder.Services.AddHttpClient();  

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();
app.MapStaticAssets();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// ---------- AUTH APIs ----------

// POST: /api/register
app.MapPost("/api/register", async (RegisterRequest req, UserService userService) =>
{
    // Check if email already exists
    var existing = await userService.GetByEmailAsync(req.Email);
    if (existing != null)
    {
        return Results.BadRequest("Email already exists.");
    }

    var user = new User
    {
        Name = req.Name,
        Email = req.Email,
        PasswordHash = PasswordHasher.Hash(req.Password),
        Role = string.IsNullOrWhiteSpace(req.Role) ? "customer" : req.Role
    };

    await userService.CreateUserAsync(user);

    return Results.Ok(new
    {
        message = "Registered successfully.",
        userId = user.Id,
        user.Name,
        user.Role
    });
});

// POST: /api/login
app.MapPost("/api/login", async (LoginRequest req, UserService userService) =>
{
    var user = await userService.GetByEmailAsync(req.Email);
    if (user == null || !PasswordHasher.Verify(req.Password, user.PasswordHash))
    {
        return Results.BadRequest(new LoginResponse
        {
            Success = false,
            Message = "Invalid email or password."
        });
    }

    return Results.Ok(new LoginResponse
    {
        Success = true,
        Message = "Login successful.",
        UserId = user.Id,
        Name = user.Name,
        Role = user.Role
    });
});

app.MapPost("/auth/login-form", async (HttpContext http,
                                       UserService userService,
                                       [FromForm] LoginRequest req,
                                       [FromQuery] string? returnUrl) =>
{
    if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
    {
        var back = "/login?error=missing";
        if (!string.IsNullOrWhiteSpace(returnUrl) && Uri.IsWellFormedUriString(returnUrl, UriKind.Relative))
            back += $"&returnUrl={Uri.EscapeDataString(returnUrl)}";
        return Results.Redirect(back);
    }

    var user = await userService.GetByEmailAsync(req.Email);
    if (user is null || !PasswordHasher.Verify(req.Password, user.PasswordHash))
    {
        var back = "/login?error=invalid";
        if (!string.IsNullOrWhiteSpace(returnUrl) && Uri.IsWellFormedUriString(returnUrl, UriKind.Relative))
            back += $"&returnUrl={Uri.EscapeDataString(returnUrl)}";
        return Results.Redirect(back);
    }

    // credenciales OK: crea cookie
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id ?? string.Empty),
        new Claim(ClaimTypes.Name, user.Name ?? user.Email),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role)
    };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

    // destino por rol
    string redirect = user.Role.Equals("cook", StringComparison.OrdinalIgnoreCase)
        ? "/cook/dashboard"
        : "/customer/dashboard";

    // returnUrl relativo tiene prioridad
    if (!string.IsNullOrWhiteSpace(returnUrl) && Uri.IsWellFormedUriString(returnUrl, UriKind.Relative))
        redirect = returnUrl!;

    return Results.Redirect(redirect);
})
.DisableAntiforgery();

// POST: /auth/logout
app.MapPost("/auth/logout", async (HttpContext http, [FromQuery] string? returnUrl) =>
{
    await http.SignOutAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);

    // Redirige a login por defecto, o al returnUrl si viene y es relativo
    var dest = "/login";
    if (!string.IsNullOrWhiteSpace(returnUrl) && Uri.IsWellFormedUriString(returnUrl, UriKind.Relative))
        dest = returnUrl;
    return Results.Redirect(dest);
})
.DisableAntiforgery();

// GET: /auth/me
app.MapGet("/auth/me", (HttpContext http) =>
{
    var user = http.User;
    if (user?.Identity?.IsAuthenticated != true)
        return Results.Unauthorized();

    var id = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
    var name = user.FindFirstValue(ClaimTypes.Name) ?? "";
    var email = user.FindFirstValue(ClaimTypes.Email) ?? "";
    var role = user.FindFirstValue(ClaimTypes.Role) ?? "customer";

    return Results.Ok(new
    {
        success = true,
        userId = id,
        name,
        role
    });
});

app.Run();
