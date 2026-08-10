namespace Audiola.Avalonia.ViewModels;

public abstract class WorkspacePageViewModel
{
    protected WorkspacePageViewModel(AudiolaHostViewModel parent) => Parent = parent;
    public AudiolaHostViewModel Parent { get; }
}

public sealed class HomePageViewModel(AudiolaHostViewModel parent) : WorkspacePageViewModel(parent);
public sealed class EditorPageViewModel(AudiolaHostViewModel parent) : WorkspacePageViewModel(parent);
public sealed class TimelinePageViewModel(AudiolaHostViewModel parent) : WorkspacePageViewModel(parent);
public sealed class EqualizerPageViewModel(AudiolaHostViewModel parent) : WorkspacePageViewModel(parent);
public sealed class MasteringPageViewModel(AudiolaHostViewModel parent) : WorkspacePageViewModel(parent);
public sealed class SpatialAudioPageViewModel(AudiolaHostViewModel parent) : WorkspacePageViewModel(parent);
public sealed class VoicesPageViewModel(AudiolaHostViewModel parent) : WorkspacePageViewModel(parent);
public sealed class VariationsPageViewModel(AudiolaHostViewModel parent) : WorkspacePageViewModel(parent);
public sealed class ProvenancePageViewModel(AudiolaHostViewModel parent) : WorkspacePageViewModel(parent);
public sealed class EvaluationPageViewModel(AudiolaHostViewModel parent) : WorkspacePageViewModel(parent);
public sealed class MetadataPageViewModel(AudiolaHostViewModel parent) : WorkspacePageViewModel(parent);
public sealed class SettingsPageViewModel(AudiolaHostViewModel parent) : WorkspacePageViewModel(parent);
public sealed class AboutPageViewModel(AudiolaHostViewModel parent) : WorkspacePageViewModel(parent);
