using VerdyForecast.Domain.Common;
using VerdyForecast.Domain.Tests.TestSupport;

namespace VerdyForecast.Domain.Tests.Common;

public class NameTests
{
    [Fact]
    public void Create_WithSurroundingSpaces_TrimsValue() =>
        Name.Create("  東京ヴェルディ  ").ShouldSucceed().Value.ShouldBe("東京ヴェルディ");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Empty_FailsWithRequired(string? value) =>
        Name.Create(value).ShouldFailWith(CommonErrors.NameRequired);

    [Fact]
    public void Create_MaxLength_Succeeds() =>
        Name.Create(new string('あ', Name.MaxLength)).ShouldSucceed().Value.Length.ShouldBe(50);

    [Fact]
    public void Create_MaxLengthWithSurroundingSpaces_Succeeds() =>
        Name.Create(" " + new string('あ', Name.MaxLength) + " ").ShouldSucceed().Value.Length.ShouldBe(50);

    [Fact]
    public void Create_OverMaxLength_FailsWithTooLong() =>
        Name.Create(new string('あ', Name.MaxLength + 1)).ShouldFailWith(CommonErrors.NameTooLong);

    [Fact]
    public void ToString_ReturnsValue() => Make.NameOf("東京ヴェルディ").ToString().ShouldBe("東京ヴェルディ");
}
