using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Extensions;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using BRaVe_Management_Backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;


namespace BRaVe_Management_Backend.Controllers.mission_operation
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class FileBasedDeduplicationController : ControllerBase
    {
        private readonly IFileBasedDeduplicationService _fileBasedDeduplicationService;

        public FileBasedDeduplicationController(IFileBasedDeduplicationService fileBasedDeduplicationService)
        {
            
            _fileBasedDeduplicationService = fileBasedDeduplicationService;
        }

        [HttpGet("uploads")]
        public async Task<ActionResult<IReadOnlyList<UploadedFile>>> GetUploadsAsync(
       [FromQuery] int maxCount = 10,
       CancellationToken cancellationToken = default)
        {
            var tenantId = User.Tenant();
            var uploads = await _fileBasedDeduplicationService.GetUploadJobsAsync(
                tenantId,
                maxCount,
                cancellationToken);

            return Ok(uploads);
        }

        [HttpPost("process")]
        [RequestSizeLimit(52_428_800)]
        public async Task<ActionResult<FileBasedDeduplicationProcessResult>> ProcessAsync(
            IFormFile file,
            CancellationToken cancellationToken)
        {
            if (file is null || file.Length == 0)
            {
                return BadRequest(new { error = "Please select a JSON file to upload." });
            }

            if (!string.Equals(Path.GetExtension(file.FileName), ".json", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = "Only .json files are allowed." });
            }

            string json;
            await using (var stream = file.OpenReadStream())
            using (var reader = new StreamReader(stream))
            {
                json = await reader.ReadToEndAsync(cancellationToken);
            }

            var validationResult = FileBasedDeduplicationUploadValidator.Validate(json);
            if (!validationResult.IsValid)
            {
                return BadRequest(new { error = validationResult.ErrorMessage });
            }

            try
            {

                var uploadedBy = User.Identity?.Name ?? string.Empty;
                int TenantId = User.Tenant();
                var result = await _fileBasedDeduplicationService.ProcessUploadAsync(
                    TenantId,
                    json,
                    file.FileName,
                    uploadedBy,
                    cancellationToken);

                return Ok(result);
            }
            catch (Exception e)
            {
                return BadRequest(new { error = e.Message });
            }
            
        }
    }

}
