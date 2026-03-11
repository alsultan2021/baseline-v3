using CMS.DataEngine;
using CMS.Membership;

using Baseline.Automation.Models;
using Baseline.Automation.Admin.ViewModels;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

[assembly: UIPage(
    parentType: typeof(Baseline.Automation.Admin.UIPages.AutomationProcessBuilderPage),
    slug: "general",
    uiPageType: typeof(Baseline.Automation.Admin.UIPages.AutomationProcessEditPage),
    name: "General",
    templateName: TemplateNames.EDIT,
    order: 100,
    Icon = Icons.Cogwheel)]

namespace Baseline.Automation.Admin.UIPages;

/// <summary>
/// General settings edit page for an automation process.
/// </summary>
[UIPageLocation(PageLocationEnum.SidePanel)]
[UIEvaluatePermission(SystemPermissions.VIEW)]
public class AutomationProcessEditPage(
    IFormItemCollectionProvider formItemCollectionProvider,
    IFormDataBinder formDataBinder,
    IInfoProvider<AutomationProcessInfo> processInfoProvider)
    : ModelEditPage<AutomationProcessViewModel>(formItemCollectionProvider, formDataBinder)
{
    private AutomationProcessViewModel? model;

    /// <summary>Object ID from the parent section page.</summary>
    [PageParameter(typeof(IntPageModelBinder))]
    public int ObjectId { get; set; }

    protected override AutomationProcessViewModel Model
    {
        get
        {
            if (model != null) return model;

            var info = processInfoProvider.Get()
                .WhereEquals(nameof(AutomationProcessInfo.AutomationProcessID), ObjectId)
                .FirstOrDefault();

            if (info == null)
            {
                model = new AutomationProcessViewModel();
                return model;
            }

            model = new AutomationProcessViewModel
            {
                Name = info.AutomationProcessDisplayName ?? "",
                CodeName = info.AutomationProcessName ?? "",
                Recurrence = info.AutomationProcessRecurrence ?? "IfNotAlreadyRunning"
            };

            return model;
        }
    }

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        PageConfiguration.Headline = "General";
        PageConfiguration.SubmitConfiguration.Label = "Apply";

        PageConfiguration.Callouts =
        [
            new CalloutConfiguration
            {
                Headline = "Not sure how to set up process recurrence?",
                Content = "<strong>Always</strong> - Runs whenever a contact meets the trigger conditions, even if the process is already running for the same contact. Use with caution, as this may result in unwanted actions, such as contacts receiving the same email multiple times.<br/><br/><strong>If not already running</strong> - Runs whenever a contact meets the trigger conditions, but only if the process isn't currently running for the same contact. Runs repeatedly if a contact meets the trigger conditions again after completing the process.<br/><br/><strong>Only once</strong> - Runs once when a contact meets the trigger conditions, and never repeats for the same contact.",
                ContentAsHtml = true,
                Type = CalloutType.QuickTip,
                Placement = CalloutPlacement.OnPaper
            }
        ];
    }

    protected override async Task<ICommandResponse> ProcessFormData(
        AutomationProcessViewModel model,
        ICollection<IFormItem> formItems)
    {
        var info = processInfoProvider.Get()
            .WhereEquals(nameof(AutomationProcessInfo.AutomationProcessID), ObjectId)
            .FirstOrDefault();

        if (info == null)
        {
            return ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.Error))
                .AddErrorMessage("Process not found.");
        }

        var validationErrors = new List<string>();

        if (string.IsNullOrWhiteSpace(model.Name))
        {
            validationErrors.Add("Process name is required.");
        }

        if (!string.IsNullOrWhiteSpace(model.Name))
        {
            var duplicate = processInfoProvider.Get()
                .WhereEquals(nameof(AutomationProcessInfo.AutomationProcessDisplayName), model.Name)
                .WhereNotEquals(nameof(AutomationProcessInfo.AutomationProcessID), ObjectId)
                .FirstOrDefault();

            if (duplicate != null)
            {
                validationErrors.Add($"A process with the name \"{model.Name}\" already exists.");
            }
        }

        if (validationErrors.Count > 0)
        {
            var errorResponse = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationFailure));
            foreach (var error in validationErrors)
            {
                errorResponse.AddErrorMessage(error);
            }
            return errorResponse;
        }

        info.AutomationProcessDisplayName = model.Name;
        info.AutomationProcessName = model.CodeName;
        info.AutomationProcessRecurrence = model.Recurrence;

        processInfoProvider.Set(info);

        return ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationSuccess))
            .AddSuccessMessage("Process updated successfully.");
    }
}
