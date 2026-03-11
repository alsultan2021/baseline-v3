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
/// Page for editing existing notification channel settings.
/// </summary>
public class NotificationChannelSettingsEdit(
    IFormItemCollectionProvider formItemCollectionProvider,
    IFormDataBinder formDataBinder,
    IInfoProvider<NotificationChannelSettingsInfo> settingsProvider,
    IPageLinkGenerator pageLinkGenerator)
    : NotificationChannelSettingsBaseEdit(formItemCollectionProvider, formDataBinder, settingsProvider)
{
    /// <summary>
    /// The settings ID from the URL.
    /// </summary>
    [PageParameter(typeof(IntPageModelBinder))]
    public int NotificationChannelSettingsIdentifier { get; set; }

    private NotificationChannelSettingsModel? _model;

    /// <inheritdoc />
    protected override NotificationChannelSettingsModel Model
    {
        get
        {
            if (_model != null) return _model;

            var settings = SettingsProvider.Get()
                .WhereEquals(nameof(NotificationChannelSettingsInfo.NotificationChannelSettingsID), NotificationChannelSettingsIdentifier)
                .FirstOrDefault()
                ?? throw new InvalidOperationException("Settings not found");

            _model = new NotificationChannelSettingsModel(settings);
            return _model;
        }
    }

    /// <inheritdoc />
    public override Task ConfigurePage()
    {
        PageConfiguration.Headline = $"Edit Channel: {Model.NotificationEmailCodeName}";
        return base.ConfigurePage();
    }

    /// <inheritdoc />
    protected override async Task<ICommandResponse> ProcessFormData(
        NotificationChannelSettingsModel model,
        ICollection<IFormItem> formItems)
    {
        model.Id = NotificationChannelSettingsIdentifier;
        var result = ValidateAndProcess(model, updateExisting: true);

        if (result.ModificationResultState == ModificationResultState.Success)
        {
            return NavigateTo(pageLinkGenerator.GetPath<NotificationChannelSettingsList>())
                .AddSuccessMessage("Notification channel settings updated!");
        }

        return await Task.FromResult(
            ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationFailure))
                .AddErrorMessage(result.Message));
    }
}
