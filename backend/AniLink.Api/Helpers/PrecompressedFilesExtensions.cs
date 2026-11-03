using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Net.Http.Headers;

namespace AniLink.Api.Helpers;

// The frontend build writes a .br and a .gz file next to each of its text files, compressed once and as small as they get.
// This serves them in place of the file when the browser accepts the encoding. Compressing on the fly can only use the
// lowest level, because it happens on every request, and leaves a quarter of the size on the table.
public static class PrecompressedFilesExtensions
{
    private const string TypeKey = "AniLink.PrecompressedFileType";

    private static readonly (string Encoding, string Extension)[] Variants = [("br", ".br"), ("gzip", ".gz")];

    // Knows the two extensions, since the static file middleware does not serve a file whose type it does not know
    public static IContentTypeProvider ContentTypes { get; } = CreateContentTypes();

    // Run when a static file is about to be sent: the file on disk is x.js.br, but what the browser gets is x.js
    public static void RestoreContentType(HttpContext context)
    {
        if (context.Items.TryGetValue(TypeKey, out var type) && type is string value) context.Response.ContentType = value;
    }

    public static WebApplication UseAniLinkPrecompressedFiles(this WebApplication app)
    {
        var files = app.Environment.WebRootFileProvider;
        var types = new FileExtensionContentTypeProvider();

        app.Use(async (context, next) =>
        {
            var request = context.Request;
            var path = request.Path.Value ?? string.Empty;

            // A range of the compressed file is not a range of the file, so a request for one gets the file itself
            if ((!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method)) || request.Headers.ContainsKey(HeaderNames.Range) || !types.TryGetContentType(path, out var type))
            {
                await next(context);
                return;
            }

            var available = Variants.Where(variant => files.GetFileInfo(path + variant.Extension).Exists).ToArray();

            if (available.Length == 0)
            {
                await next(context);
                return;
            }

            // Whether the answer is compressed depends on the header, caches in between have to know that
            context.Response.Headers.Append(HeaderNames.Vary, HeaderNames.AcceptEncoding);

            var accepted = request.GetTypedHeaders().AcceptEncoding;
            var chosen = available.FirstOrDefault(variant => accepted.Any(encoding => Matches(encoding, variant.Encoding)));

            if (chosen.Encoding is not null)
            {
                request.Path = path + chosen.Extension;
                context.Response.Headers.ContentEncoding = chosen.Encoding;
                context.Items[TypeKey] = type;
            }

            await next(context);
        });

        return app;
    }

    private static bool Matches(StringWithQualityHeaderValue encoding, string name) =>
        (encoding.Value.Equals(name, StringComparison.OrdinalIgnoreCase) || encoding.Value.Equals("*", StringComparison.Ordinal)) && encoding.Quality is null or > 0;

    private static FileExtensionContentTypeProvider CreateContentTypes()
    {
        var provider = new FileExtensionContentTypeProvider();

        foreach (var (_, extension) in Variants) provider.Mappings[extension] = "application/octet-stream";

        return provider;
    }
}