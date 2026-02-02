namespace RevitPluginUpdater.Server.DTOs
{
    /// <summary>
    /// DTO для ответа на авторизацию
    /// </summary>
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string Username { get; set; } = string.Empty;
    }
}