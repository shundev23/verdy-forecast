using System.Reflection;
using ArchUnitNET.Fluent.Syntax.Elements.Types;
using ArchUnitNET.Loader;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using Architecture = ArchUnitNET.Domain.Architecture;

namespace VerdyForecast.ArchitectureTests;

/// <summary>
/// 検査の対象（src の5つのアセンブリ）。読み込みに時間がかかるので、テスト全体で1回だけ読み込む。
/// </summary>
/// <remarks>
/// フィールド名に Assembly を付けているのは、名前空間 VerdyForecast.Domain などと衝突させないため
/// （VerdyForecast.ArchitectureTests の中では、Domain と書くと名前空間 VerdyForecast.Domain が優先される）。
/// </remarks>
internal static class ProjectArchitecture
{
    // 型がまだ1つもないプロジェクトもあるので、typeof ではなくアセンブリ名で読み込む。
    // 名前を間違えると Assembly.Load が例外を出すので、検査の対象が黙って0件になることはない。
    public static readonly Assembly DomainAssembly = Load("VerdyForecast.Domain");
    public static readonly Assembly ApplicationAssembly = Load("VerdyForecast.Application");
    public static readonly Assembly InfrastructureAssembly = Load("VerdyForecast.Infrastructure");
    public static readonly Assembly ApiAssembly = Load("VerdyForecast.Api");
    public static readonly Assembly FunctionsAssembly = Load("VerdyForecast.Functions");

    public static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(DomainAssembly, ApplicationAssembly, InfrastructureAssembly, ApiAssembly, FunctionsAssembly)
        .Build();

    /// <summary>
    /// Api と Functions の入口（トップレベルステートメントの Program と、その中にコンパイラーが作る型）。
    /// Infrastructure を参照してよいのは、ここでの DI の登録だけ。
    /// </summary>
    public const string ProgramTypePattern = @"^Program([+/].*)?$";

    /// <summary>指定したアセンブリにある型。</summary>
    public static GivenTypesConjunction TypesIn(Assembly assembly, params Assembly[] moreAssemblies) =>
        Types().That().ResideInAssembly(assembly, moreAssemblies);

    /// <summary>指定した名前空間（その下も含む）にある型。参照しているだけの外部の型も含める。</summary>
    public static GivenTypesConjunction ReferencedTypesInNamespaces(string first, params string[] more)
    {
        var types = Types(includeReferenced: true).That().ResideInNamespaceOrBelow(first);
        foreach (var ns in more)
        {
            types = types.Or().ResideInNamespaceOrBelow(ns);
        }

        return types;
    }

    private static Assembly Load(string name) => Assembly.Load(new AssemblyName(name));
}
