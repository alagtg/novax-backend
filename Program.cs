using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;
using System.Text;
using YourProject.API.Data;
using YourProject.API.Helpers;
using YourProject.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter()
        );
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opts =>
{
    opts.SwaggerDoc("v1", new() { Title = "NOVAX API", Version = "v1" });

    opts.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Entrer: Bearer {token}"
    });

    opts.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (builder.Environment.IsDevelopment())
{
    var provider = builder.Configuration["DatabaseProvider"] ?? "SqlServer";

    if (provider == "Postgres")
    {
        var pgConn = builder.Configuration.GetConnectionString("PostgresConnection");
        builder.Services.AddDbContext<AppDbContext>(opt =>
            opt.UseNpgsql(pgConn));
    }
    else
    {
        var sqlConn = builder.Configuration.GetConnectionString("SqlServerConnection");
        builder.Services.AddDbContext<AppDbContext>(opt =>
            opt.UseSqlServer(sqlConn));
    }
}
else
{
    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseNpgsql(connectionString));
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("spa", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:4200",
                "https://brilliant-mermaid-4531b4.netlify.app"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException("Jwt:Key manquant dans appsettings.json");

var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<JwtTokenHelper>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<DossierService>();
builder.Services.AddScoped<FiscalYearService>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<BillingService>();
builder.Services.AddScoped<LeaveService>();
builder.Services.AddScoped<TrackingService>();
builder.Services.AddScoped<FollowUpService>();

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddScoped<EmailService>();

var app = builder.Build();

QuestPDF.Settings.License = LicenseType.Community;

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();

app.UseCors("spa");

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/healthz", () => Results.Ok("OK"));
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    try
    {
        db.Database.Migrate();
    }
    catch
    {
    }

    try
    {
        var seeder = new DbSeeder(db);
        seeder.Seed();
    }
    catch
    {
    }
}

app.Run();