using ArchUnitNET.xUnitV3;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using static VerdyForecast.ArchitectureTests.ProjectArchitecture;

namespace VerdyForecast.ArchitectureTests;

/// <summary>
/// モデルの材料（作業指示書 2-8、脅威モデル T19）。
/// v1 のモデル（Elo、期待得点、予想）は、自分の採点・タグ・メモ・予想を材料にしない（使うかどうかの検証は v3）。
/// 答え合わせ（試合の結果のあとの計算）も、モデルの材料にしない。
/// </summary>
public class ModelInputTests
{
    [Fact]
    public void ForecastingAndRatings_DoNotUsePersonalRecordsOrEvaluation() =>
        Types().That().ResideInNamespaceOrBelow("VerdyForecast.Domain.Forecasting")
            .Or().ResideInNamespaceOrBelow("VerdyForecast.Domain.Ratings")
            .Should()
            .NotDependOnAny(ReferencedTypesInNamespaces(
                "VerdyForecast.Domain.Reviews",
                "VerdyForecast.Domain.Predictions",
                "VerdyForecast.Domain.Tags",
                "VerdyForecast.Domain.Evaluation"))
            .Because("v1 のモデルは、自分の採点・タグ・メモ・予想と、答え合わせの結果を材料にしない（データリーケージの防止）")
            .Check(Architecture);
}
