using ArchUnitNET.Domain.Dependencies;
using ArchUnitNET.Fluent.Conditions;
using ArchUnitNET.xUnitV3;
using static VerdyForecast.ArchitectureTests.ProjectArchitecture;
using IType = ArchUnitNET.Domain.IType;

namespace VerdyForecast.ArchitectureTests;

/// <summary>
/// 現在時刻は TimeProvider を受け取って使う（スキルの「ドメインのルールや計算を変えるとき」）。
/// 時刻を直接読むと、キックオフ前後の判定をテストで再現できなくなる。
/// </summary>
public class SystemClockTests
{
    private static readonly string[] _systemClockReads =
    [
        "System.DateTime::get_Now()",
        "System.DateTime::get_UtcNow()",
        "System.DateTime::get_Today()",
        "System.DateTimeOffset::get_Now()",
        "System.DateTimeOffset::get_UtcNow()",
        "System.TimeProvider::get_System()",
    ];

    [Fact]
    public void Domain_DoesNotReadSystemClockDirectly() =>
        TypesIn(DomainAssembly).Should()
            .FollowCustomCondition(DoesNotReadSystemClock, "現在時刻を TimeProvider から受け取る")
            .WithoutRequiringPositiveResults()
            .Check(Architecture);

    [Fact]
    public void Application_DoesNotReadSystemClockDirectly() =>
        TypesIn(ApplicationAssembly).Should()
            .FollowCustomCondition(DoesNotReadSystemClock, "現在時刻を TimeProvider から受け取る")
            .WithoutRequiringPositiveResults()
            .Check(Architecture);

    private static ConditionResult DoesNotReadSystemClock(IType type)
    {
        var reads = type.Dependencies
            .OfType<MethodCallDependency>()
            .Select(call => call.TargetMember.FullName)
            .Where(member => _systemClockReads.Any(read => member.Contains(read, StringComparison.Ordinal)))
            .Distinct()
            .Order(StringComparer.Ordinal)
            .ToList();

        return new ConditionResult(type, reads.Count == 0, "現在時刻を直接読んでいる: " + string.Join(", ", reads));
    }
}
