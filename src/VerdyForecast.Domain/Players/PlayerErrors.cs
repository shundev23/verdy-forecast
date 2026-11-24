using VerdyForecast.Domain.Common;

namespace VerdyForecast.Domain.Players;

/// <summary>選手に関する失敗。</summary>
public static class PlayerErrors
{
    public static readonly DomainError ShirtNumberOutOfRange = new("player.shirtNumberOutOfRange", "背番号は1〜99にしてください。");
}
