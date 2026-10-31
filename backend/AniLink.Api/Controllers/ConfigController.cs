using AniLink.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AniLink.Api.Controllers;

[ApiController]
[Route("api/config")]
public sealed class ConfigController(DownloadService downloads) : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(downloads.GetConfig());
    }
}