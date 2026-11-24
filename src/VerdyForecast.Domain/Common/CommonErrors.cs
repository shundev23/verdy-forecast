namespace VerdyForecast.Domain.Common;

/// <summary>複数の場所で使う値の失敗。</summary>
public static class CommonErrors
{
    public static readonly DomainError NameRequired = new("name.required", "名前を入力してください。");

    public static readonly DomainError NameTooLong = new("name.tooLong", "名前は50文字以内にしてください。");
}
