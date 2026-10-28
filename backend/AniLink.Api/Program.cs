using System.Text.RegularExpressions;
using AniLink.Api.Archives;
using AniLink.Api.DebridLink;
using AniLink.Api.Helpers;
using AniLink.Api.Services;

var builder = WebApplication.CreateBuilder(args);

#if DEBUG
builder.Configuration.AddUserSecrets<Program>(optional: true);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
#endif

builder.Configuration.UseShortSettingNames();

var debridLink = DebridLinkOptions.From(builder.Configuration);
var downloads = DownloadOptions.From(builder.Configuration, builder.Environment);
var archives = ArchiveOptions.From(builder.Configuration);

builder.Services.AddAniLinkDebridLink(debridLink);
builder.Services.AddAniLinkDownloads(downloads, archives);
builder.Services.AddControllers();

var app = builder.Build();

if (downloads.RequestedConcurrency > downloads.MaxConcurrency)
{
    app.Logger.LogWarning(
        "Concurrent_Downloads is {Requested}, but the server has {Available} threads; {Used} downloads run at the same time.",
        downloads.RequestedConcurrency, DownloadOptions.MaxAllowedConcurrency, downloads.MaxConcurrency);
}

app.UseAniLinkRequestLogging();

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        var hashed = Regex.IsMatch(context.File.Name, @"-[A-Za-z0-9]{8}\.(js|css)$");
        context.Context.Response.Headers.CacheControl = hashed ? "public, max-age=31536000, immutable" : "no-cache";
    }
});

app.MapAniLinkHealthChecks();
app.MapControllers();

app.MapFallback("/api/{**path}", () => Results.NotFound());
app.MapFallbackToFile("index.html");

app.Run();