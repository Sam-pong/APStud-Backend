using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<DBConnect>();

var app = builder.Build();
app.UseCors("AllowAll");

app.Use(async (context, next) =>
{
    Console.WriteLine($"→ {DateTime.Now:HH:mm:ss} {context.Request.Method} {context.Request.Path}");

    await next();
    Console.WriteLine($"→ {DateTime.Now:HH:mm:ss} {context.Response}");

    Console.WriteLine($"← {DateTime.Now:HH:mm:ss} {context.Response.StatusCode}");
});

app.MapMethods("/api/auth/login", new[] { "OPTIONS" }, () => Results.Ok());
app.MapMethods("/api/auth/register", new[] { "OPTIONS" }, () => Results.Ok());
app.MapMethods("/api/auth/resend", new[] { "OPTIONS" }, () => Results.Ok());

app.MapPost("/api/auth/login", async (HttpContext context, AuthService authService) =>
{
    var requestJson = await context.Request.ReadFromJsonAsync<JsonElement>();
    var user = requestJson.GetProperty("user").GetString();
    var password = requestJson.GetProperty("password").GetString();
    var resultt = $"User: {user},  Password: {password}";

    var result = await authService.Login(user, password);

    return Results.Json(result, statusCode: result.Code);
});

app.MapPost("/api/auth/register", async (HttpContext context, AuthService authService) =>
{
    var request = await context.Request.ReadFromJsonAsync<dynamic>();
    Console.WriteLine($"→ {DateTime.Now:HH:mm:ss} Registering user: request {request}");
    var user = request.GetProperty("user").GetString();
    var password = request.GetProperty("password").GetString();
    var email = request.GetProperty("email").GetString();

    var result = await authService.Register(user, email, password);
    return Results.Json(result, statusCode: result.Code);
});


app.MapPost("/api/auth/resend", async (HttpContext context, AuthService authService) =>
{
    var request = await context.Request.ReadFromJsonAsync<JsonElement>();
    var user = request.GetProperty("user").GetString();
    var password = request.TryGetProperty("password", out var passProp) ? passProp.GetString() : null;

    Console.WriteLine($"→ {DateTime.Now:HH:mm:ss} Resend confirmation for {user}");

    var result = await authService.ResendConfirmation(user, password);
    return Results.Json(result, statusCode: result.Code);
});

app.MapGet("/api/auth/verify", (HttpContext ctx) =>
{
    if (!ctx.Request.Headers.TryGetValue("Authorization", out var authHeader))
        return Results.Json(ApiResponse<object>.Fail(401, "Missing token"), statusCode: 401);

    var token = authHeader.ToString().Replace("Bearer ", "");

    try
    {
        var claims = JwtService.ValidateJwt(token);
        var userId = claims.FindFirst("uid")?.Value;

        return Results.Json(
            ApiResponse<object>.Ok(new { userId = userId }, "Token valid"),
            statusCode: 200
        );
    }
    catch
    {
        return Results.Json(ApiResponse<object>.Fail(401, "Invalid or expired token"), statusCode: 401);
    }
});

app.MapGet("/api/test", () => "API is working!");

app.Run();
