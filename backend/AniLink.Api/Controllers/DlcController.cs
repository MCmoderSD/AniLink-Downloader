using AniLink.Api.Data.Downloads;
using AniLink.Api.Helpers;
using AniLink.Api.Services;
using DlcSharp;
using Microsoft.AspNetCore.Mvc;

namespace AniLink.Api.Controllers;

[ApiController]
[Route("api/dlc")]
public sealed class DlcController(DlcService dlc, ILogger<DlcController> logger) : ControllerBase
{
    private const int FormOverhead = 64 * 1024;

    [HttpPost("decode")]
    [RequestSizeLimit(DownloadLimits.MaxDlcBytes + FormOverhead)]
    public async Task<IActionResult> Decode(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0) return BadRequest("No file was sent.");

        if (file.Length > DownloadLimits.MaxDlcBytes)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge, $"The file is larger than {ByteSize.Format(DownloadLimits.MaxDlcBytes)}.");
        }

        try
        {
            await using var stream = file.OpenReadStream();

            return Content(await dlc.DecodeAsync(stream, cancellationToken), "application/json");
        }
        catch (DlcFormatException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (DlcServiceException exception)
        {
            logger.LogWarning("The DLC key service failed: {Error}, status {StatusCode}.", exception.Error, exception.StatusCode);

            return StatusCode(exception.Error == DlcServiceError.Timeout ? StatusCodes.Status504GatewayTimeout : StatusCodes.Status502BadGateway, exception.Message);
        }
    }
}