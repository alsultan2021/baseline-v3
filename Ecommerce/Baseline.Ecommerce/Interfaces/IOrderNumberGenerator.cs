namespace Baseline.Ecommerce.Interfaces;

/// <summary>
/// Service responsible for generating unique order numbers.
/// </summary>
public interface IOrderNumberGenerator
{
    /// <summary>
    /// Generates a new unique order number.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the generated order number.</returns>
    Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken = default);
}
