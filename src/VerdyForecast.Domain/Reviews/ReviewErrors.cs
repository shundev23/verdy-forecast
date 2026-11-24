using VerdyForecast.Domain.Common;

namespace VerdyForecast.Domain.Reviews;

/// <summary>試合後のレビューに関する失敗。</summary>
public static class ReviewErrors
{
    public static readonly DomainError RatingOutOfRange = new("rating.outOfRange", "採点は1.0〜10.0にしてください。");

    public static readonly DomainError RatingInvalidStep = new("rating.invalidStep", "採点は0.5刻みにしてください。");

    public static readonly DomainError MinutesOutOfRange = new("appearance.minutesOutOfRange", "出場時間は1〜120分にしてください。");

    public static readonly DomainError NegativeCount = new("appearance.negativeCount", "得点、アシスト、オウンゴールは0以上にしてください。");

    public static readonly DomainError YellowCardsOutOfRange = new("appearance.yellowCardsOutOfRange", "警告は0〜2枚にしてください。");

    public static readonly DomainError SecondYellowWithoutSendingOff = new("appearance.secondYellowWithoutSendingOff", "警告が2枚のときは、退場にしてください。");

    public static readonly DomainError PlayerNotAppeared = new("review.playerNotAppeared", "出場していない選手です。先に出場の記録を入れてください。");
}
