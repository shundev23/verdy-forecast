using static VerdyForecast.ArchitectureTests.ProjectArchitecture;

namespace VerdyForecast.ArchitectureTests;

/// <summary>
/// 依存ルールのテストの前提。検査の対象が読み込めていないと、ほかのテストが「対象0件で成功」になってしまう。
/// </summary>
public class ArchitectureLoadingTests
{
    [Theory]
    [InlineData("VerdyForecast.Domain")]
    [InlineData("VerdyForecast.Application")]
    [InlineData("VerdyForecast.Infrastructure")]
    [InlineData("VerdyForecast.Api")]
    [InlineData("VerdyForecast.Functions")]
    public void Architecture_ContainsSourceAssembly(string assemblyName) =>
        Assert.Contains(
            Architecture.Assemblies,
            assembly => assembly.FullName.StartsWith(assemblyName + ",", StringComparison.Ordinal));
}
