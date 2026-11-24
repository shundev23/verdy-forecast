using VerdyForecast.Domain.Tags;

namespace VerdyForecast.Domain.Reviews;

/// <summary>試合に付けた戦術タグ。良い点か悪い点かは、タグのマスタの種類で決まる。</summary>
public sealed record ReviewTag(TagId TagId, TagSource Source);
