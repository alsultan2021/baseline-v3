namespace Baseline.Automation;

/// <summary>
/// Event ID constants for structured logging with ILogger.
/// Maps to CMS.Automation.AutomationEventIds.
/// </summary>
public static class AutomationEventIds
{
    // Process lifecycle (1000-1099)
    public const int ProcessStarted = 1000;
    public const int ProcessFinished = 1001;
    public const int ProcessAborted = 1002;
    public const int ProcessError = 1003;
    public const int ProcessDisabled = 1004;
    public const int ProcessRecurrenceBlocked = 1005;

    // Step execution (1100-1199)
    public const int StepEntered = 1100;
    public const int StepCompleted = 1101;
    public const int StepFailed = 1102;
    public const int StepTimedOut = 1103;
    public const int StepSkipped = 1104;

    // Trigger events (1200-1299)
    public const int TriggerFired = 1200;
    public const int TriggerMatched = 1201;
    public const int TriggerRejected = 1202;
    public const int TriggerError = 1203;

    // Action execution (1300-1399)
    public const int ActionExecuting = 1300;
    public const int ActionExecuted = 1301;
    public const int ActionFailed = 1302;
    public const int ActionTimeout = 1303;

    // Email (1400-1499)
    public const int EmailSent = 1400;
    public const int EmailFailed = 1401;
    public const int EmailQueued = 1402;

    // Condition evaluation (1500-1599)
    public const int ConditionEvaluated = 1500;
    public const int ConditionTrue = 1501;
    public const int ConditionFalse = 1502;
    public const int ConditionError = 1503;

    // Background processing (1600-1699)
    public const int BackgroundProcessingStarted = 1600;
    public const int BackgroundProcessingCompleted = 1601;
    public const int BackgroundProcessingError = 1602;
    public const int StatisticsCalculated = 1603;

    // Graph operations (1700-1799)
    public const int GraphBuilt = 1700;
    public const int GraphValidated = 1701;
    public const int GraphValidationFailed = 1702;
}
