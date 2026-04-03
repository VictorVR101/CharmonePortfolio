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
    // Upload result
    // ─────────────────────────────────────────────
    public class UploadResult
    {
        public bool Success { get; set; }
        public string? PublicUrl { get; set; }
        public string? FileName { get; set; }
        public string? Error { get; set; }
    }

    // ─────────────────────────────────────────────
    // Main Supabase service
    // ─────────────────────────────────────────────
    public class SupabaseService
    {
        private readonly HttpClient _http;

        private const string Url = "https://kvpnalsxfbiktmovvjkj.supabase.co";
        private const string AnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Imt2cG5hbHN4ZmJpa3Rtb3Z2amtqIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzUxNDg0NzAsImV4cCI6MjA5MDcyNDQ3MH0.gWcKx4iVDU1Wkjy8LUQbUM1C6-OhLsFWBSyBQp9MWDQ";
        private const string Bucket = "portfolio";

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

        // ─── Upload a file to Supabase Storage ───
        public async Task<UploadResult> UploadFileAsync(string folder, string fileName, byte[] fileBytes, string contentType)
        {
            // Sanitise folder name — strip any leading/trailing slashes
            folder = folder.Trim('/');
            var storagePath = $"{folder}/{fileName}";

            var request = new HttpRequestMessage(HttpMethod.Post,
                $"{Url}/storage/v1/object/{Bucket}/{storagePath}");
            request.Headers.Add("apikey", AnonKey);
            request.Headers.Add("Authorization", $"Bearer {_accessToken}");

            request.Content = new ByteArrayContent(fileBytes);
            request.Content.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

            var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                return new UploadResult { Success = false, Error = err };
            }

            var publicUrl = $"{Url}/storage/v1/object/public/{Bucket}/{storagePath}";
            return new UploadResult
            {
                Success = true,
                PublicUrl = publicUrl,
                FileName = fileName
            };
        }

        // ─── Delete a file from Supabase Storage ─
        public async Task<bool> DeleteFileAsync(string folder, string fileName)
        {
            folder = folder.Trim('/');
            var storagePath = $"{folder}/{fileName}";

            var payload = new { prefixes = new[] { storagePath } };
            var request = new HttpRequestMessage(HttpMethod.Delete,
                $"{Url}/storage/v1/object/{Bucket}");
            request.Headers.Add("apikey", AnonKey);
            request.Headers.Add("Authorization", $"Bearer {_accessToken}");
            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        // ─── Get public URL for a stored file ────
        public string GetPublicUrl(string folder, string fileName)
        {
            folder = folder.Trim('/');
            return $"{Url}/storage/v1/object/public/{Bucket}/{folder}/{fileName}";
        }
    }
}