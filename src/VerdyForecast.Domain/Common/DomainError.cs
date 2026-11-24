namespace VerdyForecast.Domain.Common;

/// <summary>
/// 想定した失敗（不正な入力やルールの違反）。
/// Code は画面や API で使う識別子、Message は人が読む説明。
/// </summary>
public sealed record DomainError(string Code, string Message);
