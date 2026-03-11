using CMS.DataEngine;
using CMS.Helpers;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

using Baseline.Core.Admin.Sms.InfoClasses;
using Baseline.Core.Admin.Sms.Models;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

namespace Baseline.Core.Admin.Sms.UIPages;

/// <summary>
/// Base class for Twilio SMS settings edit pages.
/// </summary>
public abstract class TwilioSmsSettingsBaseEdit(
    IFormItemCollectionProvider formItemCollectionProvider,
    IFormDataBinder formDataBinder,
    IInfoProvider<TwilioSmsSettingsInfo> settingsProvider)
    : ModelEditPage<TwilioSmsSettingsModel>(formItemCollectionProvider, formDataBinder)
{
    /// <summary>
    /// The Twilio SMS settings info provider.
    /// </summary>
    protected readonly IInfoProvider<TwilioSmsSettingsInfo> SettingsProvider = settingsProvider;

    /// <summary>
    /// Validates model and processes the save operation.
    /// </summary>
    protected ModificationResult ValidateAndProcess(TwilioSmsSettingsModel model, bool updateExisting = false)
    {
        if (string.IsNullOrWhiteSpace(model.DisplayName))
        {
            return new ModificationResult(ModificationResultState.Failure, "Display name is required.");
        }

        if (string.IsNullOrWhiteSpace(model.AccountSid))
        {
            return new ModificationResult(ModificationResultState.Failure, "Account SID is required.");
        }

        if (string.IsNullOrWhiteSpace(model.AuthToken))
        {
            return new ModificationResult(ModificationResultState.Failure, "Auth Token is required.");
        }

        if (string.IsNullOrWhiteSpace(model.FromPhoneNumber))
        {
            return new ModificationResult(ModificationResultState.Failure, "From phone number is required.");
        }

        if (model.UseVerifyService && string.IsNullOrWhiteSpace(model.VerifyServiceSid))
        {
            return new ModificationResult(ModificationResultState.Failure, "Verify Service SID is required when using Verify Service.");
        }

        var info = updateExisting
            ? SettingsProvider.Get().WhereEquals(nameof(TwilioSmsSettingsInfo.TwilioSmsSettingsID), model.Id).FirstOrDefault()
                ?? new TwilioSmsSettingsInfo()
            : new TwilioSmsSettingsInfo();

        info.TwilioSmsSettingsDisplayName = model.DisplayName;
        info.Environment = model.Environment;
        info.AccountSid = model.AccountSid;
        info.AuthToken = model.AuthToken;
        info.FromPhoneNumber = model.FromPhoneNumber;
        info.MessagingServiceSid = model.MessagingServiceSid;
        info.DefaultCountryCode = model.DefaultCountryCode ?? "+1";
        info.IsEnabled = model.IsEnabled;
        info.UseVerifyService = model.UseVerifyService;
        info.VerifyServiceSid = model.VerifyServiceSid;
        info.TwilioSmsSettingsLastModified = DateTime.Now;

        if (!updateExisting)
        {
            info.TwilioSmsSettingsGuid = Guid.NewGuid();
            // Generate a valid code name from display name
            info.TwilioSmsSettingsCodeName = ValidationHelper.GetCodeName(model.DisplayName);
        }

        if (updateExisting)
        {
            SettingsProvider.Set(info);
        }
        else
        {
            SettingsProvider.Set(info);
        }

        return new ModificationResult(ModificationResultState.Success);
    }
}
