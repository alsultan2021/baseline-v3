using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Baseline.Ecommerce;

/// <summary>
/// Service for retrieving and managing customer data.
/// Provides a unified interface for customer operations.
/// </summary>
public interface ICustomerDataRetriever
{
    /// <summary>
    /// Gets a customer by their unique ID.
    /// </summary>
    /// <param name="customerId">The customer ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The customer data, or null if not found.</returns>
    Task<CustomerData?> GetCustomerByIdAsync(int customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a customer by their email address.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The customer data, or null if not found.</returns>
    Task<CustomerData?> GetCustomerByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a customer by their associated member ID.
    /// </summary>
    /// <param name="memberId">The member ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The customer data, or null if not found.</returns>
    Task<CustomerData?> GetCustomerByMemberIdAsync(int memberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all addresses for a customer.
    /// </summary>
    /// <param name="customerId">The customer ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of customer addresses.</returns>
    Task<IReadOnlyList<CustomerAddressData>> GetCustomerAddressesAsync(int customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current customer for the logged-in member.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The customer data, or null if not authenticated or no customer exists.</returns>
    Task<CustomerData?> GetCurrentCustomerAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Customer data transfer object.
/// </summary>
public record CustomerData
{
    /// <summary>
    /// The customer's unique ID.
    /// </summary>
    public int CustomerId { get; init; }

    /// <summary>
    /// The customer's first name.
    /// </summary>
    public string FirstName { get; init; } = "";

    /// <summary>
    /// The customer's last name.
    /// </summary>
    public string LastName { get; init; } = "";

    /// <summary>
    /// The customer's email address.
    /// </summary>
    public string Email { get; init; } = "";

    /// <summary>
    /// The customer's phone number.
    /// </summary>
    public string Phone { get; init; } = "";

    /// <summary>
    /// The customer's company name.
    /// </summary>
    public string Company { get; init; } = "";

    /// <summary>
    /// The associated member ID, if any.
    /// </summary>
    public int? MemberId { get; init; }

    /// <summary>
    /// The customer's full name.
    /// </summary>
    public string FullName => string.IsNullOrEmpty(FirstName) && string.IsNullOrEmpty(LastName)
        ? Email
        : $"{FirstName} {LastName}".Trim();
}

/// <summary>
/// Customer address data transfer object.
/// </summary>
public record CustomerAddressData
{
    /// <summary>
    /// The address ID.
    /// </summary>
    public int AddressId { get; init; }

    /// <summary>
    /// The customer ID this address belongs to.
    /// </summary>
    public int CustomerId { get; init; }

    /// <summary>
    /// The first line of the address.
    /// </summary>
    public string AddressLine1 { get; init; } = "";

    /// <summary>
    /// The second line of the address.
    /// </summary>
    public string AddressLine2 { get; init; } = "";

    /// <summary>
    /// The city.
    /// </summary>
    public string City { get; init; } = "";

    /// <summary>
    /// The state or region code.
    /// </summary>
    public string State { get; init; } = "";

    /// <summary>
    /// The postal/ZIP code.
    /// </summary>
    public string PostalCode { get; init; } = "";

    /// <summary>
    /// The country code.
    /// </summary>
    public string Country { get; init; } = "";

    /// <summary>
    /// The contact name for this address.
    /// </summary>
    public string ContactName { get; init; } = "";

    /// <summary>
    /// The phone number for this address.
    /// </summary>
    public string Phone { get; init; } = "";

    /// <summary>
    /// Whether this is the default address.
    /// </summary>
    public bool IsDefault { get; init; }
}

/// <summary>
/// Abstract base implementation of <see cref="ICustomerDataRetriever"/>.
/// Sites should provide a concrete implementation that maps Kentico CustomerInfo
/// to CustomerData using the actual property names from the Kentico version in use.
/// </summary>
public abstract class CustomerDataRetrieverBase(
    IHttpContextAccessor httpContextAccessor,
    ILogger logger) : ICustomerDataRetriever
{
    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger Logger => logger;

    /// <summary>
    /// Gets the HTTP context accessor.
    /// </summary>
    protected IHttpContextAccessor HttpContextAccessor => httpContextAccessor;

    /// <inheritdoc/>
    public abstract Task<CustomerData?> GetCustomerByIdAsync(int customerId, CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public abstract Task<CustomerData?> GetCustomerByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public abstract Task<CustomerData?> GetCustomerByMemberIdAsync(int memberId, CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public abstract Task<IReadOnlyList<CustomerAddressData>> GetCustomerAddressesAsync(int customerId, CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public virtual async Task<CustomerData?> GetCurrentCustomerAsync(CancellationToken cancellationToken = default)
    {
        var httpContext = HttpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            Logger.LogDebug("No authenticated user, cannot retrieve current customer");
            return null;
        }

        // Try to get member ID from claims
        var memberIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (memberIdClaim == null || !int.TryParse(memberIdClaim.Value, out var memberId))
        {
            Logger.LogDebug("Could not find member ID in claims");
            return null;
        }

        return await GetCustomerByMemberIdAsync(memberId, cancellationToken);
    }
}

/// <summary>
/// No-op implementation of <see cref="ICustomerDataRetriever"/> for testing/development.
/// </summary>
public sealed class NoOpCustomerDataRetriever(
    IHttpContextAccessor httpContextAccessor,
    ILogger<NoOpCustomerDataRetriever> logger) : CustomerDataRetrieverBase(httpContextAccessor, logger)
{
    /// <inheritdoc/>
    public override Task<CustomerData?> GetCustomerByIdAsync(int customerId, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("NoOpCustomerDataRetriever: GetCustomerByIdAsync called for ID {CustomerId}", customerId);
        return Task.FromResult<CustomerData?>(null);
    }

    /// <inheritdoc/>
    public override Task<CustomerData?> GetCustomerByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("NoOpCustomerDataRetriever: GetCustomerByEmailAsync called for email {Email}", email);
        return Task.FromResult<CustomerData?>(null);
    }

    /// <inheritdoc/>
    public override Task<CustomerData?> GetCustomerByMemberIdAsync(int memberId, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("NoOpCustomerDataRetriever: GetCustomerByMemberIdAsync called for member {MemberId}", memberId);
        return Task.FromResult<CustomerData?>(null);
    }

    /// <inheritdoc/>
    public override Task<IReadOnlyList<CustomerAddressData>> GetCustomerAddressesAsync(int customerId, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("NoOpCustomerDataRetriever: GetCustomerAddressesAsync called for customer {CustomerId}", customerId);
        return Task.FromResult<IReadOnlyList<CustomerAddressData>>([]);
    }
}
