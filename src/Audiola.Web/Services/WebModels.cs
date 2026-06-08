namespace Audiola.Web.Services;

public sealed record VariationItemDto(string Id, string Name, string Description);
public sealed record VariationProviderDto(string Provider, List<VariationItemDto> Variations);

/// <summary>Auswahl aus dem Variations-Dialog: ein Provider + mehrere Variation-IDs.</summary>
public sealed record VariationChoice(string Provider, List<string> Ids);
