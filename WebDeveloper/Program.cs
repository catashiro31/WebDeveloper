using Microsoft.AspNetCore.Authentication.JwtBearer;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WebDeveloper.BackgroundServices;
using WebDeveloper.Data;
using WebDeveloper.Middleware;
using WebDeveloper.Security;
using WebDeveloper.Services;
using WebDeveloper.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// 1. Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 1.5 Add Redis
builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(sp => 
    StackExchange.Redis.ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379"));

// 2. Add JWT Configuration & Authentication
builder.Services.AddSingleton<AccessTokenProvider>();
builder.Services.AddSingleton<AuthCookieSettings>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"] ?? ""))
        };
        // Config for Token from Cookie and 401 handling
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = async context =>
            {
                var token = context.Request.Cookies["accessToken"];
                if (!string.IsNullOrEmpty(token))
                {
                    var provider = context.HttpContext.RequestServices.GetRequiredService<AccessTokenProvider>();
                    var expiry = provider.GetExpiryDateFromToken(token);
                    if (expiry == null || expiry > DateTime.UtcNow ||
                        context.Request.Path.StartsWithSegments("/api/v1/auth"))
                    {
                        context.Token = token;
                        return;
                    }
                }

                // An access cookie expires after 15 minutes. Restore the browser
                // session from its HttpOnly refresh cookie on the next request.
                var refreshToken = context.Request.Cookies["refreshToken"];
                if (string.IsNullOrEmpty(refreshToken) ||
                    context.Request.Path.StartsWithSegments("/api/v1/auth"))
                    return;

                try
                {
                    var auth = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();
                    // Concurrent page/API requests share the same browser session.
                    // Only the explicit refresh endpoint rotates its refresh token.
                    var refreshed = await auth.RefreshToken(refreshToken, rotateRefreshToken: false);
                    var secure = context.HttpContext.RequestServices.GetRequiredService<AuthCookieSettings>().Secure;
                    context.Response.Cookies.Append("accessToken", refreshed.Token, new CookieOptions
                    {
                        HttpOnly = true, Secure = secure, SameSite = SameSiteMode.Strict,
                        Expires = DateTimeOffset.UtcNow.AddMinutes(15)
                    });
                    context.Token = refreshed.Token;
                    context.HttpContext.Items["AuthenticatedAccessToken"] = refreshed.Token;
                }
                catch (InvalidOperationException)
                {
                    // Invalid or expired refresh token: continue as a guest.
                }
            },
            OnChallenge = context =>
            {
                context.HandleResponse();
                if (!context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.Redirect("/Auth/Login?reason=session");
                    return Task.CompletedTask;
                }
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync("{\"error\": \"Bạn cần đăng nhập để truy cập tài nguyên này.\"}");
            }
        };
    });

// 3. Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// 4. Add Services (Dependency Injection)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IDoctorService, DoctorService>();
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IPublicService, PublicService>();
builder.Services.AddScoped<IAppointmentJobService, AppointmentJobService>();

// 5. Add Background Services & Caching
builder.Services.AddMemoryCache();
// builder.Services.AddHostedService<AppointmentCleanupService>();

// Configure Hangfire with PostgreSQL
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHangfireServer();

builder.Services.AddControllersWithViews();
// Add API controllers support specifically
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAll");

app.UseHangfireDashboard("/hangfire");

// Custom Global Exception Middleware
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseAuthentication();

// Custom JWT Middleware
app.UseMiddleware<JwtMiddleware>();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Cấu hình map attribute routes cho API
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

app.Run();
