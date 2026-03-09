namespace service_auth.Services
{
    public interface ITokenService
    {
        string CreateToken(string googleUserId, string email);
    }
}