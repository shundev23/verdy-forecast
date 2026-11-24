using VerdyForecast.Domain.Common;
using VerdyForecast.Domain.Players;

namespace VerdyForecast.Domain.Reviews;

/// <summary>
/// 出場の記録（公式サイトを見て手で入れる数字）。参考点と MVP の計算に使う（作業指示書 D6、D7、2-4）。
/// </summary>
public sealed record PlayerAppearance
{
    public const int MinMinutes = 1;
    public const int MaxMinutes = 120;
    public const int MaxYellowCards = 2;

    private PlayerAppearance(PlayerId playerId, int minutes, int goals, int assists, int yellowCards, bool sentOff, int ownGoals)
    {
        PlayerId = playerId;
        Minutes = minutes;
        Goals = goals;
        Assists = assists;
        YellowCards = yellowCards;
        SentOff = sentOff;
        OwnGoals = ownGoals;
    }

    public PlayerId PlayerId { get; }

    /// <summary>出場時間（分）。</summary>
    public int Minutes { get; }

    public int Goals { get; }

    public int Assists { get; }

    /// <summary>警告の枚数（0〜2）。</summary>
    public int YellowCards { get; }

    /// <summary>退場したか（一発退場と、警告2枚の両方）。</summary>
    public bool SentOff { get; }

    public int OwnGoals { get; }

    public static Result<PlayerAppearance> Create(
        PlayerId playerId,
        int minutes,
        int goals,
        int assists,
        int yellowCards,
        bool sentOff,
        int ownGoals)
    {
        var error = Validate(minutes, goals, assists, yellowCards, sentOff, ownGoals);
        return error is null
            ? Result.Success(new PlayerAppearance(playerId, minutes, goals, assists, yellowCards, sentOff, ownGoals))
            : Result.Failure<PlayerAppearance>(error);
    }

    private static DomainError? Validate(int minutes, int goals, int assists, int yellowCards, bool sentOff, int ownGoals)
    {
        if (minutes is < MinMinutes or > MaxMinutes)
        {
            return ReviewErrors.MinutesOutOfRange;
        }

        if (goals < 0 || assists < 0 || ownGoals < 0)
        {
            return ReviewErrors.NegativeCount;
        }

        if (yellowCards is < 0 or > MaxYellowCards)
        {
            return ReviewErrors.YellowCardsOutOfRange;
        }

        if (yellowCards == MaxYellowCards && !sentOff)
        {
            return ReviewErrors.SecondYellowWithoutSendingOff;
        }

        return null;
    }
}
