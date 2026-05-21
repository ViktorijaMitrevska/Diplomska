using Diplomska.Models;
using Google.Cloud.Firestore;
using System.Net.Http;
using System.Text;
using System.Text.Json;

public class FirebaseAuthService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;

    public FirebaseAuthService(IConfiguration config)
    {
        _http = new HttpClient();
        _apiKey = config["Firebase:ApiKey"];
    }

    public async Task<string> Register(string email, string password)
    {
        return await AuthRequest(
            $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={_apiKey}",
            email, password);
    }

    public async Task<string> Login(string email, string password)
    {
        return await AuthRequest(
            $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={_apiKey}",
            email, password);
    }

    private async Task<string> AuthRequest(string url, string email, string password)
    {
        var payload = new
        {
            email,
            password,
            returnSecureToken = true
        };

        var response = await _http.PostAsync(
            url,
            new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json")
        );

        if (!response.IsSuccessStatusCode)
            throw new Exception("Firebase authentication failed");

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        return doc.RootElement.GetProperty("idToken").GetString();
    }

    public async Task SendPasswordResetEmail(string email)
    {
        var payload = new
        {
            requestType = "PASSWORD_RESET",
            email = email
        };

        var response = await _http.PostAsync(
            $"https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key={_apiKey}",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        );

        if (!response.IsSuccessStatusCode)
            throw new Exception("Failed to send reset email");
    }

    public async Task ChangePassword(string idToken, string newPassword)
    {
        var payload = new
        {
            idToken = idToken,
            password = newPassword,
            returnSecureToken = true
        };

        var response = await _http.PostAsync(
            $"https://identitytoolkit.googleapis.com/v1/accounts:update?key={_apiKey}",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        );

        if (!response.IsSuccessStatusCode)
            throw new Exception("Password change failed");
    }

}

