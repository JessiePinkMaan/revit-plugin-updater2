using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RevitPluginUpdater.Server.Data;
using RevitPluginUpdater.Server.Services;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Настройка Serilog для логирования
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/server-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Используем SQLite для простоты развертывания
var connectionString = "Data Source=/tmp/revit_updater.db";

// Добавление сервисов в контейнер
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// Настройка JWT аутентификации
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Регистрация сервисов
builder.Services.AddScoped<JwtService>();
builder.Services.AddSingleton<InMemoryFileService>(); // Singleton для хранения в памяти

// Настройка CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Добавление контроллеров
builder.Services.AddControllers();

// Настройка Swagger для документации API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Revit Plugin Updater API", Version = "v1" });
    
    // Настройка JWT в Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
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

var app = builder.Build();

// Настройка конвейера HTTP запросов
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Применение миграций базы данных при запуске
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        // Удаляем и пересоздаем базу данных (для разработки)
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        Log.Information("База данных SQLite пересоздана успешно");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Ошибка при пересоздании базы данных SQLite");
    }
}

// Создание директории для плагинов
var pluginsPath = builder.Configuration["FileStorage:PluginsPath"] ?? "/var/data/plugins";
if (!Directory.Exists(pluginsPath))
{
    Directory.CreateDirectory(pluginsPath);
    Log.Information("Создана директория для плагинов: {Path}", pluginsPath);
}

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

// Статические файлы для админки
app.UseStaticFiles();

// Маршрутизация для SPA (Single Page Application)
app.MapFallbackToFile("index.html");

app.MapControllers();

// Настройка порта для Render.com
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://0.0.0.0:{port}");

Log.Information("Сервер запускается на порту {Port}", port);

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Критическая ошибка при запуске сервера");
}
finally
{
    Log.CloseAndFlush();
}