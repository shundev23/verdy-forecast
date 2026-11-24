using VerdyForecast.Domain.Teams;
using VerdyForecast.Domain.Tests.TestSupport;

namespace VerdyForecast.Domain.Tests.Teams;

public class TeamTests
{
    [Fact]
    public void Create_HasName() => Team.Create(Make.NameOf("チームA")).Name.ShouldBe(Make.NameOf("チームA"));

    [Fact]
    public void Rename_ChangesName()
    {
        var team = Team.Create(Make.NameOf("チームA"));

        team.Rename(Make.NameOf("チームB"));

        team.Name.ShouldBe(Make.NameOf("チームB"));
    }
}
