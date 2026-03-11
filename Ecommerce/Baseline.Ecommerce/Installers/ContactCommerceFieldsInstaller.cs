using CMS.DataEngine;
using CMS.FormEngine;
using Microsoft.Extensions.Logging;

namespace Baseline.Ecommerce.Installers;

/// <summary>
/// Interface for installing commerce-related custom fields on OM_Contact.
/// </summary>
public interface IContactCommerceFieldsInstaller
{
    /// <summary>
    /// Ensures custom commerce columns exist on the OM_Contact data class.
    /// </summary>
    void Install();
}

/// <summary>
/// Adds custom commerce metric columns to the existing OM_Contact data class
/// for contact-group-based segmentation and marketing automation.
/// </summary>
/// <remarks>
/// Fields installed:
/// <list type="bullet">
///   <item><c>ContactTotalOrders</c> — lifetime order count</item>
///   <item><c>ContactTotalSpent</c> — lifetime spend (decimal)</item>
///   <item><c>ContactLastOrderDate</c> — most recent order date</item>
///   <item><c>ContactAverageOrderValue</c> — average order total (decimal)</item>
///   <item><c>ContactCommerceLastSyncedAt</c> — last sync timestamp</item>
/// </list>
/// </remarks>
public class ContactCommerceFieldsInstaller(
    ILogger<ContactCommerceFieldsInstaller> logger) : IContactCommerceFieldsInstaller
{
    /// <summary>Column name for total orders count.</summary>
    public const string FIELD_TOTAL_ORDERS = "ContactTotalOrders";
    /// <summary>Column name for total amount spent.</summary>
    public const string FIELD_TOTAL_SPENT = "ContactTotalSpent";
    /// <summary>Column name for last order date.</summary>
    public const string FIELD_LAST_ORDER_DATE = "ContactLastOrderDate";
    /// <summary>Column name for average order value.</summary>
    public const string FIELD_AVERAGE_ORDER_VALUE = "ContactAverageOrderValue";
    /// <summary>Column name for last commerce sync timestamp.</summary>
    public const string FIELD_LAST_SYNCED_AT = "ContactCommerceLastSyncedAt";

    /// <summary>
    /// Ensures custom commerce columns exist on the OM_Contact data class.
    /// Safe to call repeatedly — skips fields that already exist.
    /// </summary>
    public void Install()
    {
        var classInfo = DataClassInfoProvider.GetDataClassInfo("om.contact");
        if (classInfo is null)
        {
            logger.LogWarning("ContactCommerceFields: OM_Contact class not found — skipping install");
            return;
        }

        var formInfo = new FormInfo(classInfo.ClassFormDefinition);
        bool changed = false;

        changed |= EnsureField(formInfo, FIELD_TOTAL_ORDERS, FieldDataType.Integer, allowEmpty: true,
            caption: "Total Orders");

        changed |= EnsureField(formInfo, FIELD_TOTAL_SPENT, FieldDataType.Decimal, allowEmpty: true,
            caption: "Total Spent", precision: 2, size: 18);

        changed |= EnsureField(formInfo, FIELD_LAST_ORDER_DATE, FieldDataType.DateTime, allowEmpty: true,
            caption: "Last Order Date");

        changed |= EnsureField(formInfo, FIELD_AVERAGE_ORDER_VALUE, FieldDataType.Decimal, allowEmpty: true,
            caption: "Average Order Value", precision: 2, size: 18);

        changed |= EnsureField(formInfo, FIELD_LAST_SYNCED_AT, FieldDataType.DateTime, allowEmpty: true,
            caption: "Commerce Last Synced");

        if (changed)
        {
            classInfo.ClassFormDefinition = formInfo.GetXmlDefinition();
            classInfo.Update();
            logger.LogInformation("ContactCommerceFields: Custom commerce columns installed on OM_Contact");
        }
        else
        {
            logger.LogDebug("ContactCommerceFields: All commerce columns already present on OM_Contact");
        }
    }

    /// <summary>
    /// Adds a field to the form definition if it does not already exist.
    /// </summary>
    /// <returns><c>true</c> if the field was added; <c>false</c> if it already existed.</returns>
    private static bool EnsureField(
        FormInfo formInfo,
        string name,
        string dataType,
        bool allowEmpty,
        string? caption = null,
        int precision = 0,
        int size = 0)
    {
        if (formInfo.GetFormField(name) is not null)
        {
            return false;
        }

        var field = new FormFieldInfo
        {
            Name = name,
            DataType = dataType,
            AllowEmpty = allowEmpty,
            Visible = false,
            Enabled = true,
            System = false,
            Precision = precision,
        };

        if (size > 0)
        {
            field.Size = size;
        }

        if (!string.IsNullOrEmpty(caption))
        {
            field.Caption = caption;
        }

        formInfo.AddFormItem(field);
        return true;
    }
}
