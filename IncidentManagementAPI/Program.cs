using IncidentManagementAPI.Common;
using IncidentManagementAPI.PlatformData;
using IncidentManagementAPI.TenantData;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

//
// ================= CONFIGURATION OPTIONS =================
//

// JWT
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection("Jwt")
);

// Email
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("Email")
);

//
// ================= DATABASE =================
//
builder.Services.AddDbContext<PlatformDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

//
// ================= CORS (REACT http://localhost:3000) =================
//
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactCors", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

//
// ================= SERVICES =================
//
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<TenantProvisioningService>();
builder.Services.AddSingleton<TenantDbContextFactory>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();

//
// ================= AUTHENTICATION JWT =================
//
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

//
// ================= SWAGGER =================
//
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "IncidentManagementAPI",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Bearer {your JWT token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

//
// ================= BUILD =================
//
var app = builder.Build();

//
// ================= MIDDLEWARE PIPELINE =================
//
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

// ?? IMPORTANT : CORS AVANT AUTH
app.UseCors("ReactCors");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
