using Fido2NetLib;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Baseline.Account.RCL.Features.Passkey;

/// <summary>
/// API controller for WebAuthn/Passkey biometric authentication.
/// </summary>
[TypeFilter(typeof(LanguagePrefixRouteFilter))]
[Route("api/account/passkeys")]
[ApiController]
public sealed class PasskeyController(
    IPasskeyService passkeyService,
    ILogger<PasskeyController> logger,
    IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    // Session keys for storing WebAuthn options
    private const string RegistrationOptionsKey = "fido2.attestationOptions";
    private const string AuthenticationOptionsKey = "fido2.assertionOptions";

    /// <summary>
    /// Gets all passkey credentials for the current user.
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetCredentials()
    {
        var memberId = GetCurrentMemberId();
        if (memberId == null)
            return Unauthorized();

        var credentials = await passkeyService.GetCredentialsAsync(memberId.Value);
        return Ok(credentials);
    }

    /// <summary>
    /// Gets WebAuthn registration options for creating a new passkey.
    /// </summary>
    [HttpPost("register/options")]
    [Authorize]
    public async Task<IActionResult> GetRegistrationOptions([FromBody] PasskeyRegistrationRequest request)
    {
        var memberId = GetCurrentMemberId();
        if (memberId == null)
            return Unauthorized();

        try
        {
            var deviceName = string.IsNullOrWhiteSpace(request.DeviceName)
                ? GetDefaultDeviceName()
                : request.DeviceName;

            var options = await passkeyService.GetRegistrationOptionsAsync(memberId.Value, deviceName);

            // Store options in session for verification
            HttpContext.Session.SetString(RegistrationOptionsKey, options.ToJson());

            return Ok(options);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate passkey registration options");
            return BadRequest(new { error = "Failed to generate registration options" });
        }
    }

    /// <summary>
    /// Completes passkey registration.
    /// </summary>
    [HttpPost("register")]
    [Authorize]
    public async Task<IActionResult> CompleteRegistration([FromBody] PasskeyRegistrationCompleteRequest request)
    {
        var memberId = GetCurrentMemberId();
        if (memberId == null)
            return Unauthorized();

        try
        {
            // Retrieve stored options from session
            var optionsJson = HttpContext.Session.GetString(RegistrationOptionsKey);
            if (string.IsNullOrEmpty(optionsJson))
                return BadRequest(new { error = "Registration session expired. Please try again." });

            var options = CredentialCreateOptions.FromJson(optionsJson);

            var deviceName = string.IsNullOrWhiteSpace(request.DeviceName)
                ? GetDefaultDeviceName()
                : request.DeviceName;

            var result = await passkeyService.CompleteRegistrationAsync(
                memberId.Value,
                deviceName,
                request.AttestationResponse,
                options);

            // Clear session
            HttpContext.Session.Remove(RegistrationOptionsKey);

            if (!result.Success)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(new { credentialId = result.CredentialId });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to complete passkey registration");
            return BadRequest(new { error = "Registration failed" });
        }
    }

    /// <summary>
    /// Gets WebAuthn authentication options for logging in with a passkey.
    /// </summary>
    [HttpPost("authenticate/options")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAuthenticationOptions([FromBody] PasskeyAuthenticationRequest? request)
    {
        try
        {
            var options = await passkeyService.GetAuthenticationOptionsAsync(request?.Username);

            // Store options in session for verification
            HttpContext.Session.SetString(AuthenticationOptionsKey, options.ToJson());

            return Ok(options);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate passkey authentication options");
            return BadRequest(new { error = "Failed to generate authentication options" });
        }
    }

    /// <summary>
    /// Completes passkey authentication and signs the user in.
    /// </summary>
    [HttpPost("authenticate")]
    [AllowAnonymous]
    public async Task<IActionResult> CompleteAuthentication([FromBody] AuthenticatorAssertionRawResponse assertionResponse)
    {
        try
        {
            // Retrieve stored options from session
            var optionsJson = HttpContext.Session.GetString(AuthenticationOptionsKey);
            if (string.IsNullOrEmpty(optionsJson))
                return BadRequest(new { error = "Authentication session expired. Please try again." });

            var options = AssertionOptions.FromJson(optionsJson);

            var result = await passkeyService.CompleteAuthenticationAsync(assertionResponse, options);

            // Clear session
            HttpContext.Session.Remove(AuthenticationOptionsKey);

            if (!result.Success)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(new
            {
                memberId = result.MemberId,
                username = result.Username
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to complete passkey authentication");
            return BadRequest(new { error = "Authentication failed" });
        }
    }

    /// <summary>
    /// Deletes a passkey credential.
    /// </summary>
    [HttpDelete("{credentialId:int}")]
    [Authorize]
    public async Task<IActionResult> DeleteCredential(int credentialId)
    {
        var memberId = GetCurrentMemberId();
        if (memberId == null)
            return Unauthorized();

        var result = await passkeyService.DeleteCredentialAsync(memberId.Value, credentialId);

        if (!result.Success)
            return result.ErrorMessage == "Passkey not found" ? NotFound() : BadRequest(new { error = result.ErrorMessage });

        return NoContent();
    }

    /// <summary>
    /// Updates the name of a passkey credential.
    /// </summary>
    [HttpPatch("{credentialId:int}")]
    [Authorize]
    public async Task<IActionResult> UpdateCredentialName(int credentialId, [FromBody] PasskeyUpdateNameRequest request)
    {
        var memberId = GetCurrentMemberId();
        if (memberId == null)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.DeviceName))
            return BadRequest(new { error = "Device name is required" });

        var success = await passkeyService.UpdateDeviceNameAsync(memberId.Value, credentialId, request.DeviceName);

        return success ? Ok() : NotFound();
    }

    private int? GetCurrentMemberId()
    {
        var memberIdClaim = User.FindFirst("MemberID")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        return int.TryParse(memberIdClaim, out var memberId) ? memberId : null;
    }

    private string GetDefaultDeviceName()
    {
        var userAgent = httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString() ?? "";
        var ua = UAParser.Parser.GetDefault().Parse(userAgent);
        return $"{ua.UA.Family ?? "Unknown"} on {ua.OS.Family ?? "Unknown"}";
    }
}

/// <summary>
/// Request model for passkey registration.
/// </summary>
public record PasskeyRegistrationRequest
{
    public string? DeviceName { get; init; }
}

/// <summary>
/// Request model for completing passkey registration.
/// </summary>
public record PasskeyRegistrationCompleteRequest
{
    public string? DeviceName { get; init; }
    public required AuthenticatorAttestationRawResponse AttestationResponse { get; init; }
}

/// <summary>
/// Request model for passkey authentication.
/// </summary>
public record PasskeyAuthenticationRequest
{
    public string? Username { get; init; }
}

/// <summary>
/// Request model for updating passkey name.
/// </summary>
public record PasskeyUpdateNameRequest
{
    public required string DeviceName { get; init; }
}
