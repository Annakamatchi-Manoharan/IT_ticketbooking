using System.Text;
using ITBookingSystem.Data;
using ITBookingSystem.Hubs;
using ITBookingSystem.Options;
using ITBookingSystem.Repositories;
using ITBookingSystem.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Hosting;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, services, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    builder.Services.AddControllersWithViews();

    var defaultConn = builder.Configuration.GetConnectionString("DefaultConnection")
                      ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection");
    builder.Services.AddDbContextPool<AppDbContext>((sp, options) =>
        options.UseSqlServer(defaultConn, sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(2), null))
               .UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll));

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<AppDbContext>();

    builder.Services.AddDataProtection();

    builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
    builder.Services.Configure<MlServiceOptions>(builder.Configuration.GetSection(MlServiceOptions.SectionName));
    builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

    builder.Services.AddSession(opts =>
    {
        opts.IdleTimeout = TimeSpan.FromHours(8);
        opts.Cookie.HttpOnly = true;
        opts.Cookie.IsEssential = true;
    });

    builder.Services.AddSignalR();

    var jwtSecret = builder.Configuration["Jwt:Secret"]
                    ?? "DEV_ONLY_CHANGE_ME__PLEASE_SET_JWT_SECRET_32CHARS_MIN";

    builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        })
        .AddCookie(opts =>
        {
            opts.LoginPath = "/Auth/Login";
            opts.AccessDeniedPath = "/Auth/Denied";
            opts.ExpireTimeSpan = TimeSpan.FromHours(8);
            opts.SlidingExpiration = true;
        })
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, opts =>
        {
            opts.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
            };
        });

    builder.Services.AddAuthorization();

    // Repositories / Services - keep scoped to avoid shared mutable state
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<ITicketRepository, TicketRepository>();
    builder.Services.AddScoped<ITicketHistoryRepository, TicketHistoryRepository>();
    builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
    builder.Services.AddScoped<IChatbotRepository, ChatbotRepository>();

    builder.Services.AddHttpClient<IMlPredictionService, MlPredictionService>((sp, client) =>
    {
        var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MlServiceOptions>>().Value;
        client.BaseAddress = new Uri(opts.BaseUrl.TrimEnd('/') + "/");
        client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
    });

    builder.Services.AddScoped<IChatbotService, ChatbotService>();

    builder.Services.AddScoped<AuthService>();
    builder.Services.AddScoped<SLAService>();
    builder.Services.AddScoped<TicketService>();
    builder.Services.AddScoped<AssignmentEngineService>();
    builder.Services.AddScoped<EngineerAvailabilityService>();
    builder.Services.AddScoped<DashboardService>();
    builder.Services.AddScoped<EnterpriseNotificationService>();
    builder.Services.AddScoped<EscalationService>();
    builder.Services.AddScoped<TicketRealtimePublisher>();

    builder.Services.AddHostedService<OverdueTicketBackgroundService>();

    var app = builder.Build();

    app.UseExceptionHandler("/Home/Error");
    if (!app.Environment.IsDevelopment())
        app.UseHsts();

    // Apply migrations and seed data with robust error handling. Failures here should not crash the host.
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            try
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                // Apply migrations and seed initial data
                await DbSeeder.SeedAsync(db);
                await DbSeeder.SyncAgentWorkloadAsync(db);
                logger.LogInformation("Database migrations and seeding completed.");
            }
            catch (Exception ex)
            {
                // Log but don't rethrow - allow the app to start so developer can see UI and errors
                logger.LogError(ex, "Database initialization failed. The application will continue running with reduced functionality.");
            }
        }
    }
    catch (Exception ex)
    {
        // Global safety net
        Log.Error(ex, "Failed during startup database initialization.");
    }

    app.UseSerilogRequestLogging();
    app.Use(async (context, next) =>
    {
        try
        {
            await next();
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Unhandled request exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            // Return generic 500 if not handled by developer exception page
            if (!app.Environment.IsDevelopment())
            {
                context.Response.Redirect("/Home/Error");
                return;
            }
            throw;
        }
    });

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();

    app.UseSession();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapHealthChecks("/health");

    app.MapHub<TicketHub>("/hubs/tickets");

    app.MapControllers();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
