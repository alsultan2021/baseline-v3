using CMS.DataEngine;
using CMS.FormEngine;
using CMS.Membership;
using Baseline.Core.Installers.Schemas;

namespace Baseline.Core;

/// <summary>
/// Installer for Baseline Core module schemas and member fields.
/// Uses modular schema classes for maintainability.
/// </summary>
public class BaselineModuleInstaller(BaselineCoreInstallerOptions baselineCoreInstallerOptions)
{
    private readonly BaselineCoreInstallerOptions _baselineCoreInstallerOptions = baselineCoreInstallerOptions;

    public bool InstallationRan { get; set; } = false;

    /// <summary>
    /// Installs all Baseline Core schemas and optionally adds member fields.
    /// </summary>
    public Task Install()
    {
        if (_baselineCoreInstallerOptions.AddMemberFields)
        {
            AddMemberFields();
        }

        // Install schemas using modular classes
        MetadataSchema.Install();
        RedirectSchema.Install();
        HasImageSchema.Install();

        InstallationRan = true;

        return Task.CompletedTask;
    }

    /// <summary>
    /// Adds custom member fields (FirstName, MiddleName, LastName, PhoneNumber, PreferredCurrency) to the MemberInfo class.
    /// Also adds 2FA fields (AuthenticatorKey, RecoveryCodes) for two-factor authentication support.
    /// </summary>
    private static void AddMemberFields()
    {
        var memberClass = DataClassInfoProvider.GetClasses()
            .WhereEquals(nameof(DataClassInfo.ClassName), MemberInfo.OBJECT_TYPE)
            .FirstOrDefault();

        if (memberClass != null)
        {
            var form = new FormInfo(memberClass.ClassFormDefinition);

            AddMemberTextField(form, "MemberFirstName");
            AddMemberTextField(form, "MemberMiddleName");
            AddMemberTextField(form, "MemberLastName");
            // Note: Phone and Company are stored on CustomerAddress, not Member
            // Note: MemberPreferredCurrency is an integer field referencing Baseline_Currency.
            // It should be added via Kentico Admin > Modules > Membership > Member > Database columns
            // with type "Integer number" and reference to "ObjectType.baseline_currency"

            // 2FA fields for two-factor authentication
            AddMemberTextField(form, "MemberAuthenticatorKey", 256);
            AddMemberTextField(form, "MemberRecoveryCodes", 1000);

            memberClass.ClassFormDefinition = form.GetXmlDefinition();
            if (memberClass.HasChanged)
            {
                DataClassInfoProvider.SetDataClassInfo(memberClass);
            }
        }
    }

    private static void AddMemberTextField(FormInfo form, string fieldName, int size = 100)
    {
        var existingField = form.GetFormField(fieldName);
        var field = existingField ?? new FormFieldInfo();
        field.Name = fieldName;
        field.Precision = 0;
        field.Size = size;
        field.DataType = "text";
        field.Enabled = true;
        field.AllowEmpty = true;

        if (existingField != null)
        {
            form.UpdateFormField(fieldName, field);
        }
        else
        {
            form.AddFormItem(field);
        }
    }
}
