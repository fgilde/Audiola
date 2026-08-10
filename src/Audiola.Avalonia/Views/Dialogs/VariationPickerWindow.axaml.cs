using Audiola.Services;
using Audiola.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Audiola.Avalonia.Views.Dialogs;

public partial class VariationPickerWindow : Window
{
    private readonly VariationPickerViewModel _vm;

    /// <summary>Parameterloser Konstruktor nur für den XAML-Vorschau-Compiler.</summary>
    public VariationPickerWindow() : this([], "") { }

    public VariationPickerWindow(IReadOnlyList<IAudioVariationProvider> providers, string scopeLabel)
    {
        _vm = new VariationPickerViewModel(providers, scopeLabel);
        DataContext = _vm;
        InitializeComponent();
    }

    /// <summary>Gesetzt, wenn der Nutzer „Anwenden" gewählt hat (sonst null).</summary>
    public VariationChoice? Result { get; private set; }

    private void Apply_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm.SelectedProvider is { } provider && _vm.SelectedVariationIds.Count > 0)
            Result = new VariationChoice(provider, _vm.SelectedVariationIds);
        Close();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close();
}
