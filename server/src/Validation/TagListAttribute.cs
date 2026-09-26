using System.ComponentModel.DataAnnotations;

namespace server.src.Validation;

[AttributeUsage(AttributeTargets.Property)]
public class TagListAttribute : ValidationAttribute
{
    public const int MaxTags = 10;
    public const int MaxTagLength = 30;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not IEnumerable<string?> tags)
        {
            return ValidationResult.Success;
        }

        var normalized = Normalize(tags);

        if (normalized.Count > MaxTags)
        {
            return new ValidationResult($"A note can have at most {MaxTags} tags.");
        }

        if (normalized.Any(tag => tag.Length > MaxTagLength))
        {
            return new ValidationResult($"Each tag must be at most {MaxTagLength} characters.");
        }

        return ValidationResult.Success;
    }

    public static List<string> Normalize(IEnumerable<string?>? tags)
    {
        if (tags == null)
        {
            return [];
        }

        return tags
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => string.Join(' ', tag!.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
