using Fido2NetLib;
using Fido2NetLib.Objects;

namespace Baseline.Account;

/// <summary>
/// Service for managing WebAuthn/Passkey credentials for biometric authentication (fingerprint/Face ID).
/// </summary>
public interface IPasskeyService
{
    /// <summary>
    /// Gets all passkey credentials for a user.
    /// </summary>
    Task<IReadOnlyList<PasskeyCredentialViewModel>> GetCredentialsAsync(int memberId);

    /// <summary>
    /// Generates WebAuthn registration options for creating a new passkey.
    /// </summary>
    Task<CredentialCreateOptions> GetRegistrationOptionsAsync(int memberId, string deviceName);

    /// <summary>
    /// Completes passkey registration by storing the credential.
    /// </summary>
    Task<PasskeyRegistrationResult> CompleteRegistrationAsync(
        int memberId,
        string deviceName,
        AuthenticatorAttestationRawResponse attestationResponse,
        CredentialCreateOptions originalOptions);

    /// <summary>
    /// Generates WebAuthn assertion options for authenticating with a passkey.
    /// </summary>
    Task<AssertionOptions> GetAuthenticationOptionsAsync(string? username = null);

    /// <summary>
    /// Completes passkey authentication and returns the authenticated member ID.
    /// </summary>
    Task<PasskeyAuthenticationResult> CompleteAuthenticationAsync(
        AuthenticatorAssertionRawResponse assertionResponse,
        AssertionOptions originalOptions);

    /// <summary>
    /// Deletes a passkey credential.
    /// </summary>
    Task<PasskeyDeleteResult> DeleteCredentialAsync(int memberId, int credentialId);

    /// <summary>
    /// Updates the device name for a passkey credential.
    /// </summary>
    Task<bool> UpdateDeviceNameAsync(int memberId, int credentialId, string newName);
}

/// <summary>
/// View model for displaying passkey credentials.
/// </summary>
public record PasskeyCredentialViewModel
{
    /// <summary>Database credential ID.</summary>
    public int CredentialId { get; init; }

    /// <summary>User-provided device/passkey name.</summary>
    public required string DeviceName { get; init; }

    /// <summary>When the passkey was registered.</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>When the passkey was last used for authentication.</summary>
    public DateTime? LastUsedAt { get; init; }

    /// <summary>Authenticator attachment type (platform = built-in, cross-platform = security key).</summary>
    public string? AttachmentType { get; init; }

    /// <summary>Whether this passkey is synced/backed up across devices.</summary>
    public bool IsBackedUp { get; init; }

    /// <summary>Whether this passkey is currently active.</summary>
    public bool IsActive { get; init; }
}

/// <summary>
/// Result of passkey registration.
/// </summary>
public record PasskeyRegistrationResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public int? CredentialId { get; init; }

    public static PasskeyRegistrationResult Succeeded(int credentialId) =>
        new() { Success = true, CredentialId = credentialId };

    public static PasskeyRegistrationResult Failed(string message) =>
        new() { Success = false, ErrorMessage = message };
}

/// <summary>
/// Result of passkey authentication.
/// </summary>
public record PasskeyAuthenticationResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public int? MemberId { get; init; }
    public string? Username { get; init; }

    public static PasskeyAuthenticationResult Succeeded(int memberId, string username) =>
        new() { Success = true, MemberId = memberId, Username = username };

    public static PasskeyAuthenticationResult Failed(string message) =>
        new() { Success = false, ErrorMessage = message };
}

/// <summary>
/// Result of passkey deletion.
/// </summary>
public record PasskeyDeleteResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }

    public static PasskeyDeleteResult Succeeded => new() { Success = true };
    public static PasskeyDeleteResult NotFound => new() { Success = false, ErrorMessage = "Passkey not found" };
    public static PasskeyDeleteResult Failed(string message) => new() { Success = false, ErrorMessage = message };
}
