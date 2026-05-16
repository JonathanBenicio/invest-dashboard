using System.Threading.Tasks;

namespace InvestDashboard.Application.Interfaces
{
    public interface ISupabaseStorageService
    {
        /// <summary>
        /// Uploads a file. If the Supabase Storage feature flag is disabled,
        /// returns the Base64 representation of the file.
        /// </summary>
        Task<string> UploadFileAsync(string bucketName, string fileName, byte[] content, string contentType);
        Task DeleteFileAsync(string bucketName, string fileName);
    }
}
