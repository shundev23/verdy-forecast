namespace VerdyForecast.Domain.Teams;

/// <summary>チームの ID。0 は「まだ保存していない」を表す。</summary>
public readonly record struct TeamId(int Value);
