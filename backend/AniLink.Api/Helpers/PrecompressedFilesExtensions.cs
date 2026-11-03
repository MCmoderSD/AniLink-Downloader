using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Net.Http.Headers;

namespace AniLink.Api.Helpers;

public static class PrecompressedFilesExtensions
{
    private const string TypeKey = "AniLink.PrecompressedFileType";

    private static readonly (string Encoding, string Extension)[] Variants = [("br", ".br"), ("gzip", ".gz")];

    public static IContentTypeProvider ContentTypes { get; } = CreateContentTypes();

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

            context.Response.Headers.Append(HeaderNames.Vary, HeaderNames.AcceptEncoding);

            var accepted = request.GetTypedHeaders().AcceptEncoding;
            var chosen = available
                .Select(variant => (Variant: variant, Quality: QualityOf(accepted, variant.Encoding)))
                .Where(candidate => candidate.Quality > 0)
                .OrderByDescending(candidate => candidate.Quality)
                .Select(candidate => candidate.Variant)
                .FirstOrDefault();

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

    private static double QualityOf(IList<StringWithQualityHeaderValue> accepted, string name)
    {
        var entry = accepted.FirstOrDefault(encoding => encoding.Value.Equals(name, StringComparison.OrdinalIgnoreCase))
            ?? accepted.FirstOrDefault(encoding => encoding.Value.Equals("*", StringComparison.Ordinal));

        return entry is null ? 0 : entry.Quality ?? 1;
    }

    private static FileExtensionContentTypeProvider CreateContentTypes()
    {
        var provider = new FileExtensionContentTypeProvider();

        foreach (var (_, extension) in Variants) provider.Mappings[extension] = "application/octet-stream";

        return provider;
    }
}