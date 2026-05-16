using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using InvestDashboard.Application.Interfaces;

namespace InvestDashboard.Infrastructure.Services
{
    public class SupabaseStorageService : ISupabaseStorageService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SupabaseStorageService> _logger;

        public SupabaseStorageService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<SupabaseStorageService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> UploadFileAsync(string bucketName, string fileName, byte[] content, string contentType)
        {
            var useSupabase = _configuration.GetValue<bool>("Storage:UseSupabaseStorage");

            if (!useSupabase)
            {
                _logger.LogInformation("Supabase Storage is disabled. Falling back to Base64 encoding.");
                return $"data:{contentType};base64,{Convert.ToBase64String(content)}";
            }

            try
            {
                var supabaseUrl = _configuration["Storage:SupabaseUrl"]?.TrimEnd('/');
                var apiKey = _configuration["Storage:SupabaseApiKey"];

                if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogWarning("Supabase credentials are not fully configured. Falling back to Base64.");
                    return $"data:{contentType};base64,{Convert.ToBase64String(content)}";
                }

                var uploadUrl = $"{supabaseUrl}/storage/v1/object/{bucketName}/{fileName}";
                
                using var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
                request.Headers.Add("apikey", apiKey);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                
                var byteContent = new ByteArrayContent(content);
                byteContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                request.Content = byteContent;

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorDetails = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to upload file to Supabase Storage. Status: {StatusCode}, Error: {Error}. Falling back to Base64.", response.StatusCode, errorDetails);
                    return $"data:{contentType};base64,{Convert.ToBase64String(content)}";
                }

                // If successful, return the public URL of the asset
                return $"{supabaseUrl}/storage/v1/object/public/{bucketName}/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error uploading file to Supabase. Falling back to Base64.");
                return $"data:{contentType};base64,{Convert.ToBase64String(content)}";
            }
        }

        public async Task DeleteFileAsync(string bucketName, string fileName)
        {
            var useSupabase = _configuration.GetValue<bool>("Storage:UseSupabaseStorage");
            if (!useSupabase) return;

            try
            {
                var supabaseUrl = _configuration["Storage:SupabaseUrl"]?.TrimEnd('/');
                var apiKey = _configuration["Storage:SupabaseApiKey"];

                if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(apiKey)) return;

                var deleteUrl = $"{supabaseUrl}/storage/v1/object/{bucketName}/{fileName}";

                using var request = new HttpRequestMessage(HttpMethod.Delete, deleteUrl);
                request.Headers.Add("apikey", apiKey);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var errorDetails = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Failed to delete file from Supabase Storage. Status: {StatusCode}, Error: {Error}", response.StatusCode, errorDetails);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting file from Supabase Storage.");
            }
        }
    }
}
