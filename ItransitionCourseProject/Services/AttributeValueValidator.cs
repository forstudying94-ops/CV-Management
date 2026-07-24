namespace ItransitionCourseProject.Services;

public static class AttributeValueValidator {
    private const int MaximumLength = 100;

    public static string? Normalize(string? value) {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalizedValue = value.Trim();
        if (normalizedValue.Length > MaximumLength)
        {
            throw new InvalidOperationException($"Attribute value cannot be longer than {MaximumLength} characters");
        }

        return normalizedValue;
    }

    public static bool IsFilled(string? value) {
        return !string.IsNullOrWhiteSpace(value);
    }
}
