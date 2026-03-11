namespace Baseline.Automation.Enums;

/// <summary>
/// How an email action resolves its email content.
/// Maps to CMS.Automation.Internal.EmailActionBasedOn.
/// </summary>
public enum EmailActionBasedOn
{
    /// <summary>HTML formatted text stored in step properties.</summary>
    FormattedText = 1,

    /// <summary>Provider-specific code-based implementation.</summary>
    Code = 2,

    /// <summary>Reference to email from the Emails application/library.</summary>
    EmailLibrary = 3
}
