using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using service_auth;
using service_auth.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddOptions<AppSettings>()
    .Bind(builder.Configuration)
    .ValidateDataAnnotations()
    .ValidateOnStart();

AppSettings appSettings = builder.Configuration.Get<AppSettings>() 
    ?? throw new InvalidOperationException("AppSettings configuration is missing");

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "AllowFrontend",
                              policy =>
                              {
                                  policy
            .WithOrigins(appSettings.FrontendUrl)
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
    options.ClientId = appSettings.Authentication.Google.ClientId;
    options.ClientSecret = appSettings.Authentication.Google.ClientSecret;
});

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();


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
