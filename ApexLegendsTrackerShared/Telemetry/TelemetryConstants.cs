namespace ApexLegendsTracker.Shared.Telemetry;

/// <summary>Custom Application Insights event names shared by the Web and Service tiers.</summary>
public static class TelemetryEvents
{
	public const string PlayerLookupRequested = "PlayerLookupRequested";
	public const string PlayerLookupSucceeded = "PlayerLookupSucceeded";
	public const string PlayerLookupFailed = "PlayerLookupFailed";
	public const string MapRotationRequested = "MapRotationRequested";
	public const string PredatorThresholdsRequested = "PredatorThresholdsRequested";
	public const string ChatRequested = "ChatRequested";
	public const string ChatSucceeded = "ChatSucceeded";
	public const string ChatFailed = "ChatFailed";
}

/// <summary>Custom Application Insights property names shared by the Web and Service tiers.</summary>
public static class TelemetryProperties
{
	public const string Platform = "Platform";
	public const string MapRotationVersion = "MapRotationVersion";
	public const string ErrorMessage = "ErrorMessage";
}
