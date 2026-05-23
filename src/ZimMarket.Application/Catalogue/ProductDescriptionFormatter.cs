namespace ZimMarket.Application.Catalogue;

internal static class ProductDescriptionFormatter
{
    public static string Truncate(string description, int maxLength = 72)
    {
        string trimmed = description.Trim();
        if (trimmed.Length <= maxLength)
            return trimmed;

        return $"{trimmed[..(maxLength - 1)].TrimEnd()}…";
    }
}
