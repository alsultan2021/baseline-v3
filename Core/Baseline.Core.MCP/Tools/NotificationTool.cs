using System.ComponentModel;
using System.Text.Json;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

using ModelContextProtocol.Server;

namespace Baseline.Core.MCP.Tools;

/// <summary>
/// MCP tools for notification and email discovery.
/// Read-only tools using SQL queries (works regardless of feature state).
/// </summary>
[McpServerToolType]
public static class NotificationTool
{
    /// <summary>
    /// Gets all notification emails configured in the system.
    /// </summary>
    [McpServerTool(
        Name = nameof(GetNotifications),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Get Notifications"),
    Description("Gets all system notification emails configured in Xperience")]
    public static async Task<string> GetNotifications(
        IOptions<BaselineMCPConfiguration> options,
        IConfiguration configuration)
    {
        var connString = configuration.GetConnectionString("CMSConnectionString");
        if (string.IsNullOrEmpty(connString))
        {
            return JsonSerializer.Serialize(new { Error = "No connection string found" }, options.Value.SerializerOptions);
        }

        const string sql = """
            SELECT 
                NotificationEmailID, NotificationEmailGUID, NotificationEmailCodeName,
                NotificationEmailDisplayName, NotificationEmailSubject, NotificationEmailSenderEmail
            FROM CMS_NotificationEmail
            ORDER BY NotificationEmailDisplayName
            """;

        try
        {
            await using var conn = new SqlConnection(connString);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            var notifications = new List<object>();
            while (await reader.ReadAsync())
            {
                notifications.Add(new
                {
                    Id = reader.GetInt32(0),
                    Guid = reader.GetGuid(1),
                    CodeName = reader.GetString(2),
                    DisplayName = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Subject = reader.IsDBNull(4) ? null : reader.GetString(4),
                    SenderEmail = reader.IsDBNull(5) ? null : reader.GetString(5)
                });
            }

            return JsonSerializer.Serialize(new
            {
                TotalCount = notifications.Count,
                Notifications = notifications
            }, options.Value.SerializerOptions);
        }
        catch (SqlException ex) when (ex.Message.Contains("Invalid object name"))
        {
            return JsonSerializer.Serialize(new
            {
                Error = "Notification table not found.",
                Message = ex.Message
            }, options.Value.SerializerOptions);
        }
    }

    /// <summary>
    /// Gets all email channels configured in the system.
    /// </summary>
    [McpServerTool(
        Name = nameof(GetEmailChannels),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Get Email Channels"),
    Description("Gets all email channels configured for marketing emails")]
    public static async Task<string> GetEmailChannels(
        IOptions<BaselineMCPConfiguration> options,
        IConfiguration configuration)
    {
        var connString = configuration.GetConnectionString("CMSConnectionString");
        if (string.IsNullOrEmpty(connString))
        {
            return JsonSerializer.Serialize(new { Error = "No connection string found" }, options.Value.SerializerOptions);
        }

        const string sql = """
            SELECT 
                EmailChannelID, EmailChannelGUID, c.ChannelName,
                EmailChannelSenderName, EmailChannelSenderEmail, EmailChannelServiceDomain
            FROM CMS_EmailChannel ec
            INNER JOIN CMS_Channel c ON c.ChannelID = ec.EmailChannelChannelID
            ORDER BY c.ChannelName
            """;

        try
        {
            await using var conn = new SqlConnection(connString);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            var channels = new List<object>();
            while (await reader.ReadAsync())
            {
                channels.Add(new
                {
                    Id = reader.GetInt32(0),
                    Guid = reader.GetGuid(1),
                    Name = reader.GetString(2),
                    SenderName = reader.IsDBNull(3) ? null : reader.GetString(3),
                    SenderEmail = reader.IsDBNull(4) ? null : reader.GetString(4),
                    ServiceDomain = reader.IsDBNull(5) ? null : reader.GetString(5)
                });
            }

            return JsonSerializer.Serialize(new
            {
                TotalCount = channels.Count,
                Channels = channels
            }, options.Value.SerializerOptions);
        }
        catch (SqlException ex) when (ex.Message.Contains("Invalid object name"))
        {
            return JsonSerializer.Serialize(new
            {
                Error = "Email channel tables not found.",
                Message = ex.Message
            }, options.Value.SerializerOptions);
        }
    }

    /// <summary>
    /// Gets all email templates configured in the system.
    /// </summary>
    [McpServerTool(
        Name = nameof(GetEmailTemplates),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Get Email Templates"),
    Description("Gets all email templates (notification templates) in the system")]
    public static async Task<string> GetEmailTemplates(
        IOptions<BaselineMCPConfiguration> options,
        IConfiguration configuration)
    {
        var connString = configuration.GetConnectionString("CMSConnectionString");
        if (string.IsNullOrEmpty(connString))
        {
            return JsonSerializer.Serialize(new { Error = "No connection string found" }, options.Value.SerializerOptions);
        }

        const string sql = """
            SELECT 
                NotificationTemplateID, NotificationTemplateGUID,
                NotificationTemplateCodeName, NotificationTemplateDisplayName
            FROM CMS_NotificationTemplate
            ORDER BY NotificationTemplateDisplayName
            """;

        try
        {
            await using var conn = new SqlConnection(connString);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            var templates = new List<object>();
            while (await reader.ReadAsync())
            {
                templates.Add(new
                {
                    Id = reader.GetInt32(0),
                    Guid = reader.GetGuid(1),
                    CodeName = reader.GetString(2),
                    DisplayName = reader.IsDBNull(3) ? null : reader.GetString(3)
                });
            }

            return JsonSerializer.Serialize(new
            {
                TotalCount = templates.Count,
                Templates = templates
            }, options.Value.SerializerOptions);
        }
        catch (SqlException ex) when (ex.Message.Contains("Invalid object name"))
        {
            return JsonSerializer.Serialize(new
            {
                Error = "Notification template table not found.",
                Message = ex.Message
            }, options.Value.SerializerOptions);
        }
    }

    /// <summary>
    /// Gets email queue statistics.
    /// </summary>
    [McpServerTool(
        Name = nameof(GetEmailQueueStats),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Get Email Queue Stats"),
    Description("Gets statistics about the email queue (pending, sent, failed)")]
    public static async Task<string> GetEmailQueueStats(
        IOptions<BaselineMCPConfiguration> options,
        IConfiguration configuration,
        [Description("Only include emails from last N hours (default: 24)")] int lastHours = 24)
    {
        var connString = configuration.GetConnectionString("CMSConnectionString");
        if (string.IsNullOrEmpty(connString))
        {
            return JsonSerializer.Serialize(new { Error = "No connection string found" }, options.Value.SerializerOptions);
        }

        var sql = $"""
            SELECT 
                EmailStatus,
                COUNT(*) as EmailCount
            FROM CMS_Email
            WHERE EmailCreated >= DATEADD(hour, -{lastHours}, GETUTCDATE())
            GROUP BY EmailStatus
            ORDER BY EmailStatus
            """;

        try
        {
            await using var conn = new SqlConnection(connString);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            var stats = new Dictionary<int, int>();
            var total = 0;

            while (await reader.ReadAsync())
            {
                var status = reader.GetInt32(0);
                var count = reader.GetInt32(1);
                stats[status] = count;
                total += count;
            }

            return JsonSerializer.Serialize(new
            {
                Period = $"Last {lastHours} hours",
                TotalEmails = total,
                Created = stats.GetValueOrDefault(0, 0),
                Waiting = stats.GetValueOrDefault(1, 0),
                Sending = stats.GetValueOrDefault(2, 0),
                Sent = stats.GetValueOrDefault(3, 0),
                Archived = stats.GetValueOrDefault(4, 0)
            }, options.Value.SerializerOptions);
        }
        catch (SqlException ex) when (ex.Message.Contains("Invalid object name"))
        {
            return JsonSerializer.Serialize(new
            {
                Error = "Email queue table not found.",
                Message = ex.Message
            }, options.Value.SerializerOptions);
        }
    }
}
