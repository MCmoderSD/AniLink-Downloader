using AniLink.Api.Data.Files;
using AniLink.Api.Services.Files;
using Microsoft.AspNetCore.Mvc;

namespace AniLink.Api.Controllers;

[ApiController]
[Route("api/files")]
public sealed class FilesController(FileBrowser files) : ControllerBase
{
    [HttpGet]
    public IActionResult List([FromQuery] string? path)
    {
        return files.List(path) switch
        {
            ListingFound found => Ok(found.Listing),
            ListingInvalid invalid => BadRequest(invalid.Reason),
            ListingMissing => NotFound(),
            ListingUnavailable unavailable => StatusCode(StatusCodes.Status503ServiceUnavailable, unavailable.Reason)
        };
    }

    [HttpPost("delete")]
    [Consumes("application/json")]
    public IActionResult Delete([FromBody] DeleteFilesRequest request)
    {
        if (Request.Headers["Sec-Fetch-Site"].ToString() is "cross-site" or "same-site")
        {
            return StatusCode(StatusCodes.Status403Forbidden, "Files can only be deleted from the page of this server.");
        }

        return Ok(files.Delete(request.Paths));
    }
}