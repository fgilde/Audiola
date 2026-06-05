using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using Audiola.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Audiola.ViewModels;

/// <summary>Eine auswählbare Variation im Dialog (Variation + Häkchen).</summary>
public sealed partial class VariationItem : ObservableObject
{
    public required AudioVariation Variation { get; init; }
    [ObservableProperty] private bool _isChecked;
    public string Name => Variation.Name;
    public string Description => Variation.Description;
}

/// <summary>Dialog-VM: Provider wählen, eine oder mehrere Variationen ankreuzen.</summary>
public sealed partial class VariationPickerViewModel : ObservableObject
{
    public IReadOnlyList<IAudioVariationProvider> Providers { get; }
    public string ScopeLabel { get; }

    [ObservableProperty] private IAudioVariationProvider? _selectedProvider;
    [ObservableProperty] private string _searchText = "";

    public ObservableCollection<VariationItem> Variations { get; } = [];

    /// <summary>Gefilterte Sicht auf die Variationen (Suchfeld).</summary>
    public ICollectionView VariationsView { get; }

    public VariationPickerViewModel(IReadOnlyList<IAudioVariationProvider> providers, string scopeLabel)
    {
        Providers = providers;
        ScopeLabel = scopeLabel;
        VariationsView = CollectionViewSource.GetDefaultView(Variations);
        VariationsView.Filter = FilterVariation;
        SelectedProvider = providers.FirstOrDefault();
    }

    partial void OnSelectedProviderChanged(IAudioVariationProvider? value)
    {
        Variations.Clear();
        if (value is not null)
            foreach (var v in value.GetVariations())
                Variations.Add(new VariationItem { Variation = v });
        VariationsView.Refresh();
    }

    partial void OnSearchTextChanged(string value) => VariationsView.Refresh();

    private bool FilterVariation(object obj)
    {
        if (obj is not VariationItem vi) return false;
        var q = SearchText?.Trim();
        if (string.IsNullOrEmpty(q)) return true;
        return vi.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
            || vi.Description.Contains(q, StringComparison.OrdinalIgnoreCase);
    }

    public IReadOnlyList<string> SelectedVariationIds =>
        Variations.Where(v => v.IsChecked).Select(v => v.Variation.Id).ToList();
}
