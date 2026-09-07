using System.Text.Json.Serialization;

namespace ApexLegendsTracker.Shared;

public sealed class MapRotationResponse
{
	[JsonPropertyName("battle_royale")]
	public MapRotationMode? BattleRoyale { get; init; }

	public MapRotationMode? Ranked { get; init; }

	public MapRotationMode? Ltm { get; init; }

	public MapRotationMode? Wildcard { get; init; }
}

public sealed class MapRotationMode
{
	public MapRotationEntry? Current { get; init; }

	public MapRotationEntry? Next { get; init; }
}

public sealed class MapRotationEntry
{
	public long Start { get; init; }

	public long End { get; init; }

	[JsonPropertyName("readableDate_start")]
	public string? ReadableDateStart { get; init; }

	[JsonPropertyName("readableDate_end")]
	public string? ReadableDateEnd { get; init; }

	public string? Map { get; init; }

	public string? Code { get; init; }

	[JsonPropertyName("DurationInSecs")]
	public int DurationInSeconds { get; init; }

	[JsonPropertyName("DurationInMinutes")]
	public int DurationInMinutes { get; init; }

	public bool? IsActive { get; init; }

	public string? EventName { get; init; }

	public string? Asset { get; init; }

	public int? RemainingSecs { get; init; }

	public int? RemainingMins { get; init; }

	public string? RemainingTimer { get; init; }
}