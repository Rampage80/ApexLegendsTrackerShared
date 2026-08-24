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

	// Additive (v1.1.0): real account fields the upstream API sends but were previously discarded.
	public string Tag { get; init; } = string.Empty;

	public string Uid { get; init; } = string.Empty;

	public string? Avatar { get; init; }

	public int LevelPrestige { get; init; }

	public int ToNextLevelPercent { get; init; }

	public PlayerBanStatus Bans { get; init; } = new();

	public PlayerRank Arena { get; init; } = new();

	public PlayerBattlepass Battlepass { get; init; } = new();

	public List<PlayerBadge> Badges { get; init; } = [];
}

public sealed class PlayerBanStatus
{
	public bool IsActive { get; init; }

	public int RemainingSeconds { get; init; }
}

public sealed class PlayerBattlepass
{
	public int? Level { get; init; }
}

public sealed class PlayerBadge
{
	public string Name { get; init; } = string.Empty;

	public int Value { get; init; }
}

public sealed class PlayerRank
{
	public int RankScore { get; init; }

	public string RankName { get; init; } = string.Empty;

	public int RankDiv { get; init; }

	// Additive (v1.1.0).
	public string? RankImg { get; init; }

	public string? RankedSeason { get; init; }
}

public sealed class PlayerRealtimeStats
{
	public string SelectedLegend { get; init; } = string.Empty;

	public string CurrentStateAsText { get; init; } = string.Empty;

	public int IsOnline { get; init; }

	// Additive (v1.1.0): richer presence data the upstream API sends but was previously discarded.
	public string LobbyState { get; init; } = string.Empty;

	public int IsInGame { get; init; }

	public int CanJoin { get; init; }

	public int PartyFull { get; init; }
}

public sealed class PlayerLegends
{
	public SelectedLegend Selected { get; init; } = new();

	// Additive (v1.1.0): per-legend stat breakdown, keyed by legend name (includes "Global").
	public Dictionary<string, LegendBreakdown> All { get; init; } = [];
}

public sealed class SelectedLegend
{
	public string LegendName { get; init; } = string.Empty;

	public List<LegendStat> Data { get; init; } = [];

	// Additive (v1.1.0).
	public LegendGameInfo? GameInfo { get; init; }

	public LegendImageAssets? ImgAssets { get; init; }
}

public sealed class LegendBreakdown
{
	public List<LegendStat> Data { get; init; } = [];

	public LegendImageAssets? ImgAssets { get; init; }
}

public sealed class LegendGameInfo
{
	public string? Skin { get; init; }

	public string? SkinRarity { get; init; }

	public string? Frame { get; init; }

	public string? FrameRarity { get; init; }

	public string? Pose { get; init; }

	public string? PoseRarity { get; init; }

	public string? Intro { get; init; }

	public string? IntroRarity { get; init; }

	public List<PlayerBadge> Badges { get; init; } = [];
}

public sealed class LegendImageAssets
{
	public string? Icon { get; init; }

	public string? Banner { get; init; }
}

public sealed class LegendStat
{
	public string Name { get; init; } = string.Empty;

	public int Value { get; init; }
}
