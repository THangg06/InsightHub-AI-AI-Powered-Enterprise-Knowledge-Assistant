using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace RagWorkspace.Api.Tests;

public sealed class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory) => client = factory.CreateClient();

    [Fact]
    public async Task Rejects_requests_without_jwt()
    {
        var response = await client.GetAsync($"/api/workspaces/{Guid.NewGuid()}/documents");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Registers_creates_workspace_uploads_and_answers_with_citation()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var email = $"{suffix}@example.test";
        var register = await client.PostAsJsonAsync("/api/auth/register", new { email, password = "Password123" });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);
        var auth = await register.Content.ReadFromJsonAsync<AuthPayload>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        var workspaceResponse = await client.PostAsJsonAsync("/api/workspaces", new { name = $"Workspace {suffix}" });
        Assert.Equal(HttpStatusCode.Created, workspaceResponse.StatusCode);
        var workspace = await workspaceResponse.Content.ReadFromJsonAsync<WorkspacePayload>();

        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent("Deployment uses GitHub Actions."u8.ToArray()), "file", "guide.txt");
        var uploadResponse = await client.PostAsync($"/api/workspaces/{workspace!.Id}/documents", form);
        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);

        var askResponse = await client.PostAsJsonAsync($"/api/workspaces/{workspace.Id}/ask", new { question = "How is deployment automated?" });
        Assert.Equal(HttpStatusCode.OK, askResponse.StatusCode);
        var answer = await askResponse.Content.ReadAsStringAsync();
        Assert.Contains("guide.txt", answer);
    }

    [Fact]
    public async Task Rejects_unsupported_document_extensions()
    {
        var suffix = Guid.NewGuid().ToString("N");
        await client.PostAsJsonAsync("/api/auth/register", new { email = $"{suffix}@example.test", password = "Password123" });
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = $"{suffix}@example.test", password = "Password123" });
        var auth = await login.Content.ReadFromJsonAsync<AuthPayload>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        var workspaceResponse = await client.PostAsJsonAsync("/api/workspaces", new { name = $"Workspace {suffix}" });
        var workspace = await workspaceResponse.Content.ReadFromJsonAsync<WorkspacePayload>();

        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent("not supported"u8.ToArray()), "file", "malware.exe");
        var response = await client.PostAsync($"/api/workspaces/{workspace!.Id}/documents", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Lists_and_deletes_a_chat_session_for_the_authenticated_user()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var email = $"{suffix}@example.test";
        var register = await client.PostAsJsonAsync("/api/auth/register", new { email, password = "Password123" });
        var auth = await register.Content.ReadFromJsonAsync<AuthPayload>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        var workspaceResponse = await client.PostAsJsonAsync("/api/workspaces", new { name = $"Workspace {suffix}" });
        var workspace = await workspaceResponse.Content.ReadFromJsonAsync<WorkspacePayload>();

        var chatsResponse = await client.GetAsync($"/api/workspaces/{workspace!.Id}/chats");
        Assert.Equal(HttpStatusCode.OK, chatsResponse.StatusCode);
        var chats = await chatsResponse.Content.ReadFromJsonAsync<ChatPayload[]>();
        Assert.NotNull(chats);
        Assert.Empty(chats!);
    }

    private sealed record AuthPayload(string Token, Guid UserId, string Email);
    private sealed record WorkspacePayload(Guid Id, string Name);
    private sealed record ChatPayload(Guid Id, string Title, DateTimeOffset CreatedAt);
}
