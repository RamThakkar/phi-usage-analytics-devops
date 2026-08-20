using PhiUsageAnalytics.Api.Middleware;
using PhiUsageAnalytics.Api.Services;
using PhiUsageAnalytics.Application;
using PhiUsageAnalytics.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Session service (singleton — in-memory token store)
builder.Services.AddSingleton<SessionService>();

// Error logger (singleton — file-based logging)
builder.Services.AddSingleton<ErrorLogger>();

// Visitor logger (singleton — tracks login/logout)
builder.Services.AddSingleton<VisitorLogger>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Middleware pipeline (order matters!)
app.UseCors();
app.UseHttpsRedirection();                     // 0. Redirect HTTP → HTTPS
app.UseMiddleware<ErrorHandlingMiddleware>();   // 1. Catch all errors first
app.UseMiddleware<RateLimitMiddleware>();       // 2. Rate limit
app.UseDefaultFiles();                          // 3. Serve index.html
app.UseStaticFiles();                           // 4. Serve static files
app.UseMiddleware<AuthMiddleware>();            // 5. Auth check for API calls
app.MapControllers();

app.Run();
