namespace ZNext.Services;

internal sealed record NodeListStatistics(
	string OnlineNodesText,
	string OnlineUsersText,
	string OnlineTunnelsText,
	string TodayInText,
	string TodayOutText);
