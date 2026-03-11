using CMS.Membership;
using CMS.Helpers;

using Baseline.Automation.Models;
using Baseline.Automation.Admin.ViewModels;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

using CMS.DataEngine;

[assembly: UIPage(
    parentType: typeof(Baseline.Automation.Admin.UIPages.AutomationProcessListPage),
    slug: "create",
    uiPageType: typeof(Baseline.Automation.Admin.UIPages.AutomationProcessCreatePage),
    name: "New process",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Baseline.Automation.Admin.UIPages;

/// <summary>
/// Create page for a new automation process.
/// Simplified form: Name + Recurrence only, then navigates to builder.
/// </summary>
[UIEvaluatePermission(SystemPermissions.CREATE)]
public class AutomationProcessCreatePage(
    IFormItemCollectionProvider formItemCollectionProvider,
    IFormDataBinder formDataBinder,
    IInfoProvider<AutomationProcessInfo> processInfoProvider,
    IPageLinkGenerator pageLinkGenerator)
    : ModelEditPage<AutomationProcessCreateViewModel>(formItemCollectionProvider, formDataBinder)
{
    private AutomationProcessCreateViewModel? model;

    protected override AutomationProcessCreateViewModel Model => model ??= new AutomationProcessCreateViewModel();

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        PageConfiguration.Callouts =
        [
            new CalloutConfiguration
            {
                Headline = "Create a new automation process",
                Content = "After creation you will be redirected to the visual builder where you can add triggers and steps.",
                Type = CalloutType.QuickTip,
                Placement = CalloutPlacement.OnDesk
            }
        ];
    }

    protected override async Task<ICommandResponse> ProcessFormData(
        AutomationProcessCreateViewModel model,
        ICollection<IFormItem> formItems)
    {
        var validationErrors = new List<string>();

        if (string.IsNullOrWhiteSpace(model.Name))
        {
            validationErrors.Add("Process name is required.");
        }

        if (!string.IsNullOrWhiteSpace(model.Name))
        {
            var duplicate = processInfoProvider.Get()
                .WhereEquals(nameof(AutomationProcessInfo.AutomationProcessDisplayName), model.Name)
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

        var codeName = ValidationHelper.GetCodeName(model.Name);
        var now = DateTime.Now;

        var info = new AutomationProcessInfo
        {
            AutomationProcessName = codeName,
            AutomationProcessDisplayName = model.Name,
            AutomationProcessRecurrence = model.Recurrence,
            AutomationProcessIsEnabled = false,
            AutomationProcessTriggerJson = "{}",
            AutomationProcessStepsJson = "[]",
            AutomationProcessCreatedWhen = now,
            AutomationProcessLastModified = now
        };

        processInfoProvider.Set(info);

        var pageParams = new PageParameterValues
        {
            { typeof(AutomationProcessSectionPage), info.AutomationProcessID }
        };
        var editUrl = pageLinkGenerator.GetPath<AutomationProcessSectionPage>(pageParams);

        var result = NavigateTo(editUrl);
        result.AddSuccessMessage($"Process \"{model.Name}\" created.");

        return await Task.FromResult<ICommandResponse>(result);
    }
}
