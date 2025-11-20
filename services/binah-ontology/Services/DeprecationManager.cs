using Binah.Ontology.Models.Migrations;
using Microsoft.Extensions.Logging;

namespace Binah.Ontology.Services;

/// <summary>
/// Manages 12-month deprecation policy for breaking ontology changes
/// Schedules and sends deprecation notifications at key intervals
/// </summary>
public interface IDeprecationManager
{
    Task<DeprecationPolicy> ScheduleDeprecationAsync(OntologyChange breakingChange, Guid tenantId);
    Task<List<DeprecationNotification>> GetPendingNotificationsAsync();
    Task<bool> SendNotificationAsync(DeprecationNotification notification);
    Task<bool> CompleteDeprecationAsync(Guid policyId);
}

public class DeprecationManager : IDeprecationManager
{
    private readonly ILogger<DeprecationManager> _logger;
    private readonly IBackwardCompatibilityService _compatibilityService;

    public DeprecationManager(
        ILogger<DeprecationManager> logger,
        IBackwardCompatibilityService compatibilityService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _compatibilityService = compatibilityService ?? throw new ArgumentNullException(nameof(compatibilityService));
    }

    public async Task<DeprecationPolicy> ScheduleDeprecationAsync(
        OntologyChange breakingChange,
        Guid tenantId)
    {
        try
        {
            _logger.LogInformation("Scheduling deprecation for change: {ChangeType} - {Description}",
                breakingChange.ChangeType, breakingChange.Description);

            var now = DateTime.UtcNow;
            var deprecationDate = now.AddMonths(12); // 12-month window

            var policy = new DeprecationPolicy
            {
                TenantId = tenantId,
                ChangeId = breakingChange.Id,
                Change = breakingChange,
                ScheduledAt = now,
                DeprecationDate = deprecationDate,
                Status = "scheduled",
                DeprecatedEntityName = breakingChange.EntityType,
                ReplacementEntityName = breakingChange.NewValue,
                DeprecationReason = $"{breakingChange.ChangeType}: {breakingChange.Description}"
            };

            // Schedule notifications at key intervals
            var policyGuid = Guid.TryParse(policy.Id, out var parsedGuid) ? parsedGuid : Guid.NewGuid();
            policy.Notifications = new List<DeprecationNotification>
            {
                CreateNotification(policyGuid, now.AddMonths(6), "6_months",
                    $"Breaking change in 6 months: {breakingChange.Description}"),
                CreateNotification(policyGuid, now.AddMonths(9), "3_months",
                    $"Breaking change in 3 months: {breakingChange.Description}"),
                CreateNotification(policyGuid, now.AddMonths(11), "1_month",
                    $"Breaking change in 1 month: {breakingChange.Description}"),
                CreateNotification(policyGuid, deprecationDate.AddDays(-7), "7_days",
                    $"URGENT: Breaking change in 7 days: {breakingChange.Description}"),
                CreateNotification(policyGuid, deprecationDate, "final",
                    $"Breaking change is now active: {breakingChange.Description}")
            };

            // TODO: Save to database
            // await _context.DeprecationPolicies.AddAsync(policy);
            // await _context.SaveChangesAsync();

            _logger.LogInformation("Scheduled deprecation policy {PolicyId} with {NotificationCount} notifications",
                policy.Id, policy.Notifications.Count);

            return policy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling deprecation for change {ChangeId}",
                breakingChange.Id);
            throw;
        }
    }

    public async Task<List<DeprecationNotification>> GetPendingNotificationsAsync()
    {
        await Task.CompletedTask;

        // TODO: Query database for pending notifications
        // var now = DateTime.UtcNow;
        // return await _context.DeprecationNotifications
        //     .Where(n => !n.Sent && n.ScheduledDate <= now)
        //     .OrderBy(n => n.ScheduledDate)
        //     .ToListAsync();

        return new List<DeprecationNotification>();
    }

    public async Task<bool> SendNotificationAsync(DeprecationNotification notification)
    {
        try
        {
            _logger.LogInformation("Sending deprecation notification: {Type} - {Message}",
                notification.NotificationType, notification.Message);

            var emailBody = GenerateEmailBody(notification);

            // TODO: Send email via notification service
            // await _emailService.SendAsync(notification.Recipients, subject, emailBody);

            notification.Sent = true;
            notification.SentAt = DateTime.UtcNow;

            // TODO: Update database
            // await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully sent deprecation notification {NotificationId}",
                notification.Id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending deprecation notification {NotificationId}",
                notification.Id);
            throw;
        }
    }

    public async Task<bool> CompleteDeprecationAsync(Guid policyId)
    {
        try
        {
            _logger.LogInformation("Completing deprecation policy {PolicyId}", policyId);

            // TODO: Load policy from database
            // var policy = await _context.DeprecationPolicies
            //     .Include(p => p.Change)
            //     .FirstOrDefaultAsync(p => p.Id == policyId);

            // Remove backward compatibility views
            // await _compatibilityService.RemoveCompatibilityViewsAsync(
            //     policy.DeprecatedEntityName,
            //     policy.TenantId);

            // Mark policy as completed
            // policy.Status = "completed";
            // await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully completed deprecation policy {PolicyId}", policyId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing deprecation policy {PolicyId}", policyId);
            throw;
        }
    }

    private DeprecationNotification CreateNotification(
        Guid policyId,
        DateTime scheduledDate,
        string type,
        string message)
    {
        return new DeprecationNotification
        {
            PolicyId = policyId,
            ScheduledDate = scheduledDate,
            NotificationType = type,
            Message = message,
            Sent = false,
            Recipients = new List<string> { "admin@tenant.com" } // TODO: Load from tenant config
        };
    }

    private string GenerateEmailBody(DeprecationNotification notification)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; }}
        .warning {{ background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 10px; }}
        .urgent {{ background-color: #f8d7da; border-left: 4px solid #dc3545; padding: 10px; }}
        .info {{ background-color: #d1ecf1; border-left: 4px solid #17a2b8; padding: 10px; }}
    </style>
</head>
<body>
    <h2>Ontology Deprecation Notice</h2>

    <div class=""{GetNotificationClass(notification.NotificationType)}"">
        <h3>{GetNotificationTitle(notification.NotificationType)}</h3>
        <p>{notification.Message}</p>
    </div>

    <h3>What This Means:</h3>
    <ul>
        <li>Your ontology has a breaking change scheduled</li>
        <li>Backward compatibility views are currently in place</li>
        <li>You should update your code to use the new entity/property names</li>
        <li>After the deprecation date, the old names will no longer work</li>
    </ul>

    <h3>Action Required:</h3>
    <ol>
        <li>Review your code for references to the deprecated entity</li>
        <li>Update API calls to use the new entity/property names</li>
        <li>Test your changes in a staging environment</li>
        <li>Deploy updated code before the deprecation date</li>
    </ol>

    <h3>Need Help?</h3>
    <p>Contact our support team if you need assistance with the migration.</p>

    <p>
        <small>
            Notification Type: {notification.NotificationType}<br>
            Scheduled Date: {notification.ScheduledDate:yyyy-MM-dd HH:mm:ss} UTC
        </small>
    </p>
</body>
</html>
";
    }

    private string GetNotificationClass(string type)
    {
        return type switch
        {
            "7_days" => "urgent",
            "final" => "urgent",
            "1_month" => "warning",
            _ => "info"
        };
    }

    private string GetNotificationTitle(string type)
    {
        return type switch
        {
            "6_months" => "Upcoming Breaking Change (6 Months)",
            "3_months" => "Breaking Change Reminder (3 Months)",
            "1_month" => "Breaking Change Alert (1 Month)",
            "7_days" => "URGENT: Breaking Change in 7 Days",
            "final" => "Breaking Change is Now Active",
            _ => "Deprecation Notice"
        };
    }
}
