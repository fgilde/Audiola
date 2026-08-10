using System.Collections.ObjectModel;
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

    /// <summary>Gefilterte Sicht auf die Variationen (Suchfeld) — eigene Liste statt
    /// <c>ICollectionView</c>, das es nur in WPF gibt.</summary>
    public ObservableCollection<VariationItem> VariationsView { get; } = [];

    public VariationPickerViewModel(IReadOnlyList<IAudioVariationProvider> providers, string scopeLabel)
    {
        Providers = providers;
        ScopeLabel = scopeLabel;
        SelectedProvider = providers.FirstOrDefault();
    }

    partial void OnSelectedProviderChanged(IAudioVariationProvider? value)
    {
        Variations.Clear();
        if (value is not null)
            foreach (var v in value.GetVariations())
                Variations.Add(new VariationItem { Variation = v });
        RefreshView();
    }

    partial void OnSearchTextChanged(string value) => RefreshView();

    private void RefreshView()
    {
        VariationsView.Clear();
        foreach (var v in Variations.Where(FilterVariation))
            VariationsView.Add(v);
    }

    private bool FilterVariation(VariationItem vi)
    {
        var q = SearchText?.Trim();
        if (string.IsNullOrEmpty(q)) return true;
        return vi.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
            || vi.Description.Contains(q, StringComparison.OrdinalIgnoreCase);
    }

    public IReadOnlyList<string> SelectedVariationIds =>
        Variations.Where(v => v.IsChecked).Select(v => v.Variation.Id).ToList();
}
