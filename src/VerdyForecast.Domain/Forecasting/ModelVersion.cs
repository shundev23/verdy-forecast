using VerdyForecast.Domain.Common;
using VerdyForecast.Domain.Matches;
using VerdyForecast.Domain.Ratings;

namespace VerdyForecast.Domain.Forecasting;

/// <summary>
/// モデルのバージョン（作業指示書の7章の ModelVersions）。Elo の設定と、期待得点の係数をまとめたもの。
/// 値は過去のデータで選んで DB に保存する（分析の出力なので、リポジトリには入れない）。
/// </summary>
public sealed record ModelVersion
{
    private ModelVersion(Name name, EloSettings elo, GoalModelCoefficients coefficients, KickoffTime coefficientsDataCutoff)
    {
        Name = name;
        Elo = elo;
        Coefficients = coefficients;
        CoefficientsDataCutoff = coefficientsDataCutoff;
    }

    /// <summary>バージョンの名前（例: v1-2026-27）。予想に記録する。</summary>
    public Name Name { get; }

    public EloSettings Elo { get; }

    public GoalModelCoefficients Coefficients { get; }

    /// <summary>係数の推定に使ったデータの締め切り。この日時より前に始まった試合だけで推定した（GoalModelFitter に渡した値）。</summary>
    public KickoffTime CoefficientsDataCutoff { get; }

    public static ModelVersion Create(Name name, EloSettings elo, GoalModelCoefficients coefficients, KickoffTime coefficientsDataCutoff)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(elo);
        ArgumentNullException.ThrowIfNull(coefficients);
        ArgumentNullException.ThrowIfNull(coefficientsDataCutoff);
        return new ModelVersion(name, elo, coefficients, coefficientsDataCutoff);
    }
}
