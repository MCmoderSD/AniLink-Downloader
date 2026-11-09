using DlcSharp;

namespace AniLink.Api.Services;

public sealed class DlcService
{
    private readonly DlcDecoder decoder = new();

    public async Task<string> DecodeAsync(Stream content, CancellationToken cancellationToken)
    {
        var container = await decoder.DecodeAsync(content, cancellationToken);

        return container.ToJson(indented: false);
    }
}