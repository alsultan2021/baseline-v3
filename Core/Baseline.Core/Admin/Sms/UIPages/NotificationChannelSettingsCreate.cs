using CMS.DataEngine;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

using Baseline.Core.Admin.Sms.InfoClasses;
using Baseline.Core.Admin.Sms.Models;
using Baseline.Core.Admin.Sms.UIPages;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

// Notification channel settings are managed programmatically
// No separate admin UI

namespace Baseline.Core.Admin.Sms.UIPages;

/// <summary>
/// Page for creating new notification channel settings.
/// </summary>
public class NotificationChannelSettingsCreate(
    IFormItemCollectionProvider formItemCollectionProvider,
    IFormDataBinder formDataBinder,
    IInfoProvider<NotificationChannelSettingsInfo> settingsProvider,
    IPageLinkGenerator pageLinkGenerator)
    : NotificationChannelSettingsBaseEdit(formItemCollectionProvider, formDataBinder, settingsProvider)
{
    /// <inheritdoc />
    protected override NotificationChannelSettingsModel Model { get; } = new();

    /// <inheritdoc />
    protected override async Task<ICommandResponse> ProcessFormData(
        NotificationChannelSettingsModel model,
        ICollection<IFormItem> formItems)
    {
        var result = ValidateAndProcess(model, updateExisting: false);

        if (result.ModificationResultState == ModificationResultState.Success)
        {
            return NavigateTo(pageLinkGenerator.GetPath<NotificationChannelSettingsList>())
                .AddSuccessMessage("Notification channel settings created!");
        }

        return await Task.FromResult(
            ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationFailure))
                .AddErrorMessage(result.Message));
    }
}
