using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.UIPages;

using Baseline.Core.Admin.Sms.UIPages;

[assembly: UIApplication(
    identifier: TwilioSmsApplication.IDENTIFIER,
    type: typeof(TwilioSmsApplication),
    slug: "twilio-sms",
    name: "SMS Notifications",
    category: BaseApplicationCategories.CONFIGURATION,
    icon: Icons.Paragraph,
    templateName: TemplateNames.SECTION_LAYOUT)]

namespace Baseline.Core.Admin.Sms.UIPages;

/// <summary>
/// Admin application for Twilio SMS notification settings.
/// </summary>
public class TwilioSmsApplication : ApplicationPage
{
    /// <summary>
    /// Unique identifier for the Twilio SMS application.
    /// </summary>
    public const string IDENTIFIER = "Baseline.TwilioSmsNotifications";
}
