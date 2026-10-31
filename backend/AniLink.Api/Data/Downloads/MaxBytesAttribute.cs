using System.ComponentModel.DataAnnotations;
using System.Text;

namespace AniLink.Api.Data.Downloads;

[AttributeUsage(AttributeTargets.Property)]
public sealed class MaxBytesAttribute(int maxBytes) : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string text || Encoding.UTF8.GetByteCount(text) <= maxBytes) return ValidationResult.Success;

        return new ValidationResult($"{validationContext.DisplayName} is too long: at most {maxBytes} bytes in UTF-8, because it becomes a file name and file systems allow 255 bytes.");
    }
}