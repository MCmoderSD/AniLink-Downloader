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
        return Ok(DownloadService.Analyze(request));
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

    [HttpPost("{id:guid}/pause")]
    public IActionResult Pause(Guid id)
    {
        return downloads.Pause(id) switch
        {
            null => NotFound(),
            false => Conflict("Nothing in this download is running that could be paused."),
            true => Ok(downloads.Get(id))
        };
    }

    [HttpPost("{id:guid}/resume")]
    public IActionResult Resume(Guid id)
    {
        return downloads.Resume(id) switch
        {
            null => NotFound(),
            false => Conflict("This download is not paused."),
            true => Ok(downloads.Get(id))
        };
    }

    [HttpPost("{id:guid}/retry")]
    public IActionResult Retry(Guid id)
    {
        return downloads.Retry(id) switch
        {
            null => NotFound(),
            false => Conflict("Nothing failed or was canceled in this download."),
            true => Ok(downloads.Get(id))
        };
    }

    [HttpPost("{id:guid}/items/{number:int}/cancel")]
    public IActionResult CancelItem(Guid id, int number)
    {
        return downloads.CancelItem(id, number) switch
        {
            null => NotFound(),
            false => Conflict("This item has already ended."),
            true => Ok(downloads.Get(id))
        };
    }

    [HttpPost("{id:guid}/items/{number:int}/pause")]
    public IActionResult PauseItem(Guid id, int number)
    {
        return downloads.PauseItem(id, number) switch
        {
            null => NotFound(),
            false => Conflict("This item is not running, so it cannot be paused."),
            true => Ok(downloads.Get(id))
        };
    }

    [HttpPost("{id:guid}/items/{number:int}/resume")]
    public IActionResult ResumeItem(Guid id, int number)
    {
        return downloads.ResumeItem(id, number) switch
        {
            null => NotFound(),
            false => Conflict("This item is not paused."),
            true => Ok(downloads.Get(id))
        };
    }

    [HttpPost("{id:guid}/items/{number:int}/retry")]
    public IActionResult RetryItem(Guid id, int number)
    {
        return downloads.RetryItem(id, number) switch
        {
            null => NotFound(),
            false => Conflict("This item neither failed nor was canceled."),
            true => Ok(downloads.Get(id))
        };
    }

    [HttpDelete("{id:guid}/items/{number:int}")]
    public IActionResult RemoveItem(Guid id, int number)
    {
        if (!downloads.RemoveItem(id, number)) return NotFound();

        // The job is gone as well when this was its last item
        return downloads.Get(id) is { } job ? Ok(job) : NoContent();
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