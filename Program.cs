using Ecoeex_Academy_Api.Data;
using Ecoeex_Academy_Api.Services;
using Ecoex_Academy_Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;
using System.Text;

var builder = WebApplication.CreateBuilder(args);


// ============================================================
// CONTROLLERS
// ============================================================
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// ============================================================
// HTTP CONTEXT ACCESSOR
// ============================================================

builder.Services.AddHttpContextAccessor();


// ============================================================
// APPLICATION SERVICES
// ============================================================

builder.Services.AddScoped<
    IEmail_Services,
    Email_Services
>();

builder.Services.AddScoped<
    ICertificateServices,
    CertificateServices
>();

builder.Services.AddHostedService<CertificateBackgroundService>();

// ============================================================
// HTTP CLIENT
// ============================================================

builder.Services.AddHttpClient();


// ============================================================
// DATABASE
// ============================================================

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "DefaultConnection"
        ),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null
            );
        }
    );
});


// ============================================================
// JWT AUTHENTICATION
// ============================================================

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme
    )
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                // Validate issuer
                ValidateIssuer = true,

                // Validate audience
                ValidateAudience = true,

                // Validate token expiry
                ValidateLifetime = true,

                // Validate signing key
                ValidateIssuerSigningKey = true,

                // Issuer
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],

                IssuerSigningKey = new SymmetricSecurityKey(
    Encoding.UTF8.GetBytes(
        builder.Configuration["Jwt:Key"]!
    )
)
            };
    });

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;
});
// ============================================================
// AUTHORIZATION
// ============================================================

builder.Services.AddAuthorization();



builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularApp", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
QuestPDF.Settings.License = LicenseType.Community; // add this line

// ============================================================
// BUILD APPLICATION
// ============================================================

var app = builder.Build();

app.UseForwardedHeaders();

app.UseSwagger();
app.UseSwaggerUI();

// ============================================================
// HTTPS
// ============================================================

app.UseHttpsRedirection();
app.UseCors("AngularApp");

// ============================================================
// AUTHENTICATION
// ============================================================

app.UseAuthentication();

// ============================================================
// AUTHORIZATION
// ============================================================

app.UseAuthorization();

// ============================================================
// CONTROLLERS
// ============================================================

app.MapControllers();

// ============================================================
// RUN
// ============================================================

app.Run();