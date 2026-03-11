namespace Localization.Models;

/// <summary>
/// BaselineCultureInfo model
/// Renamed from CultureInfo to avoid clash with System.Globalization.CultureInfo
/// </summary>
public class BaselineCultureInfo
{
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? NativeName { get; set; }
    public string ShortCode { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}

/// <summary>
/// ResourceString
/// </summary>
public class ResourceString
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? CultureCode { get; set; }
}
