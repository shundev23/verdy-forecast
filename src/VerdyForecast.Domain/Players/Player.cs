using VerdyForecast.Domain.Common;

namespace VerdyForecast.Domain.Players;

/// <summary>ヴェルディの選手（選手名簿の1人）。</summary>
public sealed class Player
{
    private Player(Name name, ShirtNumber shirtNumber, Position position)
    {
        Name = name;
        ShirtNumber = shirtNumber;
        Position = position;
        IsActive = true;
    }

    public PlayerId Id { get; private set; }

    public Name Name { get; private set; }

    public ShirtNumber ShirtNumber { get; private set; }

    public Position Position { get; private set; }

    /// <summary>在籍中かどうか。退団しても過去の記録を残すため、消さずに在籍中でなくする。</summary>
    public bool IsActive { get; private set; }

    public static Player Register(Name name, ShirtNumber shirtNumber, Position position)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(shirtNumber);
        Guard.Defined(position);
        return new Player(name, shirtNumber, position);
    }

    public void Update(Name name, ShirtNumber shirtNumber, Position position)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(shirtNumber);
        Guard.Defined(position);
        Name = name;
        ShirtNumber = shirtNumber;
        Position = position;
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
