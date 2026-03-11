namespace Baseline.Account;

/// <summary>
/// Represents a user in the Baseline system.
/// </summary>
public class BaselineUser
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string FullName => string.IsNullOrWhiteSpace(FirstName)
        ? UserName
        : $"{FirstName} {LastName}".Trim();
    public bool EmailConfirmed { get; set; }
    public bool IsEnabled { get; set; }
    public DateTimeOffset? LastLoginDate { get; set; }
    public DateTimeOffset CreatedDate { get; set; }
    public IEnumerable<string> Roles { get; set; } = [];
}

/// <summary>
/// Result of an authentication attempt.
/// </summary>
public record AuthenticationResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public AuthErrorCode? ErrorCode { get; init; }
    public BaselineUser? User { get; init; }
    public bool RequiresTwoFactor { get; init; }
    public bool IsLockedOut { get; init; }
    public bool IsNotAllowed { get; init; }

    public static AuthenticationResult Succeeded(BaselineUser user) => new() { Success = true, User = user };
    public static AuthenticationResult Failed(string message, AuthErrorCode code) =>
        new() { Success = false, ErrorMessage = message, ErrorCode = code };
}

/// <summary>
/// Authentication error codes.
/// </summary>
public enum AuthErrorCode
{
    InvalidCredentials,
    AccountLocked,
    AccountDisabled,
    EmailNotConfirmed,
    TwoFactorRequired,
    Unknown
}

/// <summary>
/// Request to register a new user.
/// </summary>
public record RegistrationRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public string? UserName { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public bool AcceptTerms { get; init; }
}

/// <summary>
/// Result of a registration attempt.
/// </summary>
public record RegistrationResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public IEnumerable<string> Errors { get; init; } = [];
    public BaselineUser? User { get; init; }
    public bool RequiresEmailConfirmation { get; init; }

    public static RegistrationResult Succeeded(BaselineUser user, bool requiresConfirmation = false) =>
        new() { Success = true, User = user, RequiresEmailConfirmation = requiresConfirmation };
    public static RegistrationResult Failed(IEnumerable<string> errors) =>
        new() { Success = false, Errors = errors };
}

/// <summary>
/// Result of password recovery initiation.
/// </summary>
public record PasswordRecoveryResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }

    public static PasswordRecoveryResult Succeeded() => new() { Success = true };
    public static PasswordRecoveryResult Failed(string message) =>
        new() { Success = false, ErrorMessage = message };
}

/// <summary>
/// Result of a password reset attempt.
/// </summary>
public record PasswordResetResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public IEnumerable<string> Errors { get; init; } = [];

    public static PasswordResetResult Succeeded() => new() { Success = true };
    public static PasswordResetResult Failed(IEnumerable<string> errors) =>
        new() { Success = false, Errors = errors };
}

/// <summary>
/// Result of a password change attempt.
/// </summary>
public record PasswordChangeResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public IEnumerable<string> Errors { get; init; } = [];

    public static PasswordChangeResult Succeeded() => new() { Success = true };
    public static PasswordChangeResult Failed(IEnumerable<string> errors) =>
        new() { Success = false, Errors = errors };
}

/// <summary>
/// Result of password validation.
/// </summary>
public record PasswordValidationResult
{
    public bool IsValid { get; init; }
    public IEnumerable<string> Errors { get; init; } = [];

    public static PasswordValidationResult Valid() => new() { IsValid = true };
    public static PasswordValidationResult Invalid(IEnumerable<string> errors) =>
        new() { IsValid = false, Errors = errors };
}

/// <summary>
/// Represents an external authentication provider.
/// </summary>
public record ExternalProvider(string Name, string DisplayName, string? IconClass = null);

/// <summary>
/// Result of an authentication challenge.
/// Contains the scheme and properties needed to issue an ASP.NET Core ChallengeResult.
/// </summary>
public record ChallengeResult(string Scheme, Microsoft.AspNetCore.Authentication.AuthenticationProperties Properties);

/// <summary>
/// Result of external authentication.
/// </summary>
public record ExternalAuthResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public BaselineUser? User { get; init; }
    public bool IsNewUser { get; init; }
    public ExternalLoginInfo? LoginInfo { get; init; }

    public static ExternalAuthResult Succeeded(BaselineUser user, bool isNew = false) =>
        new() { Success = true, User = user, IsNewUser = isNew };
    public static ExternalAuthResult Failed(string message) =>
        new() { Success = false, ErrorMessage = message };
}

/// <summary>
/// External login information.
/// </summary>
public record ExternalLoginInfo(string Provider, string ProviderKey, string? Email, string? Name);

/// <summary>
/// Result of linking an external login.
/// </summary>
public record LinkResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }

    public static LinkResult Succeeded() => new() { Success = true };
    public static LinkResult Failed(string message) => new() { Success = false, ErrorMessage = message };
}

/// <summary>
/// Result of unlinking an external login.
/// </summary>
public record UnlinkResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }

    public static UnlinkResult Succeeded() => new() { Success = true };
    public static UnlinkResult Failed(string message) => new() { Success = false, ErrorMessage = message };
}

/// <summary>
/// User profile information.
/// </summary>
public record UserProfile
{
    public int UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? PhoneNumber { get; init; }
    public string? ProfilePictureUrl { get; init; }
    public string? Bio { get; init; }
    public string? TimeZone { get; init; }
    public string? PreferredLanguage { get; init; }
    public bool TwoFactorEnabled { get; init; }
    public IEnumerable<ExternalLoginInfo> ExternalLogins { get; init; } = [];

    /// <summary>
    /// Gets the display name for the user (FirstName LastName, or UserName if no name set).
    /// </summary>
    public string DisplayName => !string.IsNullOrWhiteSpace(FirstName) || !string.IsNullOrWhiteSpace(LastName)
        ? $"{FirstName} {LastName}".Trim()
        : UserName;
}

/// <summary>
/// Request to update a user profile.
/// </summary>
public record ProfileUpdateRequest
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Bio { get; init; }
    public string? TimeZone { get; init; }
    public string? PreferredLanguage { get; init; }
}

/// <summary>
/// Result of a profile update.
/// </summary>
public record ProfileUpdateResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public UserProfile? Profile { get; init; }

    public static ProfileUpdateResult Succeeded(UserProfile profile) =>
        new() { Success = true, Profile = profile };
    public static ProfileUpdateResult Failed(string message) =>
        new() { Success = false, ErrorMessage = message };
}

/// <summary>
/// Result of a profile picture upload.
/// </summary>
public record ProfilePictureResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? PictureUrl { get; init; }

    public static ProfilePictureResult Succeeded(string url) =>
        new() { Success = true, PictureUrl = url };
    public static ProfilePictureResult Failed(string message) =>
        new() { Success = false, ErrorMessage = message };
}

/// <summary>
/// Result of account deletion.
/// </summary>
public record AccountDeletionResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }

    public static AccountDeletionResult Succeeded() => new() { Success = true };
    public static AccountDeletionResult Failed(string message) =>
        new() { Success = false, ErrorMessage = message };
}
