using Microsoft.AspNetCore.Identity;

namespace Baseline.Account;

/// <summary>
/// Legacy interfaces for backward compatibility with v2 controllers.
/// These will be removed in a future version when controllers are updated.
/// </summary>

/// <summary>
/// Legacy: User settings repository interface.
/// </summary>
public interface IAccountSettingsRepository
{
    Task<AccountSettings> GetSettingsAsync();
    Task<bool> IsRegistrationEnabledAsync();
    Task<bool> IsEmailConfirmationRequiredAsync();
    Task<string> GetAccountLoginUrlAsync(string defaultUrl);
    Task<string> GetAccountRegistrationUrlAsync(string defaultUrl);
    Task<string> GetAccountForgotPasswordUrlAsync(string defaultUrl);
    Task<string> GetAccountForgottenPasswordResetUrlAsync(string defaultUrl);
    Task<string> GetAccountResetPasswordUrlAsync(string defaultUrl);
    Task<string> GetAccountConfirmationUrlAsync(string defaultUrl);
    Task<string> GetAccountMyAccountUrlAsync(string defaultUrl);
    Task<string> GetAccountTwoFormAuthenticationUrlAsync(string defaultUrl);
}

/// <summary>
/// Account settings model.
/// </summary>
public class AccountSettings
{
    public bool RegistrationEnabled { get; set; } = true;
    public bool EmailConfirmationRequired { get; set; } = true;
    public bool PasswordRecoveryEnabled { get; set; } = true;
    public string? LoginPageUrl { get; set; }
    public string? RegistrationPageUrl { get; set; }
    public string? ProfilePageUrl { get; set; }
    public string? ForgotPasswordPageUrl { get; set; }
    public string? ResetPasswordPageUrl { get; set; }
    public string? ConfirmationPageUrl { get; set; }
    public string? AfterLoginRedirectUrl { get; set; }
    public string? AfterRegistrationRedirectUrl { get; set; }
}

/// <summary>
/// Legacy: User service interface.
/// </summary>
public interface IUserService
{
    Task<IUser?> GetUserAsync(int userId);
    Task<IUser?> GetUserByEmailAsync(string email);
    Task<IUser?> GetUserByUsernameAsync(string username);
    Task<IUser?> CreateUserAsync(CreateUserRequest request);
    Task<bool> ValidatePasswordAsync(string password);
    Task<CSharpFunctionalExtensions.Result<IUser>> CreateUser(IUser user, string password, bool enabled = true);
    Task<bool> HasPasswordAsync(string email);
    Task<CSharpFunctionalExtensions.Result<IUser>> UpgradeGuestUserAsync(string email, string userName, string password);
    Task SendRegistrationConfirmationEmailAsync(IUser user, string confirmationUrl);
    Task SendVerificationCodeEmailAsync(IUser user, string token);
    Task SendPasswordResetEmailAsync(IUser user, string resetUrl);
    Task<bool> ResetPasswordAsync(IUser user, string newPassword, string currentPassword);
    Task<IdentityResult> ResetPasswordFromTokenAsync(IUser user, string token, string newPassword);
}

/// <summary>
/// Legacy: User creation request.
/// </summary>
public class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

/// <summary>
/// Legacy: User repository interface.
/// </summary>
public interface IUserRepository
{
    Task<IUser?> GetByIdAsync(int id);
    Task<IUser?> GetByEmailAsync(string email);
    Task<IUser?> GetByUsernameAsync(string username);
    Task<IEnumerable<IUser>> GetAllAsync();
    Task UpdateAsync(IUser user);
    Task<CSharpFunctionalExtensions.Result<IUser>> GetUserAsync(string username);
    Task<CSharpFunctionalExtensions.Result<IUser>> GetUserAsync(Guid userGuid);
    Task<CSharpFunctionalExtensions.Result<IUser>> GetUserByEmailAsync(string email);
}

/// <summary>
/// Legacy: URL resolver interface.
/// </summary>
public interface IUrlResolver
{
    string ResolveUrl(string path);
    string GetAbsoluteUrl(string relativePath);
    string GetCurrentUrl();
}

/// <summary>
/// Legacy: Model state service for controller validation.
/// </summary>
public interface IModelStateService
{
    bool IsValid { get; }
    void AddError(string key, string message);
    void MergeErrors(IDictionary<string, string[]> errors);
    void StoreViewModel<T>(Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary tempData, T viewModel);
    T? RetrieveViewModel<T>(Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary tempData) where T : class;
    void ClearViewModel(Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary tempData);
}

/// <summary>
/// Legacy: Sign-in manager service.
/// </summary>
public interface ISignInManagerService
{
    Task<SignInResult> PasswordSignInAsync(string username, string password, bool rememberMe, bool lockoutOnFailure);
    Task SignInAsync(IUser user, bool isPersistent);
    Task SignOutAsync();
    Task<bool> IsLockedOutAsync(IUser user);
    Task<IUser?> GetTwoFactorAuthenticationUserAsync();
    Task<SignInResult> TwoFactorSignInAsync(string provider, string code, bool rememberMe, bool rememberBrowser);
    Task<bool> IsTwoFactorClientRememberedByNameAsync(string username);
    Task SignInByNameAsync(string username, bool isPersistent);
    Task<SignInResult> PasswordSignInByNameAsync(string username, string password, bool isPersistent, bool lockoutOnFailure);
    Task RememberTwoFactorClientByNameAsync(string username);
}

/// <summary>
/// Legacy: Sign-in result.
/// </summary>
public class SignInResult
{
    public bool Succeeded { get; set; }
    public bool IsLockedOut { get; set; }
    public bool IsNotAllowed { get; set; }
    public bool RequiresTwoFactor { get; set; }

    public static SignInResult Success => new() { Succeeded = true };
    public static SignInResult Failed => new() { Succeeded = false };
    public static SignInResult LockedOut => new() { IsLockedOut = true };
    public static SignInResult NotAllowed => new() { IsNotAllowed = true };
    public static SignInResult TwoFactorRequired => new() { RequiresTwoFactor = true };
}

/// <summary>
/// Legacy: User manager service.
/// </summary>
public interface IUserManagerService
{
    Task<IUser?> FindByIdAsync(string userId);
    Task<IUser?> FindByEmailAsync(string email);
    Task<IUser?> FindByNameAsync(string userName);
    Task<IdentityResult> CreateAsync(IUser user, string password);
    Task<IdentityResult> AddToRoleAsync(IUser user, string role);
    Task<IdentityResult> RemoveFromRoleAsync(IUser user, string role);
    Task<bool> IsInRoleAsync(IUser user, string role);
    Task<IList<string>> GetRolesAsync(IUser user);
    Task<string> GenerateEmailConfirmationTokenAsync(IUser user);
    Task<IdentityResult> ConfirmEmailAsync(IUser user, string token);
    Task<string> GeneratePasswordResetTokenAsync(IUser user);
    Task<IdentityResult> ResetPasswordAsync(IUser user, string token, string newPassword);
    Task<IdentityResult> ChangePasswordAsync(IUser user, string currentPassword, string newPassword);
    Task<IdentityResult> UpdateAsync(IUser user);
    Task<IdentityResult> EnableUserByIdAsync(int userId);
    Task<bool> CheckPasswordByNameAsync(string username, string password);
    Task<string> GenerateTwoFactorTokenByNameAsync(string username, string provider);
    Task<bool> VerifyTwoFactorTokenByNameAsync(string username, string provider, string token);
    Task<string> GetSecurityStampAsync(string username);
}

/// <summary>
/// Legacy: Identity operation result.
/// </summary>
public class IdentityResult
{
    public bool Succeeded { get; set; }
    public IEnumerable<IdentityError> Errors { get; set; } = [];

    public static IdentityResult Success => new() { Succeeded = true, Errors = [] };
    public static IdentityResult Failed(params IdentityError[] errors) => new() { Succeeded = false, Errors = errors };
}

/// <summary>
/// Legacy: Identity error.
/// </summary>
public class IdentityError
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Legacy: Authentication configurations.
/// </summary>
public interface IAuthenticationConfigurations
{
    bool AllowPasswordSignIn { get; }
    bool RequireConfirmedEmail { get; }
    bool RequireConfirmedAccount { get; }
    int LockoutMaxFailedAttempts { get; }
    TimeSpan LockoutDuration { get; }
    IEnumerable<string> ExternalAuthenticationProviders { get; }
    bool UseTwoFormAuthentication();
}

/// <summary>
/// Legacy: User interface for backward compatibility.
/// </summary>
public interface IUser
{
    int UserId { get; }
    Guid UserGuid { get; }
    string UserName { get; }
    string Email { get; }
    string FirstName { get; }
    string LastName { get; }
    bool Enabled { get; }
    bool IsExternal { get; }
}
