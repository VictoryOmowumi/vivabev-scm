namespace VivaBev.SharedKernel.Domain;

public static class Guard
{
    public static string Required(string? value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException($"{field} is required.");

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new DomainException($"{field} must be {maxLength} characters or fewer.");

        return trimmed;
    }

    public static string Code(string? value, string field, int maxLength) =>
        Required(value, field, maxLength).ToUpperInvariant();
}