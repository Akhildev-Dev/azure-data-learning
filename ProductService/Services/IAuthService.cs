namespace ProductService.Services;

public interface IAuthService
{
    string GenerateToken(string username, List<string> roles);
    bool ValidateToken(string token);
}