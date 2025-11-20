using System.Text;
using System.Text.Json;

namespace Binah.Ontology.Services;

/// <summary>
/// HTTP client for calling binah-webhooks GitHub integration APIs
/// </summary>
public class GitHubIntegrationClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GitHubIntegrationClient> _logger;

    public GitHubIntegrationClient(HttpClient httpClient, ILogger<GitHubIntegrationClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create autonomous pull request via binah-webhooks
    /// </summary>
    public async Task<CreateAutonomousPRResponse> CreatePRAsync(CreateAutonomousPRRequest request, string jwtToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {jwtToken}");
            _httpClient.DefaultRequestHeaders.Add("X-Tenant-Id", request.TenantId);

            _logger.LogInformation("Calling binah-webhooks to create PR for {WorkflowType}", request.WorkflowType);

            var response = await _httpClient.PostAsync("/api/github/pr/create", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("PR creation failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return new CreateAutonomousPRResponse
                {
                    Success = false,
                    ErrorMessage = $"HTTP {response.StatusCode}: {responseContent}"
                };
            }

            var result = JsonSerializer.Deserialize<CreateAutonomousPRResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new CreateAutonomousPRResponse
            {
                Success = false,
                ErrorMessage = "Failed to deserialize response"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling binah-webhooks PR creation API");
            return new CreateAutonomousPRResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}

// DTOs matching binah-webhooks contracts
public class CreateAutonomousPRRequest
{
    public string TenantId { get; set; } = string.Empty;
    public string RepositoryOwner { get; set; } = string.Empty;
    public string RepositoryName { get; set; } = string.Empty;
    public string BaseBranch { get; set; } = "main";
    public string BranchPrefix { get; set; } = "claude/autonomous";
    public string Title { get; set; } = string.Empty;
    public string WorkflowType { get; set; } = "OntologyRefactoring";
    public List<GitHubFileChange> Files { get; set; } = new();
    public Dictionary<string, string> TemplateData { get; set; } = new();
    public bool AutoMerge { get; set; } = false;
    public List<string> Reviewers { get; set; } = new();
    public List<string> Labels { get; set; } = new();
    public string CommitMessage { get; set; } = string.Empty;
    public bool Draft { get; set; } = false;
}

public class GitHubFileChange
{
    public string Path { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Mode { get; set; } = "Add";
    public string? Sha { get; set; }
}

public class CreateAutonomousPRResponse
{
    public string PrId { get; set; } = string.Empty;
    public int PrNumber { get; set; }
    public string PrUrl { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? CommitSha { get; set; }
    public int FilesChanged { get; set; }
    public bool ReviewersRequested { get; set; }
    public bool LabelsAdded { get; set; }
}
