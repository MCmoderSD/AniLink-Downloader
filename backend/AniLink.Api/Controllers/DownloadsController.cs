using AniLink.Api.Data.Downloads;
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
        return await downloads.CreateAsync(request) switch
        {
            DownloadCreated created => CreatedAtAction(nameof(Get), new { id = created.Job.Id }, created.Job),
            DownloadInvalid invalid => BadRequest(invalid.Reason),
            DownloadConflict conflict => Conflict(conflict.Reason)
        };
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
        return Respond(downloads.SubmitPassword(id, request.Password));
    }

    [HttpPost("{id:guid}/cancel")]
    public IActionResult Cancel(Guid id)
    {
        return Respond(downloads.Cancel(id));
    }

    [HttpPost("{id:guid}/pause")]
    public IActionResult Pause(Guid id)
    {
        return Respond(downloads.Pause(id));
    }

    [HttpPost("{id:guid}/resume")]
    public IActionResult Resume(Guid id)
    {
        return Respond(downloads.Resume(id));
    }

    [HttpPost("{id:guid}/retry")]
    public IActionResult Retry(Guid id)
    {
        return Respond(downloads.Retry(id));
    }

    [HttpPut("{id:guid}/auto-unpack")]
    public IActionResult SetUnpack(Guid id, [FromBody] UnpackSettingRequest request)
    {
        return Respond(downloads.SetUnpack(id, request.Unpack!.Value));
    }

    [HttpPost("{id:guid}/unpack")]
    public IActionResult Unpack(Guid id)
    {
        return Respond(downloads.Unpack(id));
    }

    [HttpPost("{id:guid}/cancel-unpack")]
    public IActionResult CancelUnpack(Guid id)
    {
        return Respond(downloads.CancelUnpack(id));
    }

    [HttpPost("{id:guid}/items/{number:int}/cancel")]
    public IActionResult CancelItem(Guid id, int number)
    {
        return Respond(downloads.CancelItem(id, number));
    }

    [HttpPost("{id:guid}/items/{number:int}/pause")]
    public IActionResult PauseItem(Guid id, int number)
    {
        return Respond(downloads.PauseItem(id, number));
    }

    [HttpPost("{id:guid}/items/{number:int}/resume")]
    public IActionResult ResumeItem(Guid id, int number)
    {
        return Respond(downloads.ResumeItem(id, number));
    }

    [HttpPost("{id:guid}/items/{number:int}/retry")]
    public IActionResult RetryItem(Guid id, int number)
    {
        return Respond(downloads.RetryItem(id, number));
    }

    [HttpPost("{id:guid}/items/{number:int}/cancel-unpack")]
    public IActionResult CancelUnpackItem(Guid id, int number)
    {
        return Respond(downloads.CancelUnpackItem(id, number));
    }

    [HttpPut("{id:guid}/items/{number:int}/auto-unpack")]
    public IActionResult SetItemUnpack(Guid id, int number, [FromBody] UnpackSettingRequest request)
    {
        return Respond(downloads.SetItemUnpack(id, number, request.Unpack!.Value));
    }

    [HttpPost("{id:guid}/items/{number:int}/unpack")]
    public IActionResult UnpackItem(Guid id, int number)
    {
        return Respond(downloads.UnpackItem(id, number));
    }

    [HttpDelete("{id:guid}/items/{number:int}")]
    public IActionResult RemoveItem(Guid id, int number)
    {
        if (!downloads.RemoveItem(id, number)) return NotFound();
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

    private IActionResult Respond(ActionOutcome outcome)
    {
        return outcome switch
        {
            ActionDone done => Ok(done.Job),
            ActionRejected rejected => Conflict(rejected.Reason),
            ActionMissing => NotFound()
        };
    }
}