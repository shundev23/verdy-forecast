using ArchUnitNET.xUnitV3;
using static VerdyForecast.ArchitectureTests.ProjectArchitecture;

namespace VerdyForecast.ArchitectureTests;

/// <summary>
/// 層の依存の向き（スキルの architecture.md「層と依存のルール」）。
/// 逆向きの参照の多くは、プロジェクト参照が循環してビルドが通らなくなるが、ここでも確かめる。
/// 今は中身が空の層もあるので、対象が0件でも失敗にしない（読み込みは ArchitectureLoadingTests で確かめる）。
/// </summary>
public class LayerDependencyTests
{
    [Fact]
    public void Domain_DoesNotDependOnOtherLayers() =>
        TypesIn(DomainAssembly).Should()
            .NotDependOnAny(TypesIn(ApplicationAssembly, InfrastructureAssembly, ApiAssembly, FunctionsAssembly))
            .Because("Domain はほかのどの層にも依存しない")
            .WithoutRequiringPositiveResults()
            .Check(Architecture);

    [Fact]
    public void Application_DoesNotDependOnInfrastructureOrEntryPoints() =>
        TypesIn(ApplicationAssembly).Should()
            .NotDependOnAny(TypesIn(InfrastructureAssembly, ApiAssembly, FunctionsAssembly))
            .Because("Application が参照してよいのは Domain だけ")
            .WithoutRequiringPositiveResults()
            .Check(Architecture);

    [Fact]
    public void Infrastructure_DoesNotDependOnEntryPoints() =>
        TypesIn(InfrastructureAssembly).Should()
            .NotDependOnAny(TypesIn(ApiAssembly, FunctionsAssembly))
            .Because("Infrastructure が参照してよいのは Application と Domain だけ")
            .WithoutRequiringPositiveResults()
            .Check(Architecture);

    [Fact]
    public void Api_DoesNotDependOnFunctions() =>
        TypesIn(ApiAssembly).Should()
            .NotDependOnAny(TypesIn(FunctionsAssembly))
            .Because("2つの入口（Api と Functions）は、お互いを参照しない")
            .WithoutRequiringPositiveResults()
            .Check(Architecture);

    [Fact]
    public void Functions_DoesNotDependOnApi() =>
        TypesIn(FunctionsAssembly).Should()
            .NotDependOnAny(TypesIn(ApiAssembly))
            .Because("2つの入口（Api と Functions）は、お互いを参照しない")
            .WithoutRequiringPositiveResults()
            .Check(Architecture);

    [Fact]
    public void Api_UsesInfrastructureOnlyInProgram() =>
        TypesIn(ApiAssembly).And().DoNotHaveFullNameMatching(ProgramTypePattern).Should()
            .NotDependOnAny(TypesIn(InfrastructureAssembly))
            .Because("Api が Infrastructure を使ってよいのは、Program.cs での DI の登録だけ")
            .WithoutRequiringPositiveResults()
            .Check(Architecture);

    [Fact]
    public void Functions_UsesInfrastructureOnlyInProgram() =>
        TypesIn(FunctionsAssembly).And().DoNotHaveFullNameMatching(ProgramTypePattern).Should()
            .NotDependOnAny(TypesIn(InfrastructureAssembly))
            .Because("Functions が Infrastructure を使ってよいのは、Program.cs での DI の登録だけ")
            .WithoutRequiringPositiveResults()
            .Check(Architecture);
}
