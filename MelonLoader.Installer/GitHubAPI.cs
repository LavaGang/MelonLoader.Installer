using System.Text.Json;
using System.Text.Json.Nodes;

namespace MelonLoader.Installer;

internal class GitHubResponse
{
    internal JsonNode? Node;
    internal string? Error;
}

public static class GitHubApi
{
    private const string RepoOwner = "LavaGang";
    private const string RepoName = "MelonLoader";
    private static readonly string ApiUrl = $"https://api.github.com/repos/{RepoOwner}/{RepoName}";
    
    internal static async Task<GitHubResponse> GetReleases()
    {
        var url =
            $"{ApiUrl}/releases";

        return await ContactUrl(url);
    }
    
    internal static async Task<GitHubResponse> GetWorkflowRuns()
    {
        var url =
            $"{ApiUrl}/actions/workflows/5411546/runs" +
            $"?branch=alpha-development" +
            $"&event=push" +
            $"&status=success" +
            $"&page=1" +
            $"&per_page=5";
        
        var resp = await ContactUrl(url);
        if (resp.Node != null)
            resp.Node = resp.Node["workflow_runs"];
        return resp;
    }
    
    internal static async Task<GitHubResponse> GetWorkflowRunArtifacts(string runId)
    {
        var url =
            $"{ApiUrl}/actions/runs/{runId}/artifacts";
        
        var resp = await ContactUrl(url);
        if (resp.Node != null)
            resp.Node = resp.Node["artifacts"];
        return resp;
    }
    
    private static async Task<GitHubResponse> ContactUrl(string url)
    {
        HttpResponseMessage resp;
        try
        {
            resp = await InstallerUtils.Http.GetAsync(url);
        }
        catch (Exception ex)
        {
            return new() { Error = ex.ToString() };
        }
        if (!resp.IsSuccessStatusCode)
            return new() { Error = $"Status Code: {(int)resp.StatusCode}\n{Enum.GetName(resp.StatusCode)}\n{resp.ReasonPhrase}" };

        var relStr = await resp.Content.ReadAsStringAsync();
        return new() { Node = JsonNode.Parse(relStr) };
    }
}