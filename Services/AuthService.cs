using System.Data.Common;
using System.Text.Json;


public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly string _supabaseUrl;
    private readonly string _supabaseKey;


    public AuthService()
    {
        _httpClient = new HttpClient();
        _supabaseUrl = "https://gzjnucqewasjsdtnxhie.supabase.co";
        _supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Imd6am51Y3Fld2FzanNkdG54aGllIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjIyNjgzODUsImV4cCI6MjA3Nzg0NDM4NX0.eJqUEmoYf5-IeAj4GkY1a2wkLwWQshTgRpa8IVT4nzo";

        _httpClient.DefaultRequestHeaders.Add("apikey", _supabaseKey);
    }

    public async Task<dynamic> Login(string user, string password)
    {
        var db = new DBConnect();

        var (success, status, data) = await db.Select("Users", "username=eq." + user);
        if (data.Count == 0)
        {
            return ApiResponse<dynamic>.Fail(404, "User not found. register dummy");
        }
        else
        {
            var userr = data[0];
            var email = userr.GetProperty("email").ToString();
            var response = await _httpClient.PostAsJsonAsync($"{_supabaseUrl}/auth/v1/token?grant_type=password",
                new { email, password });

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<dynamic>(json);
            Console.WriteLine($"→ {DateTime.Now:HH:mm:ss} Login Result {result}");

            if (!response.IsSuccessStatusCode)
            {
                int code = (int)response.StatusCode;
                string message = "Login failed.";




                return code switch
                {
                    400 => ApiResponse<dynamic>.Fail(code, "Email not confirmed. Please verify your account."),
                    401 => ApiResponse<dynamic>.Fail(code, "Invalid credentials. Wrong password or email."),
                    404 => ApiResponse<dynamic>.Fail(code, "User not found."),
                    422 => ApiResponse<dynamic>.Fail(code, "Weak password or invalid input."),
                    _ => ApiResponse<dynamic>.Fail(code, message)
                };
            }
            else
            {
                string userId = result.GetProperty("user").GetProperty("id").GetString();
                string token = JwtService.GenerateJwt(userId);

                return ApiResponse<dynamic>.Ok(
                    new
                    {
                        token = token,
                        user = new
                        {
                            username = user,
                            email = result.GetProperty("user").GetProperty("email").GetString(),
                            id = result.GetProperty("user").GetProperty("id").GetString()
                        }
                    },
                    "Logged in"
                );

                //return ApiResponse<object>.Ok(
                //   new
                //   {
                //       token = token,
                //       user = result
                //   },
                //  "Logged in"
                //);
                //return ApiResponse<dynamic>.Ok(result, "Logged in.");
            }


        }
    }

    public async Task<dynamic> Register(string user, string email, string password)
    {
        var db = new DBConnect();
        var (success, status, data) = await db.Select("Users", $"or=(email.eq.{email},username.eq.{user})");
        if (success && data.Count > 0)
        {

            return ApiResponse<object>.Fail(409, "Email or username already exists");
        }

        var response = await _httpClient.PostAsJsonAsync($"{_supabaseUrl}/auth/v1/signup",
            new { email, password });
        Console.WriteLine($"{DateTime.Now:HH:mm:ss} Auth Register response: {response}");
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<dynamic>(json);
        //Console.WriteLine($"Parsed result: {result}");


        //Console.WriteLine($"code: {code}");
        if (!response.IsSuccessStatusCode)
        {
            int code = result.GetProperty("code").GetInt32();

            Console.WriteLine($"AUTH FAILED CAUSE {result.GetProperty("error_code").GetString()}");
            var debugResponse = ApiResponse<dynamic>.Fail(code, result.GetProperty("error_code").GetString(), result);
            Console.WriteLine(JsonSerializer.Serialize(debugResponse, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
            return ApiResponse<dynamic>.Fail(code, result.GetProperty("error_code").GetString(), result);

        }
        else
        {
            await db.Insert("Users", new { email = email, username = user, created_at = DateTime.UtcNow });
            Console.WriteLine("User record inserted successfully.");
            return ApiResponse<dynamic>.Ok(result, "User registered successfully");

        }
    }

    public async Task<dynamic> ResendConfirmation(string user, string? password = null)
    {
        try
        {
            var db = new DBConnect();
            var (success, status, data) = await db.Select("Users", "username=eq." + user);
            var userr = data[0];
            var email = userr.GetProperty("email").ToString();
            var response = await _httpClient.PostAsJsonAsync($"{_supabaseUrl}/auth/v1/recover", new { email });
            var json = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"{DateTime.Now:HH:mm:ss} Auth Resend response: {response.StatusCode} {json}");

            if (!response.IsSuccessStatusCode)
            {
                int code = (int)response.StatusCode;
                string message = "Failed to resend confirmation email.";

                try
                {
                    var result = JsonSerializer.Deserialize<JsonElement>(json);
                    if (result.TryGetProperty("message", out var msgProp))
                        message = msgProp.GetString() ?? message;
                    if (result.TryGetProperty("code", out var codeProp))
                        code = codeProp.GetInt32();
                }
                catch { }

                return ApiResponse<dynamic>.Fail(code, message);
            }

            return ApiResponse<dynamic>.Ok(200, "Confirmation email sent successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Resend error: {ex.Message}");
            return ApiResponse<dynamic>.Fail(500, "Unexpected server error while resending confirmation email.");
        }
    }




}




