using VerdyForecast.Domain.Players;
using VerdyForecast.Domain.Reviews;
using VerdyForecast.Domain.Tests.TestSupport;

namespace VerdyForecast.Domain.Tests.Reviews;

// 期待値は作業指示書 D6 の表から手で計算した値。属性に decimal は書けないので double で書く
public class ReferenceScoreRuleTests
{
    private static readonly PlayerId _playerId = new(1);

    [Fact]
    public void CalculateV1_FourteenMinutes_ReturnsNull() =>
        Calculate(Make.AppearanceOf(_playerId, minutes: 14), Position.Midfielder, 1, 1).ShouldBeNull();

    [Fact]
    public void CalculateV1_FifteenMinutes_ReturnsBase() =>
        Calculate(Make.AppearanceOf(_playerId, minutes: 15), Position.Midfielder, 1, 1).ShouldBe(6.0m);

    [Theory]
    [InlineData(1, 0, 6.5)]
    [InlineData(1, 1, 6.0)]
    [InlineData(0, 1, 5.5)]
    public void CalculateV1_TeamResult_AddsOrSubtractsHalf(int goalsFor, int goalsAgainst, double expected) =>
        Calculate(Make.AppearanceOf(_playerId), Position.Midfielder, goalsFor, goalsAgainst).ShouldBe((decimal)expected);

    [Theory]
    [InlineData(2, 0, 8.0)]
    [InlineData(0, 1, 6.5)]
    [InlineData(1, 1, 7.5)]
    public void CalculateV1_GoalsAndAssists_AddOneAndHalf(int goals, int assists, double expected) =>
        Calculate(Make.AppearanceOf(_playerId, goals: goals, assists: assists), Position.Forward, 2, 2).ShouldBe((decimal)expected);

    [Theory]
    [InlineData(Position.Defender, 60, 6.5)]
    [InlineData(Position.Defender, 59, 6.0)]
    [InlineData(Position.Goalkeeper, 90, 6.5)]
    [InlineData(Position.Midfielder, 90, 6.0)]
    [InlineData(Position.Forward, 90, 6.0)]
    public void CalculateV1_CleanSheet_AddsHalfForGoalkeeperAndDefenderPlayingSixtyMinutes(Position position, int minutes, double expected) =>
        Calculate(Make.AppearanceOf(_playerId, minutes: minutes), position, 0, 0).ShouldBe((decimal)expected);

    [Theory]
    [InlineData(Position.Defender, 3, 5.5)]
    [InlineData(Position.Goalkeeper, 4, 5.5)]
    [InlineData(Position.Defender, 2, 6.0)]
    [InlineData(Position.Midfielder, 3, 6.0)]
    public void CalculateV1_ThreeOrMoreConceded_SubtractsHalfForGoalkeeperAndDefender(Position position, int conceded, double expected) =>
        Calculate(Make.AppearanceOf(_playerId), position, conceded, conceded).ShouldBe((decimal)expected);

    [Fact]
    public void CalculateV1_SentOff_SubtractsOne() =>
        Calculate(Make.AppearanceOf(_playerId, yellowCards: 2, sentOff: true), Position.Midfielder, 1, 1).ShouldBe(5.0m);

    [Theory]
    [InlineData(1, 5.0)]
    [InlineData(2, 4.0)]
    public void CalculateV1_OwnGoals_SubtractOneEach(int ownGoals, double expected) =>
        Calculate(Make.AppearanceOf(_playerId, ownGoals: ownGoals), Position.Defender, 2, 2).ShouldBe((decimal)expected);

    [Fact]
    public void CalculateV1_OneYellowCard_HasNoEffect() =>
        Calculate(Make.AppearanceOf(_playerId, yellowCards: 1), Position.Midfielder, 1, 1).ShouldBe(6.0m);

    [Theory]
    [InlineData(3, 3, 3, 9.0)]
    [InlineData(4, 4, 0, 9.0)]
    public void CalculateV1_HighScore_IsCappedAtNine(int goals, int goalsFor, int goalsAgainst, double expected) =>
        Calculate(Make.AppearanceOf(_playerId, goals: goals), Position.Forward, goalsFor, goalsAgainst).ShouldBe((decimal)expected);

    [Fact]
    public void CalculateV1_ExactlyFour_IsKept() =>
        Calculate(Make.AppearanceOf(_playerId, yellowCards: 2, sentOff: true, ownGoals: 1), Position.Midfielder, 1, 1).ShouldBe(4.0m);

    [Fact]
    public void CalculateV1_LowScore_IsRaisedToFour() =>
        Calculate(Make.AppearanceOf(_playerId, sentOff: true, ownGoals: 2), Position.Defender, 0, 3).ShouldBe(4.0m);

    [Fact]
    public void CalculateV1_UndefinedPosition_Throws() =>
        Should.Throw<ArgumentOutOfRangeException>(() =>
            ReferenceScoreRule.CalculateV1(Make.AppearanceOf(_playerId), (Position)99, Make.TeamScoreOf(1, 1)));

    private static decimal? Calculate(PlayerAppearance appearance, Position position, int goalsFor, int goalsAgainst) =>
        ReferenceScoreRule.CalculateV1(appearance, position, Make.TeamScoreOf(goalsFor, goalsAgainst))?.Value;
}
