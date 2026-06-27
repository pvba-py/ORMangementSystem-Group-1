using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ORManagement.Api.Middleware;
using ORManagement.Application.DTOs.Requests;
using ORManagement.Application.Engines;
using ORManagement.Application.Interfaces.Repositories;
using ORManagement.Application.Interfaces.Services;
using ORManagement.Application.Services;
using ORManagement.Infrastructure.AI;
using ORManagement.Infrastructure.Data;
using ORManagement.Infrastructure.Repositories;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

//Add Cors Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173") // Vue dev server
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials() // REQUIRED for cookies
            .WithExposedHeaders("X-Data-Source");
    });
});

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(item => item.Value?.Errors.Count > 0)
            .SelectMany(item => item.Value!.Errors)
            .Select(error => error.ErrorMessage)
            .ToList();

        return new BadRequestObjectResult(new
        {
            success = false,
            errorCode = "VALIDATION_ERROR",
            message = errors.FirstOrDefault() ?? "Invalid request.",
            errors
        });
    };
});

//Register Controllers
builder.Services.AddControllers();

//Register Memory Cache
builder.Services.AddMemoryCache();

//Register the DbContext with the connection string from appsettings.json
builder.Services.AddDbContext<ORManagementDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ORManagementConnection")));

// Add services to the container.
// Auth
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Audit
builder.Services.AddScoped<IAuditRepository, AuditRepository>();
builder.Services.AddScoped<IAuditService, AuditService>();

// Master Data
builder.Services.AddScoped<IMasterDataRepository, MasterDataRepository>();
builder.Services.AddScoped<IMasterDataService, MasterDataService>();

// Rooms
builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<IRoomService, RoomService>();

// Engines
builder.Services.AddScoped<AvailabilityWindowEngine>();
builder.Services.AddScoped<PriorityScoreEngine>();
builder.Services.AddScoped<ForecastRecommendationEngine>();

// Requests
builder.Services.AddScoped<IRequestRepository, RequestRepository>();
builder.Services.AddScoped<IRequestService, RequestService>();

// Clinical Text Scoring: ONNX ClinicalBERT first, fallback keyword scorer if ONNX fails
builder.Services.Configure<ClinicalScoringOptions>(
    builder.Configuration.GetSection("ClinicalScoring"));

builder.Services.AddSingleton<FallbackClinicalKeywordScoringService>();

builder.Services.AddSingleton<IClinicalTextScoringService, ClinicalBertOnnxTextScoringService>();

// Cycles
builder.Services.AddScoped<ICycleRepository, CycleRepository>();
builder.Services.AddScoped<ISchedulingCycleService, SchedulingCycleService>();

// Cases
builder.Services.AddScoped<ICaseRepository, CaseRepository>();
builder.Services.AddScoped<ICaseService, CaseService>();

// Blocks
builder.Services.AddScoped<IBlockRepository, BlockRepository>();
builder.Services.AddScoped<IBlockService, BlockService>();

// Waitlist
builder.Services.AddScoped<IWaitlistRepository, WaitlistRepository>();
builder.Services.AddScoped<IWaitlistService, WaitlistService>();

// Utilization
builder.Services.AddScoped<IUtilizationRepository, UtilizationRepository>();
builder.Services.AddScoped<IUtilizationService, UtilizationService>();

// Dashboard
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// Settings
builder.Services.AddScoped<ISettingsRepository, SettingsRepository>();
builder.Services.AddScoped<ISettingsService, SettingsService>();

// Forecast
builder.Services.AddScoped<IForecastRepository, ForecastRepository>();
builder.Services.AddScoped<IForecastService, ForecastService>();

// Notification Stack
builder.Services.AddScoped<INotificationStackRepository, NotificationStackRepository>();
builder.Services.AddScoped<INotificationStackService, NotificationStackService>();

//Automation
builder.Services.AddScoped<IAutoSchedulingService, AutoSchedulingService>();
//Used Cycles Controller 

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if(string.IsNullOrWhiteSpace(jwtKey) || string.IsNullOrWhiteSpace(jwtIssuer) || string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException("JWT configuration is missing. Please check appsettings.json.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(2) // Optional: reduce default clock skew
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessTokenFromCookie = context.Request.Cookies["or_access_token"];
                if (!string.IsNullOrEmpty(accessTokenFromCookie))
                {
                    context.Token = accessTokenFromCookie;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Add global exception handling middleware
app.UseMiddleware<ExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("FrontendPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();



