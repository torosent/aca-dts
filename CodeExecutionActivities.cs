
using System.Net.Http.Headers;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;

public class CodeExecutionActivities
{
    private readonly HttpClient httpClient;

    public CodeExecutionActivities(HttpClient client)
    {
        httpClient = client;
    }

    public async Task<string> RunCodeInSession(string code)
    {

        string sessionId = Guid.NewGuid().ToString();

        string poolUrl = Environment.GetEnvironmentVariable("SESSION_POOL_URL")
            ?? throw new InvalidOperationException("Missing required environment variable 'SESSION_POOL_URL'");
        // Create a new session for code execution

        string url = $"{poolUrl}/code/execute?api-version=2024-02-02-preview&identifier={sessionId}";

        var requestBody = new
        {
            properties = new
            {
                codeInputType = "inline",
                executionType = "synchronous",
                code
            }
        };

        var requestContent = new StringContent(JsonSerializer.Serialize(requestBody));
        requestContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        // Add bearer token for authentication
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync());

        var response = await httpClient.PostAsync(url, requestContent);
        response.EnsureSuccessStatusCode();

        var resultJson = await response.Content.ReadAsStringAsync();

        // Optionally parse and return output from resultJson
        return resultJson;
    }

    private async Task<string> GetAccessTokenAsync()
    {
        // Retrieve Azure AD token for Azure Container Apps session pool access
        var credential = new DefaultAzureCredential();
        var tokenRequestContext = new TokenRequestContext(new[] { "https://dynamicsessions.io/.default" });
        var accessToken = await credential.GetTokenAsync(tokenRequestContext);
        return accessToken.Token;
    }
}