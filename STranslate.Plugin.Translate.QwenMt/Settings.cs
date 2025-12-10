using CommunityToolkit.Mvvm.ComponentModel;

namespace STranslate.Plugin.Translate.QwenMt;

public class Settings
{
    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "qwen-mt-turbo";

    public List<string> Models { get; set; } =
    [
        "qwen-mt-turbo",
        "qwen-mt-plus"
    ];

    public bool IsEnableTerms { get; set; }

    public bool IsEnableDomains { get; set; }

    /// <summary>
    ///     术语列表
    /// </summary>
    public List<Term> Terms { get; set; } = [];

    /// <summary>
    ///     领域提示
    /// </summary>
    public string Domains { get; set; } = string.Empty;
}

public partial class Term : ObservableObject
{
    [ObservableProperty] public partial string SourceText { get; set; } = string.Empty;

    [ObservableProperty] public partial string TargetText { get; set; } = string.Empty;
}