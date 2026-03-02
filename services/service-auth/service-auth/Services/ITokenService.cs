namespace service_auth.Services
{
    public interface ITokenService
    {
        Task<string> CreateToken(string googleUserId, string email);
    }
}