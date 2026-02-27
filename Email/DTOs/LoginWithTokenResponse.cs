namespace Email.DTOs
{
    // Dedicated login response that carries the token. Use only for login/refresh.
    public class LoginWithTokenResponse : LoginResponse
    {
        public string? Token { get; set; }
    }
}