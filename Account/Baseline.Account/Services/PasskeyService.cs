using System.Text;
using CMS.Core;
using CMS.DataEngine;
using CMS.Membership;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.Extensions.Logging;

namespace Baseline.Account;

/// <summary>
/// Service for managing WebAuthn/Passkey credentials for biometric authentication (fingerprint/Face ID).
/// </summary>
public class PasskeyService(
    IFido2 fido2,
    IInfoProvider<MemberInfo> memberProvider,
    ILogger<PasskeyService> logger) : IPasskeyService
{
    private IInfoProvider<PasskeyCredentialInfo>? _provider;
    private IInfoProvider<PasskeyCredentialInfo> Provider =>
        _provider ??= Service.Resolve<IInfoProvider<PasskeyCredentialInfo>>();

    private bool IsTableAvailable()
    {
        try
        {
            return DataClassInfoProvider.GetDataClassInfo(PasskeyCredentialInfo.OBJECT_TYPE) != null;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PasskeyCredentialViewModel>> GetCredentialsAsync(int memberId)
    {
        if (!IsTableAvailable())
            return [];

        var credentials = await Provider.Get()
            .WhereEquals(nameof(PasskeyCredentialInfo.MemberID), memberId)
            .WhereEquals(nameof(PasskeyCredentialInfo.IsActive), true)
            .GetEnumerableTypedResultAsync();

        return credentials.Select(c => new PasskeyCredentialViewModel
        {
            CredentialId = c.PasskeyCredentialID,
            DeviceName = c.DeviceName,
            CreatedAt = c.CreatedAt,
            LastUsedAt = c.LastUsedAt,
            AttachmentType = c.AttachmentType,
            IsBackedUp = c.IsBackedUp,
            IsActive = c.IsActive
        }).ToList();
    }

    /// <inheritdoc/>
    public async Task<CredentialCreateOptions> GetRegistrationOptionsAsync(int memberId, string deviceName)
    {
        var member = await memberProvider.GetAsync(memberId);
        if (member == null)
            throw new InvalidOperationException($"Member {memberId} not found");

        // Get existing credentials to exclude
        var existingCredentials = await GetExistingCredentialDescriptors(memberId);

        var user = new Fido2User
        {
            Id = Encoding.UTF8.GetBytes(memberId.ToString()),
            Name = member.MemberEmail,
            DisplayName = member.MemberName ?? member.MemberEmail
        };

        var authenticatorSelection = new AuthenticatorSelection
        {
            // Prefer platform authenticator (built-in biometrics)
            AuthenticatorAttachment = AuthenticatorAttachment.Platform,
            UserVerification = UserVerificationRequirement.Required,
            ResidentKey = ResidentKeyRequirement.Required
        };

        var options = fido2.RequestNewCredential(
            new RequestNewCredentialParams
            {
                User = user,
                ExcludeCredentials = existingCredentials,
                AuthenticatorSelection = authenticatorSelection,
                AttestationPreference = AttestationConveyancePreference.None,
                Extensions = new AuthenticationExtensionsClientInputs
                {
                    CredProps = true
                }
            });

        logger.LogInformation(
            "Generated passkey registration options for member {MemberId}, device: {DeviceName}",
            memberId, deviceName);

        return options;
    }

    /// <inheritdoc/>
    public async Task<PasskeyRegistrationResult> CompleteRegistrationAsync(
        int memberId,
        string deviceName,
        AuthenticatorAttestationRawResponse attestationResponse,
        CredentialCreateOptions originalOptions)
    {
        if (!IsTableAvailable())
            return PasskeyRegistrationResult.Failed("Passkey storage not available");

        try
        {
            var credential = await fido2.MakeNewCredentialAsync(
                new MakeNewCredentialParams
                {
                    AttestationResponse = attestationResponse,
                    OriginalOptions = originalOptions,
                    IsCredentialIdUniqueToUserCallback = async (args, ct) =>
                    {
                        // Check if this credential ID is already registered for any user
                        if (!IsTableAvailable())
                            return true;

                        var existing = await Provider.Get()
                            .WhereEquals(nameof(PasskeyCredentialInfo.CredentialId), Convert.ToBase64String(args.CredentialId))
                            .GetEnumerableTypedResultAsync();

                        return !existing.Any();
                    }
                });

            if (credential == null)
                return PasskeyRegistrationResult.Failed("Failed to create credential");

            // Store the credential
            var passkeyInfo = new PasskeyCredentialInfo
            {
                MemberID = memberId,
                CredentialId = Convert.ToBase64String(credential.Id),
                PublicKey = Convert.ToBase64String(credential.PublicKey),
                UserHandle = Convert.ToBase64String(credential.User.Id),
                SignatureCounter = (int)credential.SignCount,
                Aaguid = credential.AaGuid.ToString(),
                CredentialType = credential.Type.ToString(),
                DeviceName = deviceName.Length > 200 ? deviceName[..200] : deviceName,
                AttachmentType = "platform", // Default to platform for biometric
                Transports = credential.Transports != null ? string.Join(",", credential.Transports) : null,
                IsBackedUp = false, // Will be updated on subsequent authentications if needed
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            Provider.Set(passkeyInfo);

            logger.LogInformation(
                "Passkey registered for member {MemberId}: {DeviceName} (ID: {CredentialId})",
                memberId, deviceName, passkeyInfo.PasskeyCredentialID);

            return PasskeyRegistrationResult.Succeeded(passkeyInfo.PasskeyCredentialID);
        }
        catch (Fido2VerificationException ex)
        {
            logger.LogWarning(ex, "Passkey registration verification failed for member {MemberId}", memberId);
            return PasskeyRegistrationResult.Failed($"Verification failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Passkey registration failed for member {MemberId}", memberId);
            return PasskeyRegistrationResult.Failed("Registration failed");
        }
    }

    /// <inheritdoc/>
    public async Task<AssertionOptions> GetAuthenticationOptionsAsync(string? username = null)
    {
        var allowedCredentials = new List<PublicKeyCredentialDescriptor>();

        if (!string.IsNullOrEmpty(username) && IsTableAvailable())
        {
            // Get member ID from username/email
            var member = await memberProvider.Get()
                .WhereEquals(nameof(MemberInfo.MemberEmail), username)
                .Or()
                .WhereEquals(nameof(MemberInfo.MemberName), username)
                .GetEnumerableTypedResultAsync();

            var memberInfo = member.FirstOrDefault();
            if (memberInfo != null)
            {
                allowedCredentials = (await GetExistingCredentialDescriptors(memberInfo.MemberID)).ToList();
            }
        }

        var options = fido2.GetAssertionOptions(
            new GetAssertionOptionsParams
            {
                AllowedCredentials = allowedCredentials,
                UserVerification = UserVerificationRequirement.Required
            });

        logger.LogDebug("Generated passkey authentication options for user: {Username}", username ?? "any");

        return options;
    }

    /// <inheritdoc/>
    public async Task<PasskeyAuthenticationResult> CompleteAuthenticationAsync(
        AuthenticatorAssertionRawResponse assertionResponse,
        AssertionOptions originalOptions)
    {
        if (!IsTableAvailable())
            return PasskeyAuthenticationResult.Failed("Passkey storage not available");

        try
        {
            // In Fido2 v4, Id is already base64-encoded string
            var credentialIdBase64 = assertionResponse.Id;

            // Find the stored credential
            var credential = (await Provider.Get()
                .WhereEquals(nameof(PasskeyCredentialInfo.CredentialId), credentialIdBase64)
                .WhereEquals(nameof(PasskeyCredentialInfo.IsActive), true)
                .GetEnumerableTypedResultAsync())
                .FirstOrDefault();

            if (credential == null)
                return PasskeyAuthenticationResult.Failed("Credential not found");

            // Get member info
            var member = await memberProvider.GetAsync(credential.MemberID);
            if (member == null)
                return PasskeyAuthenticationResult.Failed("Member not found");

            var storedPublicKey = Convert.FromBase64String(credential.PublicKey);
            uint storedSignCount = (uint)credential.SignatureCounter;

            var result = await fido2.MakeAssertionAsync(
                new MakeAssertionParams
                {
                    AssertionResponse = assertionResponse,
                    OriginalOptions = originalOptions,
                    StoredPublicKey = storedPublicKey,
                    StoredSignatureCounter = storedSignCount,
                    IsUserHandleOwnerOfCredentialIdCallback = async (args, ct) =>
                    {
                        // Verify the user handle matches the credential owner
                        if (!IsTableAvailable())
                            return true;

                        var credIdBase64 = Convert.ToBase64String(args.CredentialId);
                        var cred = (await Provider.Get()
                            .WhereEquals(nameof(PasskeyCredentialInfo.CredentialId), credIdBase64)
                            .GetEnumerableTypedResultAsync())
                            .FirstOrDefault();

                        if (cred == null)
                            return false;

                        var storedUserHandle = Convert.FromBase64String(cred.UserHandle);
                        return storedUserHandle.SequenceEqual(args.UserHandle);
                    }
                });

            // MakeAssertionAsync throws Fido2VerificationException on failure
            // If we reach here, authentication succeeded

            // Update signature counter and last used
            credential.SignatureCounter = (int)result.SignCount;
            credential.LastUsedAt = DateTime.UtcNow;
            credential.LastModified = DateTime.UtcNow;
            Provider.Set(credential);

            logger.LogInformation(
                "Passkey authentication successful for member {MemberId} using credential {CredentialId}",
                credential.MemberID, credential.PasskeyCredentialID);

            return PasskeyAuthenticationResult.Succeeded(credential.MemberID, member.MemberEmail);
        }
        catch (Fido2VerificationException ex)
        {
            logger.LogWarning(ex, "Passkey authentication verification failed");
            return PasskeyAuthenticationResult.Failed($"Verification failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Passkey authentication failed");
            return PasskeyAuthenticationResult.Failed("Authentication failed");
        }
    }

    /// <inheritdoc/>
    public async Task<PasskeyDeleteResult> DeleteCredentialAsync(int memberId, int credentialId)
    {
        if (!IsTableAvailable())
            return PasskeyDeleteResult.Failed("Passkey storage not available");

        try
        {
            var credential = await Provider.GetAsync(credentialId);
            if (credential == null || credential.MemberID != memberId)
                return PasskeyDeleteResult.NotFound;

            // Soft delete by deactivating
            credential.IsActive = false;
            credential.LastModified = DateTime.UtcNow;
            Provider.Set(credential);

            logger.LogInformation(
                "Passkey deleted for member {MemberId}: credential {CredentialId}",
                memberId, credentialId);

            return PasskeyDeleteResult.Succeeded;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete passkey {CredentialId} for member {MemberId}",
                credentialId, memberId);
            return PasskeyDeleteResult.Failed("Deletion failed");
        }
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateDeviceNameAsync(int memberId, int credentialId, string newName)
    {
        if (!IsTableAvailable())
            return false;

        try
        {
            var credential = await Provider.GetAsync(credentialId);
            if (credential == null || credential.MemberID != memberId)
                return false;

            credential.DeviceName = newName.Length > 200 ? newName[..200] : newName;
            credential.LastModified = DateTime.UtcNow;
            Provider.Set(credential);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update passkey name {CredentialId} for member {MemberId}",
                credentialId, memberId);
            return false;
        }
    }

    private async Task<IReadOnlyList<PublicKeyCredentialDescriptor>> GetExistingCredentialDescriptors(int memberId)
    {
        if (!IsTableAvailable())
            return [];

        var credentials = await Provider.Get()
            .WhereEquals(nameof(PasskeyCredentialInfo.MemberID), memberId)
            .WhereEquals(nameof(PasskeyCredentialInfo.IsActive), true)
            .GetEnumerableTypedResultAsync();

        return credentials.Select(c => new PublicKeyCredentialDescriptor(
            Convert.FromBase64String(c.CredentialId))).ToList();
    }
}
