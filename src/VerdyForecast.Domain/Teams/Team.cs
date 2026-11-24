using VerdyForecast.Domain.Common;

namespace VerdyForecast.Domain.Teams;

/// <summary>J1 のチーム。</summary>
public sealed class Team
{
    private Team(Name name) => Name = name;

    public TeamId Id { get; private set; }

    public Name Name { get; private set; }

    public static Team Create(Name name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return new Team(name);
    }

    public void Rename(Name name)
    {
        ArgumentNullException.ThrowIfNull(name);
        Name = name;
    }
}
