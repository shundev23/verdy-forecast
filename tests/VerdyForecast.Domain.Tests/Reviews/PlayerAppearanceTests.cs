using VerdyForecast.Domain.Common;
using VerdyForecast.Domain.Players;
using VerdyForecast.Domain.Reviews;
using VerdyForecast.Domain.Tests.TestSupport;

namespace VerdyForecast.Domain.Tests.Reviews;

public class PlayerAppearanceTests
{
    private static readonly PlayerId _playerId = new(7);

    [Fact]
    public void Create_StoresAllValues()
    {
        var appearance = PlayerAppearance.Create(_playerId, minutes: 78, goals: 2, assists: 1, yellowCards: 1, sentOff: false, ownGoals: 0).ShouldSucceed();

        appearance.PlayerId.ShouldBe(_playerId);
        appearance.Minutes.ShouldBe(78);
        appearance.Goals.ShouldBe(2);
        appearance.Assists.ShouldBe(1);
        appearance.YellowCards.ShouldBe(1);
        appearance.SentOff.ShouldBeFalse();
        appearance.OwnGoals.ShouldBe(0);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(90)]
    [InlineData(120)]
    public void Create_MinutesInRange_Succeeds(int minutes) =>
        Create(minutes: minutes).ShouldSucceed();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(121)]
    public void Create_MinutesOutOfRange_Fails(int minutes) =>
        Create(minutes: minutes).ShouldFailWith(ReviewErrors.MinutesOutOfRange);

    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(0, -1, 0)]
    [InlineData(0, 0, -1)]
    public void Create_NegativeCount_Fails(int goals, int assists, int ownGoals) =>
        Create(goals: goals, assists: assists, ownGoals: ownGoals).ShouldFailWith(ReviewErrors.NegativeCount);

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void Create_YellowCardsOutOfRange_Fails(int yellowCards) =>
        Create(yellowCards: yellowCards, sentOff: true).ShouldFailWith(ReviewErrors.YellowCardsOutOfRange);

    [Fact]
    public void Create_TwoYellowsWithoutSendingOff_Fails() =>
        Create(yellowCards: 2, sentOff: false).ShouldFailWith(ReviewErrors.SecondYellowWithoutSendingOff);

    [Fact]
    public void Create_TwoYellowsAndSentOff_Succeeds() =>
        Create(yellowCards: 2, sentOff: true).ShouldSucceed();

    [Fact]
    public void Create_StraightRedWithoutYellows_Succeeds() =>
        Create(yellowCards: 0, sentOff: true).ShouldSucceed();

    private static Result<PlayerAppearance> Create(
        int minutes = 90,
        int goals = 0,
        int assists = 0,
        int yellowCards = 0,
        bool sentOff = false,
        int ownGoals = 0) =>
        PlayerAppearance.Create(_playerId, minutes, goals, assists, yellowCards, sentOff, ownGoals);
}
