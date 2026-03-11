using System.Security.Claims;

namespace Baseline.Account;

/// <summary>
/// Service for managing user authentication and identity operations.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Signs in a user with username and password.
    /// </summary>
    Task<AuthenticationResult> SignInAsync(string username, string password, bool rememberMe = false);

    /// <summary>
    /// Signs out the current user.
    /// </summary>
    Task SignOutAsync();

    /// <summary>
    /// Registers a new user.
    /// </summary>
    Task<RegistrationResult> RegisterAsync(RegistrationRequest request);

    /// <summary>
    /// Gets the current authenticated user.
    /// </summary>
    Task<BaselineUser?> GetCurrentUserAsync();

    /// <summary>
    /// Gets a user by their ID.
    /// </summary>
    Task<BaselineUser?> GetUserByIdAsync(int userId);

    /// <summary>
    /// Gets a user by email.
    /// </summary>
    Task<BaselineUser?> GetUserByEmailAsync(string email);

    /// <summary>
    /// Checks if the current user is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets the current user's claims principal.
    /// </summary>
    ClaimsPrincipal? CurrentPrincipal { get; }
}

/// <summary>
/// Service for password management operations.
/// </summary>
public interface IPasswordService
{
    /// <summary>
    /// Initiates password recovery for a user.
    /// </summary>
    Task<PasswordRecoveryResult> InitiateRecoveryAsync(string email);

    /// <summary>
    /// Resets a user's password using a recovery token.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="token">The password reset token.</param>
    /// <param name="newPassword">The new password.</param>
    Task<PasswordResetResult> ResetPasswordAsync(int userId, string token, string newPassword);

    /// <summary>
    /// Changes the current user's password.
    /// </summary>
    Task<PasswordChangeResult> ChangePasswordAsync(string currentPassword, string newPassword);

    /// <summary>
    /// Validates a password against the configured policy.
    /// </summary>
    PasswordValidationResult ValidatePassword(string password);
}

/// <summary>
/// Service for external authentication provider operations.
/// </summary>
public interface IExternalAuthenticationService
{
    /// <summary>
    /// Gets available external authentication providers.
    /// </summary>
    IEnumerable<ExternalProvider> GetProviders();

    /// <summary>
    /// Initiates external authentication challenge.
    /// </summary>
    Task<ChallengeResult> ChallengeAsync(string provider, string returnUrl);

    /// <summary>
    /// Handles the external authentication callback.
    /// </summary>
    Task<ExternalAuthResult> HandleCallbackAsync();

    /// <summary>
    /// Links an external login to an existing account.
    /// </summary>
    Task<LinkResult> LinkExternalLoginAsync(int userId, string provider, string providerKey);

    /// <summary>
    /// Unlinks an external login from an account.
    /// </summary>
    Task<UnlinkResult> UnlinkExternalLoginAsync(int userId, string provider);
}

/// <summary>
/// Service for managing user profiles.
/// </summary>
public interface IProfileService
{
    /// <summary>
    /// Gets the profile for the current user.
    /// </summary>
    Task<UserProfile?> GetProfileAsync();

    /// <summary>
    /// Gets a user's profile by ID.
    /// </summary>
    Task<UserProfile?> GetProfileByIdAsync(int userId);

    /// <summary>
    /// Updates the current user's profile.
    /// </summary>
    Task<ProfileUpdateResult> UpdateProfileAsync(ProfileUpdateRequest request);

    /// <summary>
    /// Uploads a profile picture.
    /// </summary>
    Task<ProfilePictureResult> UploadProfilePictureAsync(Stream imageStream, string fileName);

    /// <summary>
    /// Deletes the current user's account.
    /// </summary>
    Task<AccountDeletionResult> DeleteAccountAsync(string confirmPassword);
}

/// <summary>
/// Service for email confirmation during registration.
/// Aligns with Kentico's SignIn.RequireConfirmedAccount option.
/// </summary>
public interface IEmailConfirmationService
{
    /// <summary>
    /// Sends an email confirmation to the user.
    /// </summary>
    Task<SendConfirmationResult> SendConfirmationEmailAsync(int userId);

    /// <summary>
    /// Confirms the user's email using a token.
    /// </summary>
    Task<EmailConfirmationResult> ConfirmEmailAsync(int userId, string token);

    /// <summary>
    /// Checks if the user's email is confirmed.
    /// </summary>
    Task<bool> IsEmailConfirmedAsync(int userId);

    /// <summary>
    /// Resends the confirmation email.
    /// </summary>
    Task<SendConfirmationResult> ResendConfirmationEmailAsync(string email);
}

/// <summary>
/// Result of sending a confirmation email.
/// </summary>
public class SendConfirmationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Result of confirming an email address.
/// </summary>
public class EmailConfirmationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
