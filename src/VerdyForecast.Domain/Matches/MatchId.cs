namespace VerdyForecast.Domain.Matches;

/// <summary>試合の ID。0 は「まだ保存していない」を表す。</summary>
public readonly record struct MatchId(int Value);
