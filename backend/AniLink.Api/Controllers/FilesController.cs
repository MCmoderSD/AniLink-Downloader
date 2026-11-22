using AniLink.Api.Data.Files;
using AniLink.Api.Helpers;
using AniLink.Api.Services.Files;
using AniLink.Api.Services.Streaming;
using Microsoft.AspNetCore.Mvc;

namespace AniLink.Api.Controllers;

[ApiController]
[Route("api/files")]
public sealed class FilesController(FileBrowser files, FileHub hub) : ControllerBase
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

    [HttpGet("tree")]
    public IActionResult Tree([FromQuery] string? path)
    {
        return files.Tree(path) switch
        {
            TreeFound found => Ok(found.Tree),
            ListingInvalid invalid => BadRequest(invalid.Reason),
            ListingMissing => NotFound(),
            ListingUnavailable unavailable => StatusCode(StatusCodes.Status503ServiceUnavailable, unavailable.Reason)
        };
    }

    [HttpPost("delete")]
    public IActionResult Delete([FromBody] DeleteFilesRequest request)
    {
        if (Request.Headers["Sec-Fetch-Site"].ToString() is "cross-site" or "same-site")
        {
            return StatusCode(StatusCodes.Status403Forbidden, "Files can only be deleted from the page of this server.");
        }

        return Ok(files.Delete(request.Paths));
    }

    [HttpGet("stream")]
    public async Task Stream(CancellationToken cancellationToken)
    {
        using var subscription = hub.Subscribe();
        await Response.StreamAsync(subscription, cancellationToken);
    }
}