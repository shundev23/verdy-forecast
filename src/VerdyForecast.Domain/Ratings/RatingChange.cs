using VerdyForecast.Domain.Matches;
using VerdyForecast.Domain.Teams;

namespace VerdyForecast.Domain.Ratings;

/// <summary>1チームのレーティングの、試合の前と後。</summary>
public sealed record TeamRatingChange(TeamId TeamId, double Before, double After);

/// <summary>
/// 1試合を反映したときの変化。TeamRatings に「いつ時点の値か」と一緒に保存する材料。
/// 試合前の値（Before）が、その試合の予測に使える値（キックオフ時点で分かっていた値）。
/// </summary>
public sealed record RatingChange(MatchId MatchId, KickoffTime Kickoff, TeamRatingChange Home, TeamRatingChange Away);
