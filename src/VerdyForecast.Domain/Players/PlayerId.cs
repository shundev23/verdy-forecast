namespace VerdyForecast.Domain.Players;

/// <summary>選手の ID。0 は「まだ保存していない」を表す。</summary>
public readonly record struct PlayerId(int Value);
