using System;
using System.IO;
using System.Threading.Tasks;
using InvestDashboard.Application.DTOs.Trading;
using InvestDashboard.Application.DTOs.Common;
using InvestDashboard.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InvestDashboard.WebAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/v1/transactions")]
    public class TransacoesController : ControllerBase
    {
        private readonly ITransacaoAppService _transacaoAppService;
        private readonly ISupabaseStorageService _storageService;

        public TransacoesController(
            ITransacaoAppService transacaoAppService,
            ISupabaseStorageService storageService)
        {
            _transacaoAppService = transacaoAppService;
            _storageService = storageService;
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<TransacaoDto>>> Register(
            [FromBody] RegistrarTransacaoDto dto,
            CancellationToken cancellationToken)
        {
            var transaction = await _transacaoAppService.RegisterTransactionAsync(dto);
            return CreatedAtAction(
                nameof(GetByPortfolio),
                new { portfolioId = transaction.CarteiraId },
                new ApiResponse<TransacaoDto>(transaction));
        }

        [HttpGet("portfolio/{portfolioId:guid}")]
        public async Task<ActionResult<ApiResponse<List<TransacaoDto>>>> GetByPortfolio(Guid portfolioId)
        {
            var transactions = await _transacaoAppService.GetTransactionsByPortfolioIdAsync(portfolioId);
            return Ok(new ApiResponse<List<TransacaoDto>>(transactions));
        }

        [HttpPatch("{id:guid}")]
        public async Task<ActionResult<ApiResponse<TransacaoDto>>> Update(
            Guid id,
            [FromBody] AtualizarTransacaoDto dto)
        {
            var transaction = await _transacaoAppService.UpdateTransactionAsync(id, dto);
            return Ok(new ApiResponse<TransacaoDto>(transaction));
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
        {
            await _transacaoAppService.DeleteTransactionAsync(id);
            return Ok(new ApiResponse<bool>(true));
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
