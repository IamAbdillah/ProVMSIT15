using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProVMSIT15.Data;
using ProVMSIT15.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

var jwtKey = builder.Configuration["Jwt:Key"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = builder.Configuration["Jwt:Audience"]!;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Auth/Login";
    options.LogoutPath = "/Auth/Logout";
    options.AccessDeniedPath = "/Auth/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(480);
    options.SlidingExpiration = true;
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = ctx =>
        {
            ctx.Token = ctx.Request.Cookies["provms_jwt"];
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly",         p => p.RequireRole("Admin"));
    options.AddPolicy("ProcurementOrAdmin",p => p.RequireRole("Admin", "Procurement"));
    options.AddPolicy("FinanceOnly",       p => p.RequireRole("Finance"));
    options.AddPolicy("InternalUsers",     p => p.RequireRole("Admin", "Procurement", "Finance", "User"));
    options.AddPolicy("VendorOnly",        p => p.RequireRole("Vendor"));
    options.AddPolicy("AllAuthenticated",  p => p.RequireAuthenticatedUser());
    // RBAC spec: Marketplace & order placement = User role only
    options.AddPolicy("RequesterOnly",     p => p.RequireRole("User"));
    // RBAC spec: Analytics = Admin, Finance, Procurement (not User, not Vendor)
    options.AddPolicy("AnalyticsViewers",  p => p.RequireRole("Admin", "Finance", "Procurement"));
    // RBAC spec: PO Vault read = Admin, Finance, Procurement, User, Vendor(own)
    options.AddPolicy("POVaultViewers",    p => p.RequireRole("Admin", "Finance", "Procurement", "User", "Vendor"));
    // RBAC spec: Vendor Directory = Admin, Finance, Procurement (not plain User)
    options.AddPolicy("DirectoryViewers",  p => p.RequireRole("Admin", "Finance", "Procurement"));
    // RBAC spec: Finance approval read also available to Procurement
    options.AddPolicy("FinanceOrAdmin",    p => p.RequireRole("Finance", "Admin"));
});

builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<BudgetGuardService>();
builder.Services.AddScoped<PdfService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();

    const string adminEmail = "admin@provms.com";
    const string adminPassword = "Admin@ProVMS2026!";
    var existingAdmin = db.Users.FirstOrDefault(u => u.Email == adminEmail);
    if (existingAdmin == null)
    {
        db.Users.Add(new ProVMSIT15.Models.AppUser
        {
            FullName       = "ProVMS Global System Admin",
            Email          = adminEmail,
            PasswordHash   = BCrypt.Net.BCrypt.HashPassword(adminPassword),
            UserRole       = ProVMSIT15.Models.UserRole.Admin,
            DepartmentCode = "SYS",
            CreatedAt      = DateTime.UtcNow
        });
        db.SaveChanges();
        Console.WriteLine($"[SEED] SysAdmin created → {adminEmail}");
    }
    else
    {
        existingAdmin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword);
        existingAdmin.UserRole     = ProVMSIT15.Models.UserRole.Admin;
        db.SaveChanges();
        Console.WriteLine($"[SEED] SysAdmin password reset → {adminEmail}");
    }
}

app.Run();
