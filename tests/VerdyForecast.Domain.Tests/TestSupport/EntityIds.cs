using VerdyForecast.Domain.Matches;

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
}
