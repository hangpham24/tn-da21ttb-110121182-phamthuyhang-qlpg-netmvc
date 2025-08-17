using GymManagement.Web.Data;
using GymManagement.Web.Data.Repositories;
using GymManagement.Web.Services;
using GymManagement.Web.Middleware;
using GymManagement.Web.Models;
using GymManagement.Web.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using GymManagement.Web.Data.Models;
using Serilog;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog with proper hosting information display
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day));

// Add services to the container.
builder.Services.AddControllersWithViews();

// ✅ ENHANCED: Add HttpClientFactory for notifications
builder.Services.AddHttpClient();

// Add Antiforgery services
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
    options.Cookie.Name = "__RequestVerificationToken";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Add Entity Framework with SQL Server
builder.Services.AddDbContext<GymDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("GymDb")));

// Add Custom Authentication Services
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Add Authentication
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "Cookies";
        options.DefaultChallengeScheme = "Cookies";
    })
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
        options.SlidingExpiration = true;
    })
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
        options.CallbackPath = "/signin-google";
        options.SaveTokens = true;
        options.SignInScheme = "Cookies"; // Important: Sign in to Cookies scheme after Google auth
        options.Events.OnCreatingTicket = async context =>
        {
            // Lưu thông tin từ Google vào claims
            var user = context.Principal;
            if (user?.Identity?.IsAuthenticated == true)
            {
                // Các claims sẽ được xử lý trong AuthController
            }
        };
    });

// Configure cookie settings
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.LoginPath = "/Auth/Login";
    options.LogoutPath = "/Auth/Logout";
    options.AccessDeniedPath = "/Auth/AccessDenied";
    options.SlidingExpiration = true;
});

// Add Authorization Policies
builder.Services.AddAuthorization(options =>
{
    // Class Management Policies
    options.AddPolicy("CanManageClasses", policy =>
        policy.RequireRole("Admin"));

    options.AddPolicy("CanViewClasses", policy =>
        policy.RequireRole("Admin", "Trainer", "Member"));

    options.AddPolicy("CanViewOwnClasses", policy =>
        policy.RequireRole("Trainer", "Member"));

    // Registration Policies
    options.AddPolicy("CanRegisterClasses", policy =>
        policy.RequireRole("Member"));

    // Attendance Policies
    options.AddPolicy("CanManageAttendance", policy =>
        policy.RequireRole("Admin", "Trainer"));

    // Reporting Policies
    options.AddPolicy("CanViewReports", policy =>
        policy.RequireRole("Admin"));
});

// Add Authorization Handlers
builder.Services.AddScoped<IAuthorizationHandler, BookingAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, BookingOperationAuthorizationHandler>();

// Add Repository pattern
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<INguoiDungRepository, NguoiDungRepository>();
builder.Services.AddScoped<IGoiTapRepository, GoiTapRepository>();
builder.Services.AddScoped<ILopHocRepository, LopHocRepository>();
builder.Services.AddScoped<IDangKyRepository, DangKyRepository>();
builder.Services.AddScoped<IThanhToanRepository, ThanhToanRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IDiemDanhRepository, DiemDanhRepository>();
builder.Services.AddScoped<IMauMatRepository, MauMatRepository>();
builder.Services.AddScoped<IBangLuongRepository, BangLuongRepository>();
builder.Services.AddScoped<IThongBaoRepository, ThongBaoRepository>();
builder.Services.AddScoped<ITinTucRepository, TinTucRepository>();

// Add Services
builder.Services.AddScoped<INguoiDungService, NguoiDungService>();
builder.Services.AddScoped<IGoiTapService, GoiTapService>();
builder.Services.AddScoped<ILopHocService, LopHocService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IDangKyService, DangKyService>();
builder.Services.AddScoped<IThanhToanService, ThanhToanService>();
builder.Services.AddScoped<IDiemDanhService, DiemDanhService>();
builder.Services.AddScoped<IBangLuongService, BangLuongService>();
builder.Services.AddScoped<IBaoCaoService, BaoCaoService>();
builder.Services.AddScoped<IThongBaoService, ThongBaoService>();
builder.Services.AddScoped<ITinTucService, TinTucService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IPdfExportService, PdfExportService>();
builder.Services.AddScoped<IAdvancedAnalyticsService, AdvancedAnalyticsService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IKhuyenMaiService, KhuyenMaiService>();
builder.Services.AddScoped<VietQRService>();

// Add Walk-In Services
builder.Services.AddScoped<IWalkInService, WalkInService>();

// Add Data Fix Service
builder.Services.AddScoped<DataFixService>();

// Add Face Recognition Services
builder.Services.AddScoped<IFaceRecognitionService, FaceRecognitionService>();

// Add Background Services
builder.Services.AddHostedService<ExpiryNotificationBackgroundService>();

// Add Memory Cache
builder.Services.AddMemoryCache();

// Configure Commission Settings
builder.Services.Configure<CommissionConfiguration>(
    builder.Configuration.GetSection("CommissionSettings"));

// Add Session Support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Add HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Add User Session Service
builder.Services.AddScoped<IUserSessionService, UserSessionService>();

// Add Trainer Security Service - Enhanced security for Trainer role
builder.Services.AddScoped<ITrainerSecurityService, TrainerSecurityService>();

// Add Member Benefit Service - Logic đơn giản cho quyền lợi member
builder.Services.AddScoped<IMemberBenefitService, MemberBenefitService>();

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<GymDbContext>();

// Configure forwarded headers for reverse proxy
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseStatusCodePagesWithReExecute("/Error/{0}");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseForwardedHeaders();

// Global exception handling (should be early in pipeline)
app.UseGlobalExceptionHandling();

app.UseHttpsRedirection();

// Configure static files with custom MIME types for Face-API.js models
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        var path = ctx.File.Name.ToLower();
        if (path.Contains("shard") || path.Contains("model"))
        {
            ctx.Context.Response.Headers.Append("Content-Type", "application/octet-stream");
        }
    }
});

app.UseRouting();
app.UseSession();

// Rate limiting for face recognition endpoints
app.UseMiddleware<RateLimitingMiddleware>();

app.UseAuthentication();
app.UseUserSession(); // Custom middleware for user session management
app.UseTrainerSecurity(); // Enhanced security for Trainer role
app.UseAuthorization();

// Add Health Check endpoint
app.MapHealthChecks("/health");

// Add Areas routing
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<GymDbContext>();
    var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
    await DbInitializer.InitializeAsync(context, authService);
}

try
{
    Log.Information("Starting web application");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class accessible for integration tests
public partial class Program { }
