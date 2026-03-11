using CMS.Membership;
using CMS.DataEngine;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Admin.DigitalCommerce.UIPages;

using Baseline.Ecommerce.Admin.ViewModels;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

// Register Billing as a top-level Digital Commerce application
[assembly: UIApplication(
    identifier: "Baseline.Ecommerce.Billing",
    type: typeof(Baseline.Ecommerce.Admin.UIPages.BillingApplication),
    slug: "billing",
    name: "Billing",
    category: DigitalCommerceApplicationCategories.DIGITAL_COMMERCE,
    icon: Icons.ShoppingCart,
    templateName: TemplateNames.SECTION_LAYOUT)]

// Register Subscription Plans listing
[assembly: UIPage(
    parentType: typeof(Baseline.Ecommerce.Admin.UIPages.BillingApplication),
    slug: "plans",
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.SubscriptionPlanListPage),
    name: "Subscription Plans",
    templateName: TemplateNames.LISTING,
    order: 100)]

// Register Plan Section (parameterized slug)
[assembly: UIPage(
    parentType: typeof(Baseline.Ecommerce.Admin.UIPages.SubscriptionPlanListPage),
    slug: PageParameterConstants.PARAMETERIZED_SLUG,
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.SubscriptionPlanSectionPage),
    name: "Subscription Plan",
    templateName: TemplateNames.SECTION_LAYOUT,
    order: UIPageOrder.NoOrder)]

// Register Plan Edit
[assembly: UIPage(
    parentType: typeof(Baseline.Ecommerce.Admin.UIPages.SubscriptionPlanSectionPage),
    slug: "edit",
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.SubscriptionPlanEditPage),
    name: "Edit Plan",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

// Register Plan Create
[assembly: UIPage(
    parentType: typeof(Baseline.Ecommerce.Admin.UIPages.SubscriptionPlanListPage),
    slug: "create",
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.SubscriptionPlanCreatePage),
    name: "Create Plan",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Billing application container for subscription plans and customer subscriptions.
/// </summary>
[UIEvaluatePermission(SystemPermissions.VIEW)]
public class BillingApplication : ApplicationPage
{
}

/// <summary>
/// Admin listing page for Subscription Plans.
/// </summary>
[UIEvaluatePermission(SystemPermissions.VIEW)]
public class SubscriptionPlanListPage : ListingPage
{
    protected override string ObjectType => SubscriptionPlanInfo.OBJECT_TYPE;

    [PageCommand(Permission = SystemPermissions.DELETE)]
    public override Task<ICommandResponse<RowActionResult>> Delete(int id) => base.Delete(id);

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        PageConfiguration.Callouts ??= [];

        var dataClassExists = DataClassInfoProvider.GetDataClassInfo("Baseline.SubscriptionPlan") != null;

        if (!dataClassExists)
        {
            PageConfiguration.Callouts.Add(new CalloutConfiguration
            {
                Headline = "Subscription Plans Not Yet Available",
                Content = "The SubscriptionPlan data class has not been installed yet. Please restart the application to complete the installation.",
                Type = CalloutType.QuickTip,
                Placement = CalloutPlacement.OnDesk
            });

            return;
        }

        PageConfiguration.TableActions.AddDeleteAction(nameof(Delete));
        PageConfiguration.HeaderActions.AddLink<SubscriptionPlanCreatePage>("Create plan");
        PageConfiguration.AddEditRowAction<SubscriptionPlanEditPage>();

        PageConfiguration.ColumnConfigurations
            .AddColumn(nameof(SubscriptionPlanInfo.PlanCode), "Code", searchable: true, maxWidth: 14)
            .AddColumn(nameof(SubscriptionPlanInfo.Name), "Name", searchable: true, maxWidth: 18)
            .AddColumn(nameof(SubscriptionPlanInfo.Price), "Price", maxWidth: 10)
            .AddColumn(nameof(SubscriptionPlanInfo.Currency), "Currency", maxWidth: 8)
            .AddColumn(nameof(SubscriptionPlanInfo.BillingInterval), "Interval", maxWidth: 10)
            .AddColumn(nameof(SubscriptionPlanInfo.TrialPeriodDays), "Trial Days", maxWidth: 8)
            .AddColumn(nameof(SubscriptionPlanInfo.TierLevel), "Tier", maxWidth: 6)
            .AddColumn(nameof(SubscriptionPlanInfo.IsFeatured), "Featured", maxWidth: 8)
            .AddColumn(nameof(SubscriptionPlanInfo.IsActive), "Active", maxWidth: 8);
    }
}

/// <summary>
/// Section page for individual Subscription Plan editing.
/// </summary>
[UIEvaluatePermission(SystemPermissions.VIEW)]
public class SubscriptionPlanSectionPage : EditSectionPage<SubscriptionPlanInfo>
{
    protected override async Task<string> GetObjectDisplayName(BaseInfo infoObject)
    {
        if (infoObject is SubscriptionPlanInfo plan)
        {
            return await Task.FromResult(plan.Name);
        }

        return await Task.FromResult("Subscription Plan");
    }
}

/// <summary>
/// Admin page for creating new Subscription Plans.
/// </summary>
[UIEvaluatePermission(SystemPermissions.CREATE)]
public class SubscriptionPlanCreatePage : ModelEditPage<SubscriptionPlanViewModel>
{
    private readonly IInfoProvider<SubscriptionPlanInfo> _planProvider;
    private SubscriptionPlanViewModel? _model;

    public SubscriptionPlanCreatePage(
        IFormItemCollectionProvider formItemCollectionProvider,
        IFormDataBinder formDataBinder,
        IInfoProvider<SubscriptionPlanInfo> planProvider)
        : base(formItemCollectionProvider, formDataBinder)
    {
        _planProvider = planProvider;
    }

    protected override SubscriptionPlanViewModel Model => _model ??= new SubscriptionPlanViewModel
    {
        SubscriptionPlanGuid = Guid.NewGuid(),
        IsActive = true,
        BillingInterval = "Monthly",
        IntervalCount = 1,
        Currency = "USD"
    };

    protected override async Task<ICommandResponse> ProcessFormData(SubscriptionPlanViewModel model, ICollection<IFormItem> formItems)
    {
        try
        {
            var existing = await _planProvider
                .Get()
                .WhereEquals(nameof(SubscriptionPlanInfo.PlanCode), model.PlanCode)
                .GetEnumerableTypedResultAsync();

            if (existing.Any())
            {
                return GetErrorResponse("A plan with this code already exists.");
            }

            var info = new SubscriptionPlanInfo
            {
                SubscriptionPlanGuid = model.SubscriptionPlanGuid,
                PlanCode = model.PlanCode,
                Name = model.Name,
                Description = model.Description,
                Price = model.Price,
                Currency = model.Currency,
                BillingInterval = model.BillingInterval,
                IntervalCount = model.IntervalCount,
                TrialPeriodDays = model.TrialPeriodDays,
                TierLevel = model.TierLevel,
                IsFeatured = model.IsFeatured,
                IsActive = model.IsActive,
                ExternalPlanId = model.ExternalPlanId
            };

            await _planProvider.SetAsync(info);

            return GetSuccessResponse("Subscription plan created successfully.");
        }
        catch (Exception ex)
        {
            return GetErrorResponse($"Failed to create plan: {ex.Message}");
        }
    }

    private ICommandResponse GetSuccessResponse(string message)
    {
        var response = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationSuccess));
        response.AddSuccessMessage(message);
        return response;
    }

    private ICommandResponse GetErrorResponse(string message)
    {
        var response = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationFailure));
        response.AddErrorMessage(message);
        return response;
    }
}

/// <summary>
/// Admin page for editing existing Subscription Plans.
/// </summary>
[UIEvaluatePermission(SystemPermissions.UPDATE)]
public class SubscriptionPlanEditPage : ModelEditPage<SubscriptionPlanViewModel>
{
    private SubscriptionPlanViewModel? _model;

    public SubscriptionPlanEditPage(
        IFormItemCollectionProvider formItemCollectionProvider,
        IFormDataBinder formDataBinder)
        : base(formItemCollectionProvider, formDataBinder)
    {
    }

    [PageParameter(typeof(IntPageModelBinder))]
    public int ObjectId { get; set; }

    protected override SubscriptionPlanViewModel Model
    {
        get
        {
            if (_model == null)
            {
                var info = Provider<SubscriptionPlanInfo>.Instance.Get()
                    .WhereEquals(nameof(SubscriptionPlanInfo.SubscriptionPlanInfoID), ObjectId)
                    .FirstOrDefault();

                if (info != null)
                {
                    _model = new SubscriptionPlanViewModel
                    {
                        SubscriptionPlanInfoID = info.SubscriptionPlanInfoID,
                        SubscriptionPlanGuid = info.SubscriptionPlanGuid,
                        PlanCode = info.PlanCode,
                        Name = info.Name,
                        Description = info.Description ?? "",
                        Price = info.Price,
                        Currency = info.Currency,
                        BillingInterval = info.BillingInterval,
                        IntervalCount = info.IntervalCount,
                        TrialPeriodDays = info.TrialPeriodDays,
                        TierLevel = info.TierLevel,
                        IsFeatured = info.IsFeatured,
                        IsActive = info.IsActive,
                        ExternalPlanId = info.ExternalPlanId
                    };
                }
                else
                {
                    _model = new SubscriptionPlanViewModel();
                }
            }
            return _model;
        }
    }

    protected override async Task<ICommandResponse> ProcessFormData(SubscriptionPlanViewModel model, ICollection<IFormItem> formItems)
    {
        try
        {
            var info = Provider<SubscriptionPlanInfo>.Instance.Get()
                .WhereEquals(nameof(SubscriptionPlanInfo.SubscriptionPlanInfoID), ObjectId)
                .FirstOrDefault();

            if (info == null)
            {
                return GetErrorResponse("Subscription plan not found.");
            }

            info.PlanCode = model.PlanCode;
            info.Name = model.Name;
            info.Description = model.Description;
            info.Price = model.Price;
            info.Currency = model.Currency;
            info.BillingInterval = model.BillingInterval;
            info.IntervalCount = model.IntervalCount;
            info.TrialPeriodDays = model.TrialPeriodDays;
            info.TierLevel = model.TierLevel;
            info.IsFeatured = model.IsFeatured;
            info.IsActive = model.IsActive;
            info.ExternalPlanId = model.ExternalPlanId;

            await Provider<SubscriptionPlanInfo>.Instance.SetAsync(info);

            return GetSuccessResponse("Subscription plan updated successfully.");
        }
        catch (Exception ex)
        {
            return GetErrorResponse($"Failed to update plan: {ex.Message}");
        }
    }

    private ICommandResponse GetSuccessResponse(string message)
    {
        var response = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationSuccess));
        response.AddSuccessMessage(message);
        return response;
    }

    private ICommandResponse GetErrorResponse(string message)
    {
        var response = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationFailure));
        response.AddErrorMessage(message);
        return response;
    }
}
