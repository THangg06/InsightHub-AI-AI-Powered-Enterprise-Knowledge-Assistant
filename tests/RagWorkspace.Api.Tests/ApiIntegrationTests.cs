using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace RagWorkspace.Api.Tests;

public sealed class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Rejects_requests_without_workspace_token()
    {
        var response = await client.GetAsync("/api/workspaces/demo/documents");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Uploads_document_without_returning_content_and_answers_with_citation()
    {
        using var form = new MultipartFormDataContent();
        using var content = new ByteArrayContent(
            "Deployment is automated with GitHub Actions."u8.ToArray());
        content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        form.Add(content, "file", "guide.txt");

        using var uploadRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/workspaces/demo/documents")
        {
            Content = form
        };
        uploadRequest.Headers.Add("X-Workspace-Token", "demo");

        var uploadResponse = await client.SendAsync(uploadRequest);
        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);
        var uploadBody = await uploadResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("GitHub Actions", uploadBody);

        using var askRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/workspaces/demo/ask")
        {
            Content = JsonContent.Create(new { question = "How is deployment automated?" })
        };
        askRequest.Headers.Add("X-Workspace-Token", "demo");

        var askResponse = await client.SendAsync(askRequest);
        Assert.Equal(HttpStatusCode.OK, askResponse.StatusCode);
        var answerBody = await askResponse.Content.ReadAsStringAsync();
        Assert.Contains("guide.txt", answerBody);
    }

    [Fact]
    public async Task Rejects_unsupported_document_extensions()
    {
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent("not supported"u8.ToArray()), "file", "malware.exe");
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/workspaces/extension-test/documents")
        {
            Content = form
        };
        request.Headers.Add("X-Workspace-Token", "extension-test");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
