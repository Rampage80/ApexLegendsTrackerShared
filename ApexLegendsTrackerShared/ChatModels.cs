namespace ApexLegendsTracker.Shared;

public enum ChatSource
{
	AIChat
}

public sealed record ChatRequest(string Message);

public sealed record ChatResponse(string Reply, ChatSource Source);
