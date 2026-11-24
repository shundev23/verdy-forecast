namespace VerdyForecast.Domain.Matches;

/// <summary>試合の状態。</summary>
public enum MatchStatus
{
    /// <summary>予定（キックオフの日時が決まっている）</summary>
    Scheduled,

    /// <summary>延期（新しい日時が決まっていない）</summary>
    Postponed,

    /// <summary>終了（90分のスコアがある）</summary>
    Finished,
}
