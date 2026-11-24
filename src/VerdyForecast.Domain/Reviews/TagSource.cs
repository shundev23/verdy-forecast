namespace VerdyForecast.Domain.Reviews;

/// <summary>試合に付けたタグを、どうやって選んだか。</summary>
public enum TagSource
{
    /// <summary>AI（AI Language）の候補から選んだ</summary>
    Suggested,

    /// <summary>自分で選んだ</summary>
    Manual,
}
