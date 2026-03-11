using Baseline.Account;
using CMS.Membership;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Baseline.Account.RCL.Features.DeviceAuthorization;

#region View Models

/// <summary>
/// View model for the device requesting authorization (shows code + QR + poll).
/// </summary>
public sealed class DeviceCodeViewModel
{
    /// <summary>Short code displayed to user, e.g. "ABCD-1234".</summary>
    public string UserCode { get; set; } = "";

    /// <summary>Secret device code for polling (not shown to user).</summary>
    public string DeviceCode { get; set; } = "";

    /// <summary>URL user should visit to enter code.</summary>
    public string VerificationUrl { get; set; } = "";

    /// <summary>Full URL with code pre-filled (for QR code).</summary>
    public string VerificationUrlComplete { get; set; } = "";

    /// <summary>Seconds between poll requests.</summary>
    public int IntervalSeconds { get; set; } = 5;

    /// <summary>When the code expires (UTC ISO 8601).</summary>
    public string ExpiresAt { get; set; } = "";
}

/// <summary>
/// View model for the authenticated user approving a device (shows device info + approve/deny).
/// </summary>
public sealed class DeviceApproveViewModel
{
    /// <summary>The user code being approved.</summary>
    public string UserCode { get; set; } = "";

    /// <summary>Device name of the requesting device.</summary>
    public string? DeviceName { get; set; }

    /// <summary>IP address of the requesting device.</summary>
    public string? IpAddress { get; set; }

    /// <summary>Whether code was found and is pending.</summary>
    public bool CodeFound { get; set; }

    /// <summary>Whether the action was completed successfully.</summary>
    public bool Completed { get; set; }

    /// <summary>Result message.</summary>
    public string? Message { get; set; }

    /// <summary>Error message if any.</summary>
    public string? Error { get; set; }
}

/// <summary>
/// View model for the trusted devices management page.
/// </summary>
public sealed class TrustedDevicesViewModel
{
    public IReadOnlyList<TrustedDeviceViewModel> Devices { get; set; } = [];
    public string? Message { get; set; }
    public string? Error { get; set; }
}

#endregion

