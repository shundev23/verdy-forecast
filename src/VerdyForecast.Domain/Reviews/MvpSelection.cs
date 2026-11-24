using VerdyForecast.Domain.Players;

namespace VerdyForecast.Domain.Reviews;

/// <summary>
/// MVP。IsManual が true なら、自分が手で選んだもの（採点を直しても自動で上書きしない。作業指示書 D7）。
/// </summary>
public sealed record MvpSelection(PlayerId PlayerId, bool IsManual);
