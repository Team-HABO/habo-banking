using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.HttpOverrides;
using service_auth.Services;
using System.Net;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
string frontendUrl = builder.Configuration["FrontendUrl"]
    ?? throw new InvalidOperationException("Frontend URL not set in environment variables");
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "AllowFrontend",
                              policy =>
                              {
                                  policy
            .WithOrigins(frontendUrl)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
                              });

});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie()
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"]
        ?? throw new InvalidOperationException("Google ClientId missing");
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]
        ?? throw new InvalidOperationException("Google ClientSecret missing");
});

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
string networkIp = builder.Configuration["NetworkIp"]
    ?? throw new InvalidOperationException("Network Ip URL not set in environment variables");
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto;

    if (networkIp.Contains('/'))
    {
        string[] parts = networkIp.Split('/', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2
            || !IPAddress.TryParse(parts[0], out IPAddress? networkAddress)
            || !int.TryParse(parts[1], out int prefixLength))
        {
            throw new InvalidOperationException("NetworkIp must be a valid CIDR (e.g. 172.22.0.0/16) or IP address (e.g. 172.22.0.10).");
        }
        try
        {
            options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(networkIp));
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("NetworkIp must be a valid CIDR (e.g. 172.22.0.0/16) or IP address (e.g. 172.22.0.10).", ex);
        }
    }
    else
    {
        if (!IPAddress.TryParse(networkIp, out IPAddress? proxyIp))
            throw new InvalidOperationException("NetworkIp must be a valid CIDR (e.g. 172.22.0.0/16) or IP address (e.g. 172.22.0.10).");

        options.KnownProxies.Add(proxyIp);
    }
});

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseForwardedHeaders();

// Health check endpoint for Dockerfile
app.MapGet("/health", () => Results.Ok());

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
