using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;

// REQUIRED for Session (fixes the “Unable to resolve IDistributedCache” error)
builder.Services.AddDistributedMemoryCache();

// HttpClient used to call the API
builder.Services.AddHttpClient("api");

// Session cookie that can be sent cross-site from the React origin
builder.Services.AddSession(o =>
{
    o.Cookie.HttpOnly = true;
    o.Cookie.SecurePolicy = CookieSecurePolicy.Always; // HTTPS only
    o.Cookie.SameSite = SameSiteMode.None;             // IMPORTANT for React (cross-site)
    o.IdleTimeout = TimeSpan.FromHours(8);
});

// CORS for React dev server
// Allow React dev server (Vite default at 5173) to call the BFF with credentials.
builder.Services.AddCors(o =>
{
    o.AddDefaultPolicy(p => p
        .WithOrigins("http://localhost:5173", "https://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

var app = builder.Build();

// ---- middleware order ----
app.UseHttpsRedirection();
app.UseCors();
app.UseSession();

string ApiBase() => cfg["JwtApiBase"]?.TrimEnd('/') ?? "https://localhost:54451";

// session helpers
bool HasToken(HttpContext ctx) => ctx.Session.TryGetValue("AuthToken", out _);
string? GetToken(HttpContext ctx) => ctx.Session.TryGetValue("AuthToken", out var b) ? Encoding.UTF8.GetString(b) : null;
void SetToken(HttpContext ctx, string token) => ctx.Session.Set("AuthToken", Encoding.UTF8.GetBytes(token));
void ClearToken(HttpContext ctx) => ctx.Session.Remove("AuthToken");

// generic proxy that forwards to the API with the server-held JWT
async Task<IResult> Proxy(HttpContext ctx, string forwardPath)
{
    if (!HasToken(ctx)) return Results.Unauthorized();

    var client = app.Services.GetRequiredService<IHttpClientFactory>().CreateClient("api");
    var target = $"{ApiBase()}{forwardPath}";
    var req = new HttpRequestMessage(new HttpMethod(ctx.Request.Method), target);

    // copy request body (enable buffering so the stream can be read safely)
    ctx.Request.EnableBuffering();
    if (ctx.Request.ContentLength is > 0)
    {
        using var sr = new StreamReader(ctx.Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await sr.ReadToEndAsync();
        ctx.Request.Body.Position = 0; // reset for any later middleware (defensive)
        req.Content = new StringContent(body, Encoding.UTF8, ctx.Request.ContentType ?? "application/json");
    }

    // attach bearer token (server-side)
    var token = GetToken(ctx);
    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // send to API
    using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ctx.RequestAborted);

    // copy status/headers/content back to caller
    ctx.Response.StatusCode = (int)resp.StatusCode;
    foreach (var h in resp.Headers) ctx.Response.Headers[h.Key] = h.Value.ToArray();
    foreach (var h in resp.Content.Headers) ctx.Response.Headers[h.Key] = h.Value.ToArray();
    ctx.Response.Headers.Remove("transfer-encoding"); // Kestrel manages it

    await resp.Content.CopyToAsync(ctx.Response.Body);
    return Results.Empty;
}

// ---- BFF endpoints ----

// -----------------------------
// BFF endpoints (now part of API)
// -----------------------------

// Login -> ask API for JWT, store it in server session
app.MapPost("/bff/login", async (HttpContext ctx) =>
{
    using var sr = new StreamReader(ctx.Request.Body, Encoding.UTF8);
    var body = await sr.ReadToEndAsync();
    if (string.IsNullOrWhiteSpace(body)) return Results.BadRequest();

    using var doc = JsonDocument.Parse(body);
    var root = doc.RootElement;
    var username = root.TryGetProperty("username", out var u) ? u.GetString() : null;
    var password = root.TryGetProperty("password", out var p) ? p.GetString() : null;
    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        return Results.BadRequest(new { error = "username/password required" });

    var client = app.Services.GetRequiredService<IHttpClientFactory>().CreateClient("api");
    var apiReq = new HttpRequestMessage(HttpMethod.Post, $"{ApiBase()}/api/auth/login")
    {
        Content = new StringContent(JsonSerializer.Serialize(new { username, password }), Encoding.UTF8, "application/json")
    };

    using var resp = await client.SendAsync(apiReq, ctx.RequestAborted);
    if (!resp.IsSuccessStatusCode) return Results.StatusCode((int)resp.StatusCode);

    var text = await resp.Content.ReadAsStringAsync(ctx.RequestAborted);
    using var tokenDoc = JsonDocument.Parse(text);
    if (!tokenDoc.RootElement.TryGetProperty("token", out var tokenProp)) return Results.Unauthorized();

    var token = tokenProp.GetString();
    if (string.IsNullOrEmpty(token)) return Results.Unauthorized();

    SetToken(ctx, token);
    return Results.Ok(new { ok = true });
});

// Logout -> clear session token
app.MapPost("/bff/logout", (HttpContext ctx) =>
{
    ClearToken(ctx);
    return Results.Ok(new { ok = true });
});

// Students (proxy)
app.MapMethods("/bff/students", new[] { "GET", "POST" }, (HttpContext ctx) => Proxy(ctx, "/api/students"));
app.MapMethods("/bff/students/{id:int}", new[] { "GET", "PUT", "DELETE" }, (HttpContext ctx, int id) => Proxy(ctx, $"/api/students/{id}"));

// Teachers (proxy)
app.MapMethods("/bff/teachers", new[] { "GET", "POST" }, (HttpContext ctx) => Proxy(ctx, "/api/teachers"));
app.MapMethods("/bff/teachers/{id:int}", new[] { "GET", "PUT", "DELETE" }, (HttpContext ctx, int id) => Proxy(ctx, $"/api/teachers/{id}"));

// health
app.MapGet("/", () => Results.Text("BFF server is running."));

app.Run();
