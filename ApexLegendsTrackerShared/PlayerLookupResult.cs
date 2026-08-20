namespace ApexLegendsTracker.Shared;

/// <summary>Canonical response shape for the players lookup endpoint, shared by the Web and Service repos.</summary>
public sealed class PlayerLookupResult
{
	public string PlayerName { get; set; } = string.Empty;

	public string Platform { get; set; } = string.Empty;

	public PlayerGlobalStats Global { get; init; } = new();

	public PlayerRealtimeStats Realtime { get; init; } = new();

	public PlayerLegends Legends { get; init; } = new();
}

public sealed class PlayerGlobalStats
{
	public string Name { get; init; } = string.Empty;

	public string Platform { get; init; } = string.Empty;

	public int Level { get; init; }

	public PlayerRank Rank { get; init; } = new();
}

public sealed class PlayerRank
{
	public int RankScore { get; init; }

	public string RankName { get; init; } = string.Empty;

	public int RankDiv { get; init; }
}

public sealed class PlayerRealtimeStats
{
	public string SelectedLegend { get; init; } = string.Empty;

	public string CurrentStateAsText { get; init; } = string.Empty;

	public int IsOnline { get; init; }
}

public sealed class PlayerLegends
{
	public SelectedLegend Selected { get; init; } = new();
}

public sealed class SelectedLegend
{
	public string LegendName { get; init; } = string.Empty;

	public List<LegendStat> Data { get; init; } = [];
}

public sealed class LegendStat
{
	public string Name { get; init; } = string.Empty;

	public int Value { get; init; }
}
