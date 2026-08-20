namespace ApexLegendsTracker.Shared;

/// <summary>Shared query signature implemented by the Service's lookup logic and consumed by the Web's API client.</summary>
public interface IPlayerLookupContract
{
	Task<PlayerLookupResult> QueryByNameAsync(string playerName, string platform, CancellationToken cancellationToken = default);
}

