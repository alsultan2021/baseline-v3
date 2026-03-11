using Baseline.Ecommerce.Models;
using CMS.DataEngine;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Baseline.Ecommerce.Admin.DataProviders;

/// <summary>
/// Data provider for tax class dropdown options.
/// </summary>
public class TaxClassDataProvider : IDropDownOptionsProvider
{
    /// <summary>
    /// Gets the dropdown options for tax classes.
    /// </summary>
    /// <returns>Collection of dropdown option items.</returns>
    public async Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        try
        {
            // Check if the data class exists
            var dataClassExists = DataClassInfoProvider.GetDataClassInfo("Baseline.TaxClass") != null;

            if (!dataClassExists)
            {
                return new List<DropDownOptionItem>
                {
                    new() { Value = "0", Text = "Tax class data not installed" }
                };
            }

            var taxClasses = await TaxClassInfo.Provider.Get()
                .WhereTrue(nameof(TaxClassInfo.TaxClassEnabled))
                .OrderByDescending(nameof(TaxClassInfo.TaxClassIsDefault))
                .OrderBy(nameof(TaxClassInfo.TaxClassOrder))
                .GetEnumerableTypedResultAsync();

            var options = new List<DropDownOptionItem>
            {
                new() { Value = "0", Text = "Select a tax class" }
            };

            options.AddRange(taxClasses.Select(taxClass => new DropDownOptionItem
            {
                Value = taxClass.TaxClassID.ToString(),
                Text = $"{taxClass.TaxClassDisplayName} ({taxClass.TaxClassDefaultRate:F2}%)"
            }));

            return options;
        }
        catch (Exception)
        {
            return new List<DropDownOptionItem>
            {
                new() { Value = "0", Text = "Error loading tax classes" }
            };
        }
    }
}
