using VerdyForecast.Domain.Matches;
using VerdyForecast.Domain.Players;

namespace VerdyForecast.Domain.Tests.TestSupport;

/// <summary>
/// 保存のときに入る ID（本番では EF Core が入れる）を、テストで入れる。
/// ID の setter は private なので、リフレクションで入れる。
/// </summary>
internal static class EntityIds
{
    public static Match WithId(this Match match, int id)
    {
        typeof(Match).GetProperty(nameof(Match.Id))!.SetValue(match, new MatchId(id));
        return match;
    }

    public static Player WithId(this Player player, int id)
    {
        typeof(Player).GetProperty(nameof(Player.Id))!.SetValue(player, new PlayerId(id));
        return player;
    }
}
