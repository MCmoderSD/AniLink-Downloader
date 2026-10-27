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

var debridLink = DebridLinkOptions.From(builder.Configuration);
var downloads = DownloadOptions.From(builder.Configuration, builder.Environment);
var archives = ArchiveOptions.From(builder.Configuration);

builder.Services.AddAniLinkDebridLink(debridLink);
builder.Services.AddAniLinkDownloads(downloads, archives);
builder.Services.AddControllers();

var app = builder.Build();

app.UseAniLinkRequestLogging();

// The Angular build lives in wwwroot and is served from the same origin as the API, which is why there is no CORS.
app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        // Hashed bundles never change under the same name; everything else (index.html above all) must be revalidated.
        var hashed = Regex.IsMatch(context.File.Name, @"-[A-Za-z0-9]{8}\.(js|css)$");
        context.Context.Response.Headers.CacheControl = hashed ? "public, max-age=31536000, immutable" : "no-cache";
    }
});

app.MapAniLinkHealthChecks();
app.MapControllers();

// An unknown API route is a 404, not the Angular page; every other unknown route is the page, so deep links work.
app.MapFallback("/api/{**path}", () => Results.NotFound());
app.MapFallbackToFile("index.html");

app.Run();
