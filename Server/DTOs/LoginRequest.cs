using System.ComponentModel.DataAnnotations;

namespace RevitPluginUpdater.Server.DTOs
{
    /// <summary>
    /// DTO для запроса авторизации
    /// </summary>
    public class LoginRequest
    {
        [Required(ErrorMessage = "Имя пользователя обязательно")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Пароль обязателен")]
        public string Password { get; set; } = string.Empty;
    }
}