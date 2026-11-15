using System.Text.Json;
using System.Text.Json.Nodes;

namespace MelonLoader.Installer;

internal class GitHubResponse
{
    internal JsonNode? Node;
    internal string? Error;
}

public class GitHubApi
{
    private string RepoOwner;
    private string RepoName;
    private string ApiUrl;

    public GitHubApi(string repoOwner, string repoName)
    {
        RepoOwner = repoOwner;
        RepoName = repoName;
        ApiUrl = $"https://api.github.com/repos/{RepoOwner}/{RepoName}";
    }
    
    internal async Task<GitHubResponse> GetReleases()
    {
        var url = $"{ApiUrl}/releases";
        return await ContactUrl(url);
    }
    
    internal async Task<GitHubResponse> GetLatestRelease()
    {
        var url = $"{ApiUrl}/releases/latest";
        return await ContactUrl(url);
    }
    
    internal async Task<GitHubResponse> GetWorkflowRuns(string? runId,
        string? branch = null)
    {
        var url = $"{ApiUrl}/actions/workflows/{runId}/runs?";
        
        if (!string.IsNullOrEmpty(branch))
            url += $"branch={branch}&";
        
        url += $"event=push" +
               $"&status=success" +
               $"&page=1" +
               $"&per_page=5";
        
        var resp = await ContactUrl(url);
        if (resp.Node != null)
            resp.Node = resp.Node["workflow_runs"];
        return resp;
    }
    
    internal async Task<GitHubResponse> GetWorkflowRunArtifacts(string runId)
    {
        var url = $"{ApiUrl}/actions/runs/{runId}/artifacts";
        var resp = await ContactUrl(url);
        if (resp.Node != null)
            resp.Node = resp.Node["artifacts"];
        return resp;
    }
    
    private async Task<GitHubResponse> ContactUrl(string url)
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