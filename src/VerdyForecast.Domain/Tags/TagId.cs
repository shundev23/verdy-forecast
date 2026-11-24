namespace VerdyForecast.Domain.Tags;

/// <summary>戦術タグの ID。0 は「まだ保存していない」を表す。</summary>
public readonly record struct TagId(int Value);
