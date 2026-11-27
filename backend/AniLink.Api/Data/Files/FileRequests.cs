using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace AniLink.Api.Data.Files;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed record DeleteFilesRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(FileLimits.MaxBatch)]
    public string[] Paths { get; init; } = [];
}