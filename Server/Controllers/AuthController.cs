using Microsoft.AspNetCore.Mvc;
using RevitPluginUpdater.Server.DTOs;
using RevitPluginUpdater.Server.Services;

namespace RevitPluginUpdater.Server.Controllers
{
    /// <summary>
    /// Контроллер для авторизации администраторов
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly JwtService _jwtService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(JwtService jwtService, IConfiguration configuration, ILogger<AuthController> logger)
        {
            _jwtService = jwtService;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Авторизация администратора
        /// POST /api/auth/login
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                _logger.LogInformation("Попытка авторизации пользователя: {Username}", request.Username);

                // Проверяем учетные данные (в реальном проекте - из базы данных)
                var adminCredentials = _configuration.GetSection("AdminCredentials");
                var validUsername = adminCredentials["Username"];
                var validPassword = adminCredentials["Password"];

                if (request.Username != validUsername || request.Password != validPassword)
                {
                    _logger.LogWarning("Неудачная попытка авторизации для пользователя: {Username}", request.Username);
                    return Unauthorized(new { message = "Неверные учетные данные" });
                }

                // Генерируем JWT токен
                var token = _jwtService.GenerateToken(request.Username);
                var expirationMinutes = int.Parse(_configuration["JwtSettings:ExpirationMinutes"] ?? "1440");

                var response = new LoginResponse
                {
                    Token = token,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes),
                    Username = request.Username
                };

                _logger.LogInformation("Успешная авторизация пользователя: {Username}", request.Username);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при авторизации пользователя: {Username}", request.Username);
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Проверка валидности токена
        /// GET /api/auth/validate
        /// </summary>
        [HttpGet("validate")]
        public ActionResult ValidateToken()
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader == null || !authHeader.StartsWith("Bearer "))
                {
                    return Unauthorized(new { message = "Токен не предоставлен" });
                }

                var token = authHeader.Substring("Bearer ".Length).Trim();
                var isValid = _jwtService.ValidateToken(token);

                if (!isValid)
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                return Ok(new { message = "Токен действителен" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при проверке токена");
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }
    }
}