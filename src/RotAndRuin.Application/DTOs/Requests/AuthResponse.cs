namespace RotAndRuin.Application.DTOs.Requests;

public class AuthResponse
{
    public string Token { get; set; }
    public string Username { get; set; }
    public bool IsAdmin { get; set; }
}