using ArchUnitNET.Fluent.Conditions;
using ArchUnitNET.xUnitV3;
using static VerdyForecast.ArchitectureTests.ProjectArchitecture;
using GenericParameter = ArchUnitNET.Domain.GenericParameter;
using IType = ArchUnitNET.Domain.IType;

namespace VerdyForecast.ArchitectureTests;

/// <summary>
/// 層ごとに使ってよい外部のもの（スキルの architecture.md「置かないもの」）。
/// </summary>
public class ExternalDependencyTests
{
    // ArchUnitNET の OnlyDependOn は、読み込んだアセンブリの外の型を判定に使わないため、外部ライブラリを検出できない。
    // そのため、依存先を1つずつ自分で判定する。
    [Fact]
    public void Domain_DependsOnlyOnItselfAndDotNetBaseLibrary() =>
        TypesIn(DomainAssembly).Should()
            .FollowCustomCondition(DependsOnlyOnDomainOrSystem, "Domain 自身と .NET の標準ライブラリ（System 名前空間）だけに依存する")
            .Because("Domain は .NET の標準ライブラリ以外に依存しない（外部ライブラリを持ち込まない）")
            .WithoutRequiringPositiveResults()
            .Check(Architecture);

    [Fact]
    public void Domain_DoesNotUseFilesNetworkOrDatabase() =>
        TypesIn(DomainAssembly).Should()
            .NotDependOnAny(ReferencedTypesInNamespaces("System.IO", "System.Net", "System.Data"))
            .Because("Domain はファイル、通信、DB を扱わない（I/O はポートを通して Infrastructure で行う）")
            .WithoutRequiringPositiveResults()
            .Check(Architecture);

    [Fact]
    public void Application_DoesNotUseInfrastructureTechnologies() =>
        TypesIn(ApplicationAssembly).Should()
            .NotDependOnAny(ReferencedTypesInNamespaces(
                "Microsoft.EntityFrameworkCore",
                "Microsoft.Data.SqlClient",
                "System.Data",
                "Azure",
                "Microsoft.Azure",
                "System.Net.Http",
                "Microsoft.AspNetCore"))
            .Because("Application は EF Core、Azure SDK、HttpClient、ASP.NET Core の具体的な実装を使わない（ポートだけを使う）")
            .WithoutRequiringPositiveResults()
            .Check(Architecture);

    private static ConditionResult DependsOnlyOnDomainOrSystem(IType type)
    {
        var outside = type.Dependencies
            .Select(dependency => dependency.Target)
            .Where(target => target is not GenericParameter && !IsInDomainOrSystem(target))
            .Select(target => target.FullName)
            .Distinct()
            .Order(StringComparer.Ordinal)
            .ToList();

        return new ConditionResult(type, outside.Count == 0, "次の型に依存している: " + string.Join(", ", outside));
    }

    private static bool IsInDomainOrSystem(IType target)
    {
        if (target.Assembly.FullName == DomainAssembly.FullName)
        {
            return true;
        }

        var ns = target.Namespace.FullName;
        return ns == "System" || ns.StartsWith("System.", StringComparison.Ordinal);
    }
}