public class DBConnect
{
    private readonly HttpClient _httpClient;
    private readonly string _supabaseUrl;
    private readonly string _supabaseKey;

    public DBConnect()
    {
        _httpClient = new HttpClient();
        _supabaseUrl = "https://gzjnucqewasjsdtnxhie.supabase.co";
        _supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Imd6am51Y3Fld2FzanNkdG54aGllIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjIyNjgzODUsImV4cCI6MjA3Nzg0NDM4NX0.eJqUEmoYf5-IeAj4GkY1a2wkLwWQshTgRpa8IVT4nzo";
        _httpClient.DefaultRequestHeaders.Add("apikey", _supabaseKey);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_supabaseKey}");
    }

    public async Task<(bool Success, int StatusCode, List<JsonElement> Data)> Select(string table, string filter)
    {
        var response = await _httpClient.GetAsync($"{_supabaseUrl}/rest/v1/{table}?{filter}");
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"❌ Supabase SELECT failed: {response.StatusCode} ({json})");
            return (false, (int)response.StatusCode, new List<JsonElement>());
        }

        try
        {
            var array = JsonSerializer.Deserialize<List<JsonElement>>(json) ?? new List<JsonElement>();
            return (true, (int)response.StatusCode, array);
        }
        catch
        {
            Console.WriteLine(" Failed to parse JSON from Supabase SELECT");
            return (false, (int)response.StatusCode, new List<JsonElement>());
        }
    }


    public async Task<dynamic> Insert(string table, object data)
    {
        var jsonData = JsonSerializer.Serialize(data);
        var content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{_supabaseUrl}/rest/v1/{table}", content);

        var body = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"→ {DateTime.Now:HH:mm:ss} Insert Response: {body}");

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"❌ Insert failed: {response.StatusCode}");
            return new { success = false, status = response.StatusCode, message = body };
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            return new { success = true, message = "Inserted successfully (no content returned)" };
        }

        try
        {
            return JsonSerializer.Deserialize<dynamic>(body);
        }
        catch (JsonException)
        {
            Console.WriteLine("⚠️ Response was not valid JSON.");
            return new { success = true, message = "Inserted successfully (invalid JSON response)", raw = body };
        }
    }

    public async Task<dynamic> Update(string table, object data, string condition)
    {
        var jsonData = JsonSerializer.Serialize(data);
        var content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"{_supabaseUrl}/rest/v1/{table}?{condition}")
        {
            Content = content
        };
        var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<dynamic>(json);
    }
}
