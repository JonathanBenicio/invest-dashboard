using System;
using System.IO;
using System.Threading.Tasks;
using InvestDashboard.Application.DTOs.Trading;
using InvestDashboard.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InvestDashboard.WebAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionAppService _transactionAppService;
        private readonly ISupabaseStorageService _storageService;

        public TransactionController(
            ITransactionAppService transactionAppService,
            ISupabaseStorageService storageService)
        {
            _transactionAppService = transactionAppService;
            _storageService = storageService;
        }

        [HttpPost]
        public async Task<IActionResult> Register([FromBody] RegisterTransactionDto dto)
        {
            var transaction = await _transactionAppService.RegisterTransactionAsync(dto);
            return CreatedAtAction(nameof(GetByPortfolio), new { portfolioId = transaction.PortfolioId }, transaction);
        }

        [HttpGet("portfolio/{portfolioId:guid}")]
        public async Task<IActionResult> GetByPortfolio(Guid portfolioId)
        {
            var transactions = await _transactionAppService.GetTransactionsByPortfolioIdAsync(portfolioId);
            return Ok(transactions);
        }

        /// <summary>
        /// Uploads a brokerage note (PDF or image).
        /// If "Storage:UseSupabaseStorage" is true in config, it uploads to Supabase Storage Bucket.
        /// If "Storage:UseSupabaseStorage" is false, it returns the Base64 Data URI of the file.
        /// </summary>
        [HttpPost("upload-note")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadNote([FromForm] IFormFile file, [FromForm] string? bucketName)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file was provided or the file is empty.");
            }

            var allowedExtensions = new[] { ".pdf", ".png", ".jpg", ".jpeg" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (Array.IndexOf(allowedExtensions, extension) < 0)
            {
                return BadRequest("Invalid file type. Only PDF and images (.png, .jpg, .jpeg) are allowed.");
            }

            var bucket = string.IsNullOrWhiteSpace(bucketName) ? "brokerage-notes" : bucketName.Trim();
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var content = memoryStream.ToArray();

            // Perform dynamic upload (handles both paths based on configuration)
            var resultUrl = await _storageService.UploadFileAsync(bucket, uniqueFileName, content, file.ContentType);

            return Ok(new { url = resultUrl });
        }
    }
}
