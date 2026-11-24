using VerdyForecast.Domain.Common;

namespace VerdyForecast.Domain.Tags;

/// <summary>戦術タグのマスタ（最初のリストは作業指示書の D9 で決める）。</summary>
public sealed class Tag
{
    private Tag(Name name, TagKind kind)
    {
        Name = name;
        Kind = kind;
        IsActive = true;
    }

    public TagId Id { get; private set; }

    public Name Name { get; private set; }

    public TagKind Kind { get; private set; }

    /// <summary>使用中かどうか。過去のレビューに残っているので、消さずに使用中でなくする。</summary>
    public bool IsActive { get; private set; }

    public static Tag Create(Name name, TagKind kind)
    {
        ArgumentNullException.ThrowIfNull(name);
        Guard.Defined(kind);
        return new Tag(name, kind);
    }

    public void Rename(Name name)
    {
        ArgumentNullException.ThrowIfNull(name);
        Name = name;
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
