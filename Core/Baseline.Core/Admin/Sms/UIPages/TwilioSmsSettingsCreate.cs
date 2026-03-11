using CMS.DataEngine;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

using Baseline.Core.Admin.Sms.InfoClasses;
using Baseline.Core.Admin.Sms.Models;
using Baseline.Core.Admin.Sms.UIPages;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

[assembly: UIPage(
    parentType: typeof(TwilioSmsSettingsList),
    slug: "create",
    uiPageType: typeof(TwilioSmsSettingsCreate),
    name: "Add SMS Configuration",
    templateName: TemplateNames.EDIT,
    order: 1)]

namespace Baseline.Core.Admin.Sms.UIPages;

/// <summary>
/// Page for creating new Twilio SMS configurations.
/// </summary>
public class TwilioSmsSettingsCreate(
    IFormItemCollectionProvider formItemCollectionProvider,
    IFormDataBinder formDataBinder,
    IInfoProvider<TwilioSmsSettingsInfo> settingsProvider,
    IPageLinkGenerator pageLinkGenerator)
    : TwilioSmsSettingsBaseEdit(formItemCollectionProvider, formDataBinder, settingsProvider)
{
    /// <inheritdoc />
    protected override TwilioSmsSettingsModel Model { get; } = new();

    /// <inheritdoc />
    protected override async Task<ICommandResponse> ProcessFormData(
        TwilioSmsSettingsModel model,
        ICollection<IFormItem> formItems)
    {
        var result = ValidateAndProcess(model, updateExisting: false);

        if (result.ModificationResultState == ModificationResultState.Success)
        {
            return NavigateTo(pageLinkGenerator.GetPath<TwilioSmsSettingsList>())
                .AddSuccessMessage("SMS configuration created!");
        }

        return await Task.FromResult(
            ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationFailure))
                .AddErrorMessage(result.Message));
    }
}
