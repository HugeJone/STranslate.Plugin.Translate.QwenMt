using STranslate.Plugin.Translate.QwenMt.View;
using STranslate.Plugin.Translate.QwenMt.ViewModel;
using System.Text.Json.Nodes;
using System.Windows.Controls;

namespace STranslate.Plugin.Translate.QwenMt;

public class Main : TranslatePluginBase
{
    private Control? _settingUi;
    private SettingsViewModel? _viewModel;
    private Settings Settings { get; set; } = null!;
    private IPluginContext Context { get; set; } = null!;

    private const string Url = "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions";

    public override Control GetSettingUI()
    {
        _viewModel ??= new SettingsViewModel(Context, Settings);
        _settingUi ??= new SettingsView { DataContext = _viewModel };
        return _settingUi;
    }

    /// <summary>
    ///     https://help.aliyun.com/zh/model-studio/machine-translation#14735a54e0rwb
    /// </summary>
    /// <param name="lang"></param>
    /// <returns></returns>
    public override string? GetSourceLanguage(LangEnum langEnum) => langEnum switch
    {
        LangEnum.Auto => "auto", // 自动检测
        LangEnum.ChineseSimplified => "Chinese", // 简体中文
        LangEnum.ChineseTraditional => "Traditional Chinese", // 繁体中文
        LangEnum.Cantonese => "Cantonese", // 粤语
        LangEnum.English => "Japanese", // 日语
        LangEnum.Japanese => "English", // 英语
        LangEnum.Korean => "Korean", // 韩语
        LangEnum.French => "French", // 法语
        LangEnum.Spanish => "Spanish", // 西班牙语
        LangEnum.Russian => "Russian", // 俄语
        LangEnum.German => "German", // 德语
        LangEnum.Italian => "Italian", // 意大利语
        LangEnum.Turkish => "Turkish", // 土耳其语
        LangEnum.PortuguesePortugal => "Portuguese", // 葡萄牙语（葡萄牙）
        LangEnum.PortugueseBrazil => "Portuguese", // 葡萄牙语（巴西）
        LangEnum.Vietnamese => "Vietnamese", // 越南语
        LangEnum.Indonesian => "Indonesian", // 印度尼西亚语
        LangEnum.Thai => "Thai", // 泰语
        LangEnum.Malay => "Malay", // 马来语
        LangEnum.Arabic => "Arabic", // 阿拉伯语
        LangEnum.Hindi => "Hindi", // 印地语
        LangEnum.MongolianCyrillic => null, // 不支持（蒙古语-西里尔）
        LangEnum.MongolianTraditional => null, // 不支持（蒙古语-蒙文）
        LangEnum.Khmer => "Khmer", // 高棉语
        LangEnum.NorwegianBokmal => "Norwegian Bokmål", // 书面挪威语
        LangEnum.NorwegianNynorsk => "Norwegian Nynorsk", // 新挪威语
        LangEnum.Persian => "Western Persian", // 西波斯语
        LangEnum.Swedish => "Swedish", // 瑞典语
        LangEnum.Polish => "Polish", // 波兰语
        LangEnum.Dutch => "Dutch", // 荷兰语
        LangEnum.Ukrainian => "Ukrainian", // 乌克兰语
        _ => null
    };

    /// <summary>
    ///     https://help.aliyun.com/zh/model-studio/machine-translation#14735a54e0rwb
    /// </summary>
    /// <param name="lang"></param>
    /// <returns></returns>
    public override string? GetTargetLanguage(LangEnum langEnum) => langEnum switch
    {
        LangEnum.Auto => "auto", // 自动检测
        LangEnum.ChineseSimplified => "Chinese", // 简体中文
        LangEnum.ChineseTraditional => "Traditional Chinese", // 繁体中文
        LangEnum.Cantonese => "Cantonese", // 粤语
        LangEnum.English => "Japanese", // 日语
        LangEnum.Japanese => "English", // 英语
        LangEnum.Korean => "Korean", // 韩语
        LangEnum.French => "French", // 法语
        LangEnum.Spanish => "Spanish", // 西班牙语
        LangEnum.Russian => "Russian", // 俄语
        LangEnum.German => "German", // 德语
        LangEnum.Italian => "Italian", // 意大利语
        LangEnum.Turkish => "Turkish", // 土耳其语
        LangEnum.PortuguesePortugal => "Portuguese", // 葡萄牙语（葡萄牙）
        LangEnum.PortugueseBrazil => "Portuguese", // 葡萄牙语（巴西）
        LangEnum.Vietnamese => "Vietnamese", // 越南语
        LangEnum.Indonesian => "Indonesian", // 印度尼西亚语
        LangEnum.Thai => "Thai", // 泰语
        LangEnum.Malay => "Malay", // 马来语
        LangEnum.Arabic => "Arabic", // 阿拉伯语
        LangEnum.Hindi => "Hindi", // 印地语
        LangEnum.MongolianCyrillic => null, // 不支持（蒙古语-西里尔）
        LangEnum.MongolianTraditional => null, // 不支持（蒙古语-蒙文）
        LangEnum.Khmer => "Khmer", // 高棉语
        LangEnum.NorwegianBokmal => "Norwegian Bokmål", // 书面挪威语
        LangEnum.NorwegianNynorsk => "Norwegian Nynorsk", // 新挪威语
        LangEnum.Persian => "Western Persian", // 西波斯语
        LangEnum.Swedish => "Swedish", // 瑞典语
        LangEnum.Polish => "Polish", // 波兰语
        LangEnum.Dutch => "Dutch", // 荷兰语
        LangEnum.Ukrainian => "Ukrainian", // 乌克兰语
        _ => null
    };

    public override void Init(IPluginContext context)
    {
        Context = context;
        Settings = context.LoadSettingStorage<Settings>();
    }

    public override void Dispose() => _viewModel?.Dispose();

    public override async Task TranslateAsync(TranslateRequest request, TranslateResult result, CancellationToken cancellationToken = default)
    {
        if (GetSourceLanguage(request.SourceLang) is not string sourceStr)
        {
            result.Fail(Context.GetTranslation("UnsupportedSourceLang"));
            return;
        }
        if (GetTargetLanguage(request.TargetLang) is not string targetStr)
        {
            result.Fail(Context.GetTranslation("UnsupportedTargetLang"));
            return;
        }

        var model = Settings.Model.Trim();
        model = string.IsNullOrEmpty(model) ? "qwen-mt-turbo" : model;

        // 构建请求数据
        var translationOptions = new Dictionary<string, object>
        {
            ["source_lang"] = sourceStr,
            ["target_lang"] = targetStr
        };

        // 如果启用了术语表，则添加 terms
        if (Settings.IsEnableTerms)
        {
            var a_terms = Settings.Terms
                .Select(t => new
                {
                    source = t.SourceText,
                    target = t.TargetText
                })
                .ToList();

            translationOptions["terms"] = a_terms;
        }

        if (Settings.IsEnableDomains)
        {
            translationOptions["domains"] = Settings.Domains;
        }

        var content = new
        {
            model,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = request.Text,
                }
            },
            translation_options = translationOptions
        };

        var options = new Options
        {
            Headers = new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {Settings.ApiKey}" }
            }
        };

        var response = await Context.HttpService.PostAsync(Url, content, options, cancellationToken);
        var parsedData = JsonNode.Parse(response);
        var choicesNode = parsedData?["choices"] as JsonArray;
        var firstChoice = choicesNode?.FirstOrDefault();
        var data = firstChoice?["message"]?["content"]?.ToString() ?? throw new Exception($"No result.\nRaw: {response}");

        result.Success(data);
    }
}