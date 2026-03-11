using Baseline.Ecommerce.Interfaces;
using CMS.DataEngine;
using CMS.Helpers;
using CMS.Base;

namespace Baseline.Ecommerce.Services;

/// <summary>
/// Generates unique order numbers using a SQL sequence.
/// Format: ORD#NNNNNN (e.g., "ORD#000042").
/// Auto-creates the sequence on first use — no collisions, no delays.
/// </summary>
public sealed class OrderNumberGenerator : IOrderNumberGenerator
{
    private const string SEQUENCE_NAME = "Baseline_OrderNumberSequence";

    /// <inheritdoc />
    public async Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken = default)
    {
        var sequence = await GetNextSequenceValueAsync(cancellationToken);
        return $"ORD#{sequence:D6}";
    }

    /// <summary>
    /// Returns the next value from the SQL sequence, creating it if it doesn't exist.
    /// </summary>
    private static async Task<long> GetNextSequenceValueAsync(CancellationToken cancellationToken)
    {
        var query = $"""
            IF NOT EXISTS (SELECT * FROM sys.sequences WHERE name = N'{SEQUENCE_NAME}')
            BEGIN
                CREATE SEQUENCE [{SEQUENCE_NAME}]
                    AS BIGINT
                    START WITH 1
                    INCREMENT BY 1
                    MINVALUE 1
                    NO CYCLE
                    CACHE 100;
            END;

            SELECT NEXT VALUE FOR [{SEQUENCE_NAME}];
            """;

        object? scalar;
        using (new CMSConnectionScope(true))
        {
            scalar = await ConnectionHelper.ExecuteScalarAsync(
            query, null, QueryTypeEnum.SQLQuery, cancellationToken);
        }

        return ValidationHelper.GetLong(scalar, 0);
    }
}
