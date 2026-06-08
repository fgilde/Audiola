namespace Audiola.Services;

public interface ISettingsService
{
    AppSettings Current { get; }

    void Save();
}
