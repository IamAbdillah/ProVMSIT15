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
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
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
    // ── Core role policies ────────────────────────────────────────────────
    options.AddPolicy("AdminOnly",          p => p.RequireRole("Admin"));
    options.AddPolicy("FinanceOnly",        p => p.RequireRole("Finance"));
    options.AddPolicy("VendorOnly",         p => p.RequireRole("Vendor"));
    options.AddPolicy("RequesterOnly",      p => p.RequireRole("User"));
    options.AddPolicy("AllAuthenticated",   p => p.RequireAuthenticatedUser());

    // ── Composite policies ────────────────────────────────────────────────
    // Accreditation Desk write: Admin C,R,U,D | Procurement R,U
    options.AddPolicy("ProcurementOrAdmin", p => p.RequireRole("Admin", "Procurement"));
    // Procurement-only actions (matrix Admin=X): IssuePO, Requisition Allocation
    options.AddPolicy("ProcurementOnly",    p => p.RequireRole("Procurement"));
    // Finance approval + Admin read
    options.AddPolicy("FinanceOrAdmin",     p => p.RequireRole("Finance", "Admin"));
    // All internal staff (no Vendor)
    options.AddPolicy("InternalUsers",      p => p.RequireRole("Admin", "Procurement", "Finance", "User"));

    // ── RBAC Matrix policies ─────────────────────────────────────────────
    // Analytics / Dashboard / Evaluation views / Contract read:
    //   Admin R | Finance R | Procurement R  (User=X, Vendor=X)
    options.AddPolicy("AnalyticsViewers",   p => p.RequireRole("Admin", "Finance", "Procurement"));
    // Vendor Directory: Admin C,R,U | Procurement C,R,U | Finance R | User R  (Vendor=X)
    options.AddPolicy("DirectoryViewers",   p => p.RequireRole("Admin", "Finance", "Procurement", "User"));
    // PO Vault: Admin R | Procurement R,U | Finance R | User R(own) | Vendor R,U(own)
    options.AddPolicy("POVaultViewers",     p => p.RequireRole("Admin", "Finance", "Procurement", "User", "Vendor"));
    // Delivery Tracking: Admin R | Procurement R,U | User R(own) | Vendor R,U(dispatch)  (Finance=X)
    options.AddPolicy("DeliveryViewers",    p => p.RequireRole("Admin", "Procurement", "User", "Vendor"));
});

builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<BudgetGuardService>();
builder.Services.AddScoped<PdfService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddHttpContextAccessor();

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

    // ── SEED: Default Department Budgets for current fiscal year ──────────
    int fy = DateTime.UtcNow.Year;
    var defaultBudgets = new[]
    {
        ("IT",          "Information Technology",  5_000_000m),
        ("HR",          "Human Resources",         2_000_000m),
        ("FINANCE",     "Finance Department",      3_000_000m),
        ("OPS",         "Operations",              4_000_000m),
        ("PROCUREMENT", "Procurement Division",    6_000_000m),
        ("SYS",         "System Administration",   1_000_000m),
    };
    foreach (var (code, name, alloc) in defaultBudgets)
    {
        if (!db.DepartmentBudgets.Any(b => b.DepartmentCode == code && b.FiscalYear == fy))
        {
            db.DepartmentBudgets.Add(new ProVMSIT15.Models.DepartmentBudget
            {
                DepartmentCode = code,
                DepartmentName = name,
                FiscalYear     = fy,
                AllocatedBudget = alloc,
                SpentAmount    = 0,
                UpdatedAt      = DateTime.UtcNow
            });
            Console.WriteLine($"[SEED] Budget seeded → {code} FY{fy} ₱{alloc:N0}");
        }
    }
    db.SaveChanges();
}

app.Run();
