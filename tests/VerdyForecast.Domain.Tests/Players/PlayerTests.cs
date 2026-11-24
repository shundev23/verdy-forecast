using VerdyForecast.Domain.Players;
using VerdyForecast.Domain.Tests.TestSupport;

namespace VerdyForecast.Domain.Tests.Players;

public class PlayerTests
{
    [Fact]
    public void Register_IsActiveWithGivenValues()
    {
        var player = Player.Register(Make.NameOf("選手A"), Make.ShirtNumberOf(10), Position.Midfielder);

        player.Name.ShouldBe(Make.NameOf("選手A"));
        player.ShirtNumber.ShouldBe(Make.ShirtNumberOf(10));
        player.Position.ShouldBe(Position.Midfielder);
        player.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Update_ChangesValues()
    {
        var player = Player.Register(Make.NameOf("選手A"), Make.ShirtNumberOf(10), Position.Midfielder);

        player.Update(Make.NameOf("選手B"), Make.ShirtNumberOf(9), Position.Forward);

        player.Name.ShouldBe(Make.NameOf("選手B"));
        player.ShirtNumber.ShouldBe(Make.ShirtNumberOf(9));
        player.Position.ShouldBe(Position.Forward);
    }

    [Fact]
    public void DeactivateAndActivate_ChangeIsActive()
    {
        var player = Player.Register(Make.NameOf("選手A"), Make.ShirtNumberOf(10), Position.Midfielder);

        player.Deactivate();
        player.IsActive.ShouldBeFalse();

        player.Activate();
        player.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Register_UndefinedPosition_Throws() =>
        Should.Throw<ArgumentOutOfRangeException>(() => Player.Register(Make.NameOf("選手A"), Make.ShirtNumberOf(10), (Position)99));
}
