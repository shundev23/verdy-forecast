using VerdyForecast.Domain.Players;

namespace VerdyForecast.Domain.Reviews;

/// <summary>
/// 選手の採点。参考点（下書き）は作業指示書 2-4 で計算して入れる。最終点は自分が付ける。
/// どちらもないときは null。
/// </summary>
public sealed record PlayerRating(PlayerId PlayerId, RatingScore? Reference, RatingScore? Final);
