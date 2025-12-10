using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using ObservableCollections;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.Json;

namespace STranslate.Plugin.Translate.QwenMt.ViewModel;

public partial class SettingsViewModel : ObservableObject, IDisposable
{
    private readonly IPluginContext _context;
    private readonly Settings _settings;
    private bool _isUpdating = false;

    public SettingsViewModel(IPluginContext context, Settings settings)
    {
        _context = context;
        _settings = settings;

        ApiKey = settings.ApiKey;
        Model = settings.Model;
        Models = [.. settings.Models];
        IsEnableTerms = settings.IsEnableTerms;
        IsEnableDomains = settings.IsEnableDomains;
        Domains = settings.Domains;
        _items = [.. settings.Terms];
        Terms = _items.ToNotifyCollectionChanged();

        PropertyChanged += OnPropertyChanged;
        Models.CollectionChanged += OnModelsCollectionChanged;
        _items.CollectionChanged += OnTermsCollectionChanged;

        foreach (var item in _items)
        {
            item.PropertyChanged += OnTermPropertyChanged;
        }
    }

    private void OnTermPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // 当 Term 的属性发生变化时保存设置
        _settings.Terms = [.. _items];
        _context.SaveSettingStorage<Settings>();
    }

    private void OnTermsCollectionChanged(in NotifyCollectionChangedEventArgs<Term> e)
    {
        e.NewItem?.PropertyChanged += OnTermPropertyChanged;
        e.OldItem?.PropertyChanged -= OnTermPropertyChanged;
        foreach (var item in e.NewItems)
        {
            item.PropertyChanged += OnTermPropertyChanged;
        }
        foreach (var item in e.OldItems)
        {
            item.PropertyChanged -= OnTermPropertyChanged;
        }
        _settings.Terms = [.. _items];
        _context.SaveSettingStorage<Settings>();
    }

    private void OnModelsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action is NotifyCollectionChangedAction.Add or
                       NotifyCollectionChangedAction.Remove or
                       NotifyCollectionChangedAction.Replace)
        {
            _settings.Models = [.. Models];
            _context.SaveSettingStorage<Settings>();
        }
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ApiKey):
                _settings.ApiKey = ApiKey;
                break;
            case nameof(Model):
                _settings.Model = Model ?? string.Empty;
                break;
            case nameof(IsEnableTerms):
                _settings.IsEnableTerms = IsEnableTerms;
                break;
            case nameof(IsEnableDomains):
                _settings.IsEnableDomains = IsEnableDomains;
                break;
            case nameof(Domains):
                _settings.Domains = Domains;
                break;
            default:
                return;
        }
        _context.SaveSettingStorage<Settings>();
    }

    [RelayCommand]
    private void AddModel(string model)
    {
        if (_isUpdating || string.IsNullOrWhiteSpace(model) || Models.Contains(model))
            return;

        using var _ = new UpdateGuard(this);

        Models.Add(model);
        Model = model;
    }

    [RelayCommand]
    private void DeleteModel(string model)
    {
        if (_isUpdating || !Models.Contains(model))
            return;

        using var _ = new UpdateGuard(this);

        if (Model == model)
            Model = Models.Count > 1 ? Models.First(m => m != model) : string.Empty;

        Models.Remove(model);
    }

    [RelayCommand]
    private void TermsAdd()
    {
        _items.Add(new Term
        {
            SourceText = string.Empty,
            TargetText = string.Empty
        });
    }

    [RelayCommand]
    private void TermsDelete(IList list)
    {
        if (list.Count == 0)
            return;

        var tmp = list.Cast<Term>().ToList();

        foreach (var item in tmp)
        {
            _items.Remove(item);
        }
    }

    [RelayCommand]
    private void TermsClear()
    {
        if (_items.Count == 0)
            return;

        _items.Clear();
    }

    [RelayCommand]
    private void TermsExport()
    {
        try
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                FileName = "qwen_terms.json",
                DefaultExt = "json"
            };

            if (saveFileDialog.ShowDialog() != true)
                return;

            var json = JsonSerializer.Serialize(_items, options);

            File.WriteAllText(saveFileDialog.FileName, json, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            _context.Logger.LogError(ex, $"Failed to export terms: {ex.Message}");
        }
    }

    [RelayCommand]
    private void TermsImport()
    {
        try
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            var json = File.ReadAllText(openFileDialog.FileName, Encoding.UTF8);
            var terms = JsonSerializer.Deserialize<IEnumerable<Term>>(json);

            if (terms != null)
            {
                _items.Clear();
                _items.AddRange(terms);
            }
        }
        catch (Exception ex)
        {
            _context.Logger.LogError(ex, $"Failed to import terms: {ex.Message}");
        }
    }

    public void Dispose()
    {
        PropertyChanged -= OnPropertyChanged;
        Models.CollectionChanged -= OnModelsCollectionChanged;
        _items.CollectionChanged -= OnTermsCollectionChanged;

        foreach (var item in _items)
        {
            item.PropertyChanged -= OnTermPropertyChanged;
        }
    }

    private static readonly JsonSerializerOptions options = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly struct UpdateGuard : IDisposable
    {
        private readonly SettingsViewModel _viewModel;

        public UpdateGuard(SettingsViewModel viewModel)
        {
            _viewModel = viewModel;
            _viewModel._isUpdating = true;
        }

        public void Dispose() => _viewModel._isUpdating = false;
    }

    [ObservableProperty] public partial string ApiKey { get; set; }

    [ObservableProperty] public partial string Model { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<string> Models { get; set; }

    [ObservableProperty] public partial bool IsEnableTerms { get; set; }

    [ObservableProperty] public partial bool IsEnableDomains { get; set; }

    /// <summary>
    ///     术语列表
    /// </summary>
    private readonly ObservableList<Term> _items;

    public INotifyCollectionChangedSynchronizedViewList<Term> Terms { get; }

    /// <summary>
    ///     领域提示
    /// </summary>
    [ObservableProperty] public partial string Domains { get; set; }
}