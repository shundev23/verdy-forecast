using VerdyForecast.Domain.Tags;
using VerdyForecast.Domain.Tests.TestSupport;

namespace VerdyForecast.Domain.Tests.Tags;

public class TagTests
{
    [Fact]
    public void Create_IsActiveWithGivenValues()
    {
        var tag = Tag.Create(Make.NameOf("前線からの守備"), TagKind.Good);

        tag.Name.ShouldBe(Make.NameOf("前線からの守備"));
        tag.Kind.ShouldBe(TagKind.Good);
        tag.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Rename_ChangesName()
    {
        var tag = Tag.Create(Make.NameOf("前線からの守備"), TagKind.Good);

        tag.Rename(Make.NameOf("ハイプレス"));

        tag.Name.ShouldBe(Make.NameOf("ハイプレス"));
    }

    [Fact]
    public void DeactivateAndActivate_ChangeIsActive()
    {
        var tag = Tag.Create(Make.NameOf("セットプレーの失点"), TagKind.Bad);

        tag.Deactivate();
        tag.IsActive.ShouldBeFalse();

        tag.Activate();
        tag.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Create_UndefinedKind_Throws() =>
        Should.Throw<ArgumentOutOfRangeException>(() => Tag.Create(Make.NameOf("タグ"), (TagKind)99));
}
