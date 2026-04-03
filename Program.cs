using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using YourProject.API.Data;
using YourProject.API.Helpers;
using YourProject.API.Services;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Controllers + Swagger
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        // Important: Angular UI sends/reads enums as strings (e.g. "ADMIN", "EnAttente", "Comptabilite")
        // This converter ensures enums are serialized/deserialized as strings instead of numeric values.
        opts.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
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
            new string[] {}
        }
    });
});

// Db
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// CORS (Angular)
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("spa", p => p
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());
});
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
// JWT
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

// DI
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

app.UseCors("spa");
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

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
        db.Database.EnsureCreated();
    }

    // Ensure new tables exist even when running without migrations on an existing database.
    // (Pragmatic approach for this template project.)
    try
    {
        db.Database.ExecuteSqlRaw(@"
IF OBJECT_ID(N'[dbo].[TrackingRows]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TrackingRows] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [CreatedAt] DATETIME2 NOT NULL,
        [UpdatedAt] DATETIME2 NOT NULL,
        [Year] INT NOT NULL,
        [Board] NVARCHAR(128) NOT NULL CONSTRAINT [DF_TrackingRows_Board] DEFAULT N'Default',
        [DossierId] INT NOT NULL,
        [AssignedToUserId] INT NOT NULL,
        [Module] INT NOT NULL,
        [DataJson] NVARCHAR(MAX) NOT NULL,
        CONSTRAINT [FK_TrackingRows_Dossiers] FOREIGN KEY ([DossierId]) REFERENCES [dbo].[Dossiers]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_TrackingRows_Users] FOREIGN KEY ([AssignedToUserId]) REFERENCES [dbo].[Users]([Id])
    );
END

IF COL_LENGTH('dbo.TrackingRows', 'Board') IS NULL
BEGIN
    ALTER TABLE [dbo].[TrackingRows] ADD [Board] NVARCHAR(128) NOT NULL CONSTRAINT [DF_TrackingRows_Board_Auto] DEFAULT N'Default';
END

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TrackingRows_Unique' AND object_id = OBJECT_ID(N'[dbo].[TrackingRows]'))
BEGIN
    DROP INDEX [IX_TrackingRows_Unique] ON [dbo].[TrackingRows];
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TrackingRows_Unique_WithBoard' AND object_id = OBJECT_ID(N'[dbo].[TrackingRows]'))
BEGIN
    CREATE UNIQUE INDEX [IX_TrackingRows_Unique_WithBoard] ON [dbo].[TrackingRows]([Year],[DossierId],[AssignedToUserId],[Module],[Board]);
END
");
    }
    catch
    {
        // ignore (e.g., permissions, different provider)
    }

    new DbSeeder(db).Seed();
}

app.Run();
