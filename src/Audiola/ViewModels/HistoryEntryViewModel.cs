namespace Audiola.ViewModels;

/// <summary>Ein Eintrag im Verlauf-Panel (ein Studio-Zustand).</summary>
public sealed record HistoryEntryViewModel(int Index, string Label, bool IsCurrent, bool IsUndone);
