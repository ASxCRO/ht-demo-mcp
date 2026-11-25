using System.Security.Cryptography;
using System.Text;
using ModelContextProtocol.Server;

var builder = WebApplication.CreateBuilder(args);
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
var apiKey = builder.Configuration["MCP_API_KEY"];
if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Length < 32)
    throw new InvalidOperationException("Set MCP_API_KEY to a random secret of at least 32 characters.");
builder.Services.AddMcpServer().WithHttpTransport(o => o.Stateless = true).WithTools<SupportTools>();
SupportTools.Initialize(); // Fail fast if demo seed data is missing or invalid.
var app = builder.Build();
var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes("Bearer " + apiKey));
app.Use(async (context, next) => {
    if (context.Request.Path.StartsWithSegments("/mcp")) {
        var supplied = context.Request.Headers.Authorization.ToString();
        if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(supplied)), expectedHash)) {
            context.Response.StatusCode = 401;
            return;
        }
    }
    await next();
});
// Allow the demo tab to be embedded by Microsoft Teams only (and same-origin).
app.Use(async (context, next) => {
    if (context.Request.Path.StartsWithSegments("/teams"))
        context.Response.Headers["Content-Security-Policy"] = "frame-ancestors 'self' https://teams.microsoft.com https://*.teams.microsoft.com https://*.cloud.microsoft;";
    await next();
});
app.UseStaticFiles();
app.MapGet("/health", () => Results.Ok(new { status = "ok", demo = true }));
app.MapMcp("/mcp");
app.Run();