/// <summary>
/// Controller for device authorization (code-based sign-in) and trusted device management.
/// </summary>
[TypeFilter(typeof(LanguagePrefixRouteFilter))]
public sealed class DeviceAuthorizationController(
    IDeviceAuthorizationService deviceAuthService,
    ITrustedDeviceService trustedDeviceService,
    IUserRepository userRepository,
    ISignInManagerService signInManagerService,
    ILoginAuditService loginAuditService,
    ILogger<DeviceAuthorizationController> logger) : Controller
{
    #region Route Constants

    public const string RequestCodeUrl = "Account/DeviceCode";
    public const string PollUrl = "Account/DeviceCode/Poll";
    public const string AuthorizeUrl = "Account/DeviceAuthorize";
    public const string TrustedDevicesUrl = "Account/TrustedDevices";
    public const string RevokeDeviceUrl = "Account/TrustedDevices/Revoke";
    public const string RevokeAllDevicesUrl = "Account/TrustedDevices/RevokeAll";

    #endregion

    #region Device Code Flow — Requesting Device (unauthenticated)

    /// <summary>
    /// GET: Display a new device code + QR for an unauthenticated device.
    /// </summary>
    [HttpGet]
    [Route(RequestCodeUrl)]
    [Route("{language}/" + RequestCodeUrl)]
    public async Task<IActionResult> RequestCode()
    {
        var result = await deviceAuthService.CreateAuthorizationRequestAsync();

        var model = new DeviceCodeViewModel
        {
            UserCode = result.UserCode,
            DeviceCode = result.DeviceCode,
            VerificationUrl = result.VerificationUrl,
            VerificationUrlComplete = result.VerificationUrlComplete,
            IntervalSeconds = result.IntervalSeconds,
            ExpiresAt = result.ExpiresAt.ToString("o")
        };

        return View("~/Features/Account/DeviceAuthorization/DeviceCode.cshtml", model);
    }

    /// <summary>
    /// GET (AJAX/JSON): Poll for device authorization status.
    /// </summary>
    [HttpGet]
    [Route(PollUrl)]
    [Route("{language}/" + PollUrl)]
    public async Task<IActionResult> Poll([FromQuery] string deviceCode)
    {
        if (string.IsNullOrEmpty(deviceCode))
            return Json(new { error = "Missing device code" });

        var result = await deviceAuthService.PollAuthorizationAsync(deviceCode);

        if (result.Authorized && result.ApprovedByMemberID.HasValue)
        {
            // Sign in the user on this device
            var userResult = result.ApprovedByUsername != null
                ? await userRepository.GetUserAsync(result.ApprovedByUsername)
                : CSharpFunctionalExtensions.Result.Failure<IUser>("User not found");

            if (userResult.IsSuccess)
            {
                await signInManagerService.SignInByNameAsync(userResult.Value.UserName, isPersistent: true);

                // Trust this device
                await trustedDeviceService.TrustCurrentDeviceAsync(userResult.Value.UserId);

                // Log the action
                await loginAuditService.LogLoginAttemptAsync(new LoginAttemptContext
                {
                    MemberId = userResult.Value.UserId,
                    Username = userResult.Value.UserName,
                    ActionType = LoginAuditActionType.LoginSuccess,
                    IsSuccess = true
                });
            }
        }

        return Json(new
        {
            authorized = result.Authorized,
            pending = result.Pending,
            denied = result.Denied,
            expired = result.Expired,
            error = result.Error
        });
    }

    #endregion

    #region Device Authorization — Authenticated User (approver)

    /// <summary>
    /// GET: Show the approval page where an authenticated user enters/confirms a code.
    /// </summary>
    [Authorize]
    [HttpGet]
    [Route(AuthorizeUrl)]
    [Route("{language}/" + AuthorizeUrl)]
    public async Task<IActionResult> Authorize([FromQuery] string? code)
    {
        var model = new DeviceApproveViewModel();

        if (!string.IsNullOrEmpty(code))
        {
            var auth = await deviceAuthService.GetPendingByUserCodeAsync(code);
            if (auth != null)
            {
                model.UserCode = code.Trim().ToUpperInvariant();
                model.DeviceName = auth.RequestingDeviceName;
                model.IpAddress = auth.RequestingIpAddress;
                model.CodeFound = true;
            }
            else
            {
                model.UserCode = code;
                model.Error = "Code not found or expired. Please check and try again.";
            }
        }

        return View("~/Features/Account/DeviceAuthorization/DeviceAuthorize.cshtml", model);
    }

    /// <summary>
    /// POST: Approve or deny a device authorization request.
    /// </summary>
    [Authorize]
    [HttpPost]
    [Route(AuthorizeUrl)]
    [Route("{language}/" + AuthorizeUrl)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Authorize(DeviceApproveViewModel model, [FromForm] string action)
    {
        try
        {
            var userResult = await userRepository.GetUserAsync(User.Identity?.Name ?? "");
            if (userResult.IsFailure)
            {
                model.Error = "Authentication error. Please sign in again.";
                return View("~/Features/Account/DeviceAuthorization/DeviceAuthorize.cshtml", model);
            }

            if (action == "approve")
            {
                var success = await deviceAuthService.ApproveAuthorizationAsync(model.UserCode, userResult.Value.UserId);
                model.Completed = true;
                model.Message = success
                    ? "Device authorized successfully! The other device is now signed in."
                    : "Code not found or expired.";
            }
            else if (action == "deny")
            {
                await deviceAuthService.DenyAuthorizationAsync(model.UserCode);
                model.Completed = true;
                model.Message = "Device authorization denied.";
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing device authorization for code {Code}", model.UserCode);
            model.Error = "An error occurred. Please try again.";
        }

        return View("~/Features/Account/DeviceAuthorization/DeviceAuthorize.cshtml", model);
    }

    #endregion

    #region Trusted Devices Management

    /// <summary>
    /// GET: Show list of trusted devices for the current user.
    /// </summary>
    [Authorize]
    [HttpGet]
    [Route(TrustedDevicesUrl)]
    [Route("{language}/" + TrustedDevicesUrl)]
    public async Task<IActionResult> TrustedDevices()
    {
        var userResult = await userRepository.GetUserAsync(User.Identity?.Name ?? "");
        if (userResult.IsFailure)
            return RedirectToAction("LogIn", "LogIn");

        var devices = await trustedDeviceService.GetTrustedDevicesAsync(userResult.Value.UserId);
        var currentToken = (trustedDeviceService as TrustedDeviceService)?.GetCurrentDeviceToken();

        var model = new TrustedDevicesViewModel
        {
            Devices = devices.Select(d => new TrustedDeviceViewModel
            {
                TrustedDeviceID = d.TrustedDeviceID,
                DeviceName = d.DeviceName,
                DeviceType = d.DeviceType,
                Browser = d.Browser,
                OperatingSystem = d.OperatingSystem,
                IpAddress = d.IpAddress,
                TrustedAt = d.TrustedAt,
                LastUsedAt = d.LastUsedAt,
                IsCurrentDevice = d.DeviceToken == currentToken
            }).ToList()
        };

        // Check TempData for messages
        if (TempData["DeviceMessage"] is string message)
            model.Message = message;
        if (TempData["DeviceError"] is string error)
            model.Error = error;

        return View("~/Features/Account/DeviceAuthorization/TrustedDevices.cshtml", model);
    }

    /// <summary>
    /// POST: Revoke a specific trusted device.
    /// </summary>
    [Authorize]
    [HttpPost]
    [Route(RevokeDeviceUrl)]
    [Route("{language}/" + RevokeDeviceUrl)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeDevice([FromForm] int trustedDeviceId)
    {
        var userResult = await userRepository.GetUserAsync(User.Identity?.Name ?? "");
        if (userResult.IsFailure)
            return RedirectToAction("LogIn", "LogIn");

        var success = await trustedDeviceService.RevokeDeviceAsync(userResult.Value.UserId, trustedDeviceId);
        TempData[success ? "DeviceMessage" : "DeviceError"] =
            success ? "Device removed successfully." : "Device not found.";

        return Redirect($"/{TrustedDevicesUrl}");
    }

    /// <summary>
    /// POST: Revoke all trusted devices for the current user.
    /// </summary>
    [Authorize]
    [HttpPost]
    [Route(RevokeAllDevicesUrl)]
    [Route("{language}/" + RevokeAllDevicesUrl)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeAllDevices()
    {
        var userResult = await userRepository.GetUserAsync(User.Identity?.Name ?? "");
        if (userResult.IsFailure)
            return RedirectToAction("LogIn", "LogIn");

        await trustedDeviceService.RevokeAllDevicesAsync(userResult.Value.UserId);
        TempData["DeviceMessage"] = "All devices have been signed out.";

        return Redirect($"/{TrustedDevicesUrl}");
    }

    #endregion
}
