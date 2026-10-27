using AniLink.Api.Data.Downloads;
using AniLink.Api.Exceptions.Downloads;
using AniLink.Api.Helpers;
using AniLink.Api.Services;
using AniLink.Api.Services.Streaming;
using Microsoft.AspNetCore.Mvc;

namespace AniLink.Api.Controllers;

[ApiController]
[Route("api/downloads")]
public sealed class DownloadsController(DownloadService downloads, ProgressHub hub) : ControllerBase
{
    [HttpPost("analyze")]
    public IActionResult Analyze([FromBody] AnalyzeRequest request)
    {
        return Ok(downloads.Analyze(request));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDownloadRequest request)
    {
        try
        {
            var job = await downloads.CreateAsync(request);

            return CreatedAtAction(nameof(Get), new { id = job.Id }, job);
        }
        catch (InvalidDownloadException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (DownloadConflictException exception)
        {
            return Conflict(exception.Message);
        }
    }

    [HttpGet]
    public IActionResult List()
    {
        return Ok(downloads.List());
    }

    [HttpGet("{id:guid}")]
    public IActionResult Get(Guid id)
    {
        return downloads.Get(id) is { } job ? Ok(job) : NotFound();
    }

    [HttpPost("{id:guid}/password")]
    public IActionResult SubmitPassword(Guid id, [FromBody] PasswordRequest request)
    {
        return downloads.SubmitPassword(id, request.Password) switch
        {
            null => NotFound(),
            false => Conflict("No item of this download is waiting for a password."),
            true => Ok(downloads.Get(id))
        };
    }

    [HttpPost("{id:guid}/cancel")]
    public IActionResult Cancel(Guid id)
    {
        return downloads.Cancel(id) ? Ok(downloads.Get(id)) : NotFound();
    }

    [HttpPost("{id:guid}/retry")]
    public IActionResult Retry(Guid id)
    {
        return downloads.Retry(id) switch
        {
            null => NotFound(),
            false => Conflict("Nothing failed or was cancelled in this download."),
            true => Ok(downloads.Get(id))
        };
    }

    [HttpDelete("{id:guid}")]
    public IActionResult Remove(Guid id)
    {
        return downloads.Remove(id) ? NoContent() : NotFound();
    }

    [HttpGet("stream")]
    public async Task Stream(CancellationToken cancellationToken)
    {
        using var subscription = hub.Subscribe();
        await Response.StreamAsync(subscription, cancellationToken);
    }
}
