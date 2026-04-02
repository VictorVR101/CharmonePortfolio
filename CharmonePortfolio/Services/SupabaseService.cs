using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CharmonePortfolio.Services
{
    // ─────────────────────────────────────────────
    // Project model — mirrors the Supabase table
    // ─────────────────────────────────────────────
    public class Project
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = "";

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("subject")]
        public string Subject { get; set; } = "";

        [JsonPropertyName("category")]
        public string Category { get; set; } = "";

        [JsonPropertyName("term")]
        public string Term { get; set; } = "";

        [JsonPropertyName("folder")]
        public string Folder { get; set; } = "";

        [JsonPropertyName("thumbnail")]
        public string Thumbnail { get; set; } = "";

        [JsonPropertyName("images")]
        public List<string> Images { get; set; } = new();

        [JsonPropertyName("video_urls")]
        public List<string> VideoUrls { get; set; } = new();

        [JsonPropertyName("pdf_url")]
        public string PdfUrl { get; set; } = "";

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new();

        [JsonPropertyName("featured")]
        public bool Featured { get; set; } = false;

        [JsonPropertyName("sort_order")]
        public int SortOrder { get; set; } = 0;

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }
    }

    // ─────────────────────────────────────────────
    // Auth response from Supabase
    // ─────────────────────────────────────────────
    public class AuthResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("error_description")]
        public string? ErrorDescription { get; set; }
    }

    // ─────────────────────────────────────────────
    // Main Supabase service
    // ─────────────────────────────────────────────
    public class SupabaseService
    {
        private readonly HttpClient _http;

        // ← Your Supabase credentials
        private const string Url = "https://kvpnalsxfbiktmovvjkj.supabase.co";
        private const string AnonKey = "sb_publishable_KDSEQtXD_GoMpZlmIjZshQ_JxTbrJq9";

        private string? _accessToken;
        public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken);

        public SupabaseService(HttpClient http)
        {
            _http = http;
        }

        // ─── Auth ────────────────────────────────
        public async Task<bool> LoginAsync(string email, string password)
        {
            var payload = new { email, password };
            var request = new HttpRequestMessage(HttpMethod.Post,
                $"{Url}/auth/v1/token?grant_type=password");
            request.Headers.Add("apikey", AnonKey);
            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return false;

            var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
            _accessToken = auth?.AccessToken;
            return IsAuthenticated;
        }

        public void Logout() => _accessToken = null;

        // ─── Read all projects (public) ──────────
        public async Task<List<Project>> GetProjectsAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{Url}/rest/v1/projects?select=*&order=sort_order.asc,created_at.desc");
            request.Headers.Add("apikey", AnonKey);
            request.Headers.Add("Authorization", $"Bearer {AnonKey}");

            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return new List<Project>();

            return await response.Content.ReadFromJsonAsync<List<Project>>()
                ?? new List<Project>();
        }

        // ─── Create a new project (admin only) ───
        public async Task<bool> CreateProjectAsync(Project project)
        {
            var request = new HttpRequestMessage(HttpMethod.Post,
                $"{Url}/rest/v1/projects");
            request.Headers.Add("apikey", AnonKey);
            request.Headers.Add("Authorization", $"Bearer {_accessToken}");
            request.Headers.Add("Prefer", "return=minimal");
            request.Content = new StringContent(
                JsonSerializer.Serialize(project),
                Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        // ─── Update an existing project (admin only) ──
        public async Task<bool> UpdateProjectAsync(Project project)
        {
            var request = new HttpRequestMessage(HttpMethod.Patch,
                $"{Url}/rest/v1/projects?id=eq.{project.Id}");
            request.Headers.Add("apikey", AnonKey);
            request.Headers.Add("Authorization", $"Bearer {_accessToken}");
            request.Headers.Add("Prefer", "return=minimal");
            request.Content = new StringContent(
                JsonSerializer.Serialize(project),
                Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        // ─── Delete a project (admin only) ───────
        public async Task<bool> DeleteProjectAsync(string id)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete,
                $"{Url}/rest/v1/projects?id=eq.{id}");
            request.Headers.Add("apikey", AnonKey);
            request.Headers.Add("Authorization", $"Bearer {_accessToken}");

            var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
    }
}