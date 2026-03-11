using CMS.Membership;
using CMS.DataEngine;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

using Baseline.Ecommerce.Admin.ViewModels;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

// Register Customer Subscriptions listing under Billing app
[assembly: UIPage(
    parentType: typeof(Baseline.Ecommerce.Admin.UIPages.BillingApplication),
    slug: "subscriptions",
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.CustomerSubscriptionListPage),
    name: "Customer Subscriptions",
    templateName: TemplateNames.LISTING,
    order: 200)]

// Register Subscription Section (parameterized slug)
[assembly: UIPage(
    parentType: typeof(Baseline.Ecommerce.Admin.UIPages.CustomerSubscriptionListPage),
    slug: PageParameterConstants.PARAMETERIZED_SLUG,
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.CustomerSubscriptionSectionPage),
    name: "Subscription",
    templateName: TemplateNames.SECTION_LAYOUT,
    order: UIPageOrder.NoOrder)]

// Register Subscription Edit
[assembly: UIPage(
    parentType: typeof(Baseline.Ecommerce.Admin.UIPages.CustomerSubscriptionSectionPage),
    slug: "edit",
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.CustomerSubscriptionEditPage),
    name: "Edit Subscription",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

// Register Subscription Create
[assembly: UIPage(
    parentType: typeof(Baseline.Ecommerce.Admin.UIPages.CustomerSubscriptionListPage),
    slug: "create",
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.CustomerSubscriptionCreatePage),
    name: "Create Subscription",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Admin listing page for Customer Subscriptions.
/// </summary>
[UIEvaluatePermission(SystemPermissions.VIEW)]
public class CustomerSubscriptionListPage : ListingPage
{
    protected override string ObjectType => CustomerSubscriptionInfo.OBJECT_TYPE;

    [PageCommand(Permission = SystemPermissions.DELETE)]
    public override Task<ICommandResponse<RowActionResult>> Delete(int id) => base.Delete(id);

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        PageConfiguration.Callouts ??= [];

        var dataClassExists = DataClassInfoProvider.GetDataClassInfo("Baseline.CustomerSubscription") != null;

        if (!dataClassExists)
        {
            PageConfiguration.Callouts.Add(new CalloutConfiguration
            {
                Headline = "Subscriptions Not Yet Available",
                Content = "The CustomerSubscription data class has not been installed yet. Please restart the application to complete the installation.",
                Type = CalloutType.QuickTip,
                Placement = CalloutPlacement.OnDesk
            });

            return;
        }

        PageConfiguration.TableActions.AddDeleteAction(nameof(Delete));
        PageConfiguration.HeaderActions.AddLink<CustomerSubscriptionCreatePage>("Create subscription");
        PageConfiguration.AddEditRowAction<CustomerSubscriptionEditPage>();

        PageConfiguration.ColumnConfigurations
            .AddColumn(nameof(CustomerSubscriptionInfo.CustomerId), "Customer ID", maxWidth: 10)
            .AddColumn(nameof(CustomerSubscriptionInfo.PlanId), "Plan ID", maxWidth: 8)
            .AddColumn(nameof(CustomerSubscriptionInfo.Status), "Status", searchable: true, maxWidth: 10)
            .AddColumn(nameof(CustomerSubscriptionInfo.StartDate), "Start Date", maxWidth: 14)
            .AddColumn(nameof(CustomerSubscriptionInfo.CurrentPeriodEnd), "Period End", maxWidth: 14)
            .AddColumn(nameof(CustomerSubscriptionInfo.CancelAtPeriodEnd), "Cancel at End", maxWidth: 10)
            .AddColumn(nameof(CustomerSubscriptionInfo.ExternalSubscriptionId), "External ID", searchable: true, maxWidth: 16)
            .AddColumn(nameof(CustomerSubscriptionInfo.CreatedOn), "Created", maxWidth: 14);
    }
}

/// <summary>
/// Section page for individual Customer Subscription editing.
/// </summary>
[UIEvaluatePermission(SystemPermissions.VIEW)]
public class CustomerSubscriptionSectionPage : EditSectionPage<CustomerSubscriptionInfo>
{
    protected override async Task<string> GetObjectDisplayName(BaseInfo infoObject)
    {
        if (infoObject is CustomerSubscriptionInfo sub)
        {
            return await Task.FromResult($"Subscription #{sub.CustomerSubscriptionInfoID} ({sub.Status})");
        }

        return await Task.FromResult("Subscription");
    }
}

/// <summary>
/// Admin page for creating new Customer Subscriptions.
/// </summary>
[UIEvaluatePermission(SystemPermissions.CREATE)]
public class CustomerSubscriptionCreatePage : ModelEditPage<CustomerSubscriptionViewModel>
{
    private readonly IInfoProvider<CustomerSubscriptionInfo> _subProvider;
    private CustomerSubscriptionViewModel? _model;

    public CustomerSubscriptionCreatePage(
        IFormItemCollectionProvider formItemCollectionProvider,
        IFormDataBinder formDataBinder,
        IInfoProvider<CustomerSubscriptionInfo> subProvider)
        : base(formItemCollectionProvider, formDataBinder)
    {
        _subProvider = subProvider;
    }

    protected override CustomerSubscriptionViewModel Model => _model ??= new CustomerSubscriptionViewModel
    {
        SubscriptionGuid = Guid.NewGuid(),
        Status = "Active",
        StartDate = DateTime.UtcNow,
        CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1)
    };

    protected override async Task<ICommandResponse> ProcessFormData(CustomerSubscriptionViewModel model, ICollection<IFormItem> formItems)
    {
        try
        {
            var info = new CustomerSubscriptionInfo
            {
                SubscriptionGuid = model.SubscriptionGuid,
                CustomerId = model.CustomerId,
                PlanId = int.TryParse(model.PlanId, out var pid) ? pid : 0,
                Status = model.Status,
                StartDate = model.StartDate,
                CurrentPeriodEnd = model.CurrentPeriodEnd,
                TrialEnd = model.TrialEnd,
                CancelAtPeriodEnd = model.CancelAtPeriodEnd,
                CancellationReason = model.CancellationReason,
                ExternalSubscriptionId = model.ExternalSubscriptionId,
                CouponCode = model.CouponCode,
                CreatedOn = DateTime.UtcNow
            };

            await _subProvider.SetAsync(info);

            return GetSuccessResponse("Subscription created successfully.");
        }
        catch (Exception ex)
        {
            return GetErrorResponse($"Failed to create subscription: {ex.Message}");
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
/// Admin page for editing existing Customer Subscriptions.
/// </summary>
[UIEvaluatePermission(SystemPermissions.UPDATE)]
public class CustomerSubscriptionEditPage : ModelEditPage<CustomerSubscriptionViewModel>
{
    private CustomerSubscriptionViewModel? _model;

    public CustomerSubscriptionEditPage(
        IFormItemCollectionProvider formItemCollectionProvider,
        IFormDataBinder formDataBinder)
        : base(formItemCollectionProvider, formDataBinder)
    {
    }

    [PageParameter(typeof(IntPageModelBinder))]
    public int ObjectId { get; set; }

    protected override CustomerSubscriptionViewModel Model
    {
        get
        {
            if (_model == null)
            {
                var info = Provider<CustomerSubscriptionInfo>.Instance.Get()
                    .WhereEquals(nameof(CustomerSubscriptionInfo.CustomerSubscriptionInfoID), ObjectId)
                    .FirstOrDefault();

                if (info != null)
                {
                    _model = new CustomerSubscriptionViewModel
                    {
                        CustomerSubscriptionInfoID = info.CustomerSubscriptionInfoID,
                        SubscriptionGuid = info.SubscriptionGuid,
                        CustomerId = info.CustomerId,
                        PlanId = info.PlanId.ToString(),
                        Status = info.Status,
                        StartDate = info.StartDate,
                        CurrentPeriodEnd = info.CurrentPeriodEnd,
                        TrialEnd = info.TrialEnd,
                        CancelAtPeriodEnd = info.CancelAtPeriodEnd,
                        CancellationReason = info.CancellationReason,
                        ExternalSubscriptionId = info.ExternalSubscriptionId,
                        CouponCode = info.CouponCode
                    };
                }
                else
                {
                    _model = new CustomerSubscriptionViewModel();
                }
            }
            return _model;
        }
    }

    protected override async Task<ICommandResponse> ProcessFormData(CustomerSubscriptionViewModel model, ICollection<IFormItem> formItems)
    {
        try
        {
            var info = Provider<CustomerSubscriptionInfo>.Instance.Get()
                .WhereEquals(nameof(CustomerSubscriptionInfo.CustomerSubscriptionInfoID), ObjectId)
                .FirstOrDefault();

            if (info == null)
            {
                return GetErrorResponse("Subscription not found.");
            }

            info.CustomerId = model.CustomerId;
            info.PlanId = int.TryParse(model.PlanId, out var pid) ? pid : 0;
            info.Status = model.Status;
            info.StartDate = model.StartDate;
            info.CurrentPeriodEnd = model.CurrentPeriodEnd;
            info.TrialEnd = model.TrialEnd;
            info.CancelAtPeriodEnd = model.CancelAtPeriodEnd;
            info.CancellationReason = model.CancellationReason;
            info.ExternalSubscriptionId = model.ExternalSubscriptionId;
            info.CouponCode = model.CouponCode;
            info.ModifiedOn = DateTime.UtcNow;

            await Provider<CustomerSubscriptionInfo>.Instance.SetAsync(info);

            return GetSuccessResponse("Subscription updated successfully.");
        }
        catch (Exception ex)
        {
            return GetErrorResponse($"Failed to update subscription: {ex.Message}");
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
