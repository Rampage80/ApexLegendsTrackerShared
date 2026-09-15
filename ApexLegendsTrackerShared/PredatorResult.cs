namespace ApexLegendsTracker.Shared;

public sealed class PredatorResult
{
	public PredatorPlatformThresholds? RP { get; init; }
}

public sealed class PredatorPlatformThresholds
{
	public PredatorThreshold? PC { get; init; }

	public PredatorThreshold? PS4 { get; init; }

	public PredatorThreshold? X1 { get; init; }
}

public sealed class PredatorThreshold
{
	public int FoundRank { get; init; }

	public int Val { get; init; }

	public string? Uid { get; init; }

	public long UpdateTimestamp { get; init; }

	public int TotalMastersAndPreds { get; init; }
}