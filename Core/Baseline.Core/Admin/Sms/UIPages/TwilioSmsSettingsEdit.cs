using CMS.DataEngine;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

using Baseline.Core.Admin.Sms.InfoClasses;
using Baseline.Core.Admin.Sms.Models;
using Baseline.Core.Admin.Sms.UIPages;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

[assembly: UIPage(
    parentType: typeof(TwilioSmsSettingsList),
    slug: PageParameterConstants.PARAMETERIZED_SLUG,
    uiPageType: typeof(TwilioSmsSettingsEdit),
    name: "Edit SMS Configuration",
    templateName: TemplateNames.EDIT,
    order: 2)]

namespace Baseline.Core.Admin.Sms.UIPages;

/// <summary>
/// Page for editing existing Twilio SMS configurations.
/// </summary>
public class TwilioSmsSettingsEdit(
    IFormItemCollectionProvider formItemCollectionProvider,
    IFormDataBinder formDataBinder,
    IInfoProvider<TwilioSmsSettingsInfo> settingsProvider,
    IPageLinkGenerator pageLinkGenerator)
    : TwilioSmsSettingsBaseEdit(formItemCollectionProvider, formDataBinder, settingsProvider)
{
    /// <summary>
    /// The settings ID from the URL.
    /// </summary>
    [PageParameter(typeof(IntPageModelBinder))]
    public int TwilioSmsSettingsIdentifier { get; set; }

    private TwilioSmsSettingsModel? _model;

    /// <inheritdoc />
    protected override TwilioSmsSettingsModel Model
    {
        get
        {
            if (_model != null) return _model;

            var settings = SettingsProvider.Get()
                .WhereEquals(nameof(TwilioSmsSettingsInfo.TwilioSmsSettingsID), TwilioSmsSettingsIdentifier)
                .FirstOrDefault()
                ?? throw new InvalidOperationException("Settings not found");

            _model = new TwilioSmsSettingsModel(settings);
            return _model;
        }
    }

    /// <inheritdoc />
    public override Task ConfigurePage()
    {
        PageConfiguration.Headline = "Edit Twilio SMS Configuration";
        return base.ConfigurePage();
    }

    /// <inheritdoc />
    protected override async Task<ICommandResponse> ProcessFormData(
        TwilioSmsSettingsModel model,
        ICollection<IFormItem> formItems)
    {
        model.Id = TwilioSmsSettingsIdentifier;
        var result = ValidateAndProcess(model, updateExisting: true);

        if (result.ModificationResultState == ModificationResultState.Success)
        {
            return NavigateTo(pageLinkGenerator.GetPath<TwilioSmsSettingsList>())
                .AddSuccessMessage("SMS configuration updated!");
        }

        return await Task.FromResult(
            ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationFailure))
                .AddErrorMessage(result.Message));
    }
}
