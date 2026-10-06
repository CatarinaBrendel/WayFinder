using System.Reflection;
using System.Text.Json;
using WayFinder.DevTools.Application.Projects.Detection;

namespace WayFinder.DevTools.Infrastructure.Projects.Detection;

public sealed class JsonTechnologySignatureProvider
    : ITechnologySignatureProvider
{
    private const int SupportedVersion = 1;

    private const string ResourceName =
        "WayFinder.DevTools.Infrastructure.Resources.technologies.json";

    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            PropertyNameCaseInsensitive = true,
        };

    public IReadOnlyCollection<TechnologySignature> GetSignatures()
    {
        var assembly = typeof(JsonTechnologySignatureProvider).Assembly;

        using var stream =
            assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded technology catalog '{ResourceName}' was not found."
            );

        var catalog =
            JsonSerializer.Deserialize<TechnologyCatalog>(
                stream,
                JsonOptions
            )
            ?? throw new InvalidDataException(
                "The embedded technology catalog is invalid."
            );

        if (catalog.Version != SupportedVersion)
        {
            throw new InvalidDataException(
                $"Unsupported technology catalog version '{catalog.Version}'."
            );
        }

        Validate(catalog);

        return catalog.Technologies
            .Select(
                technology =>
                    new TechnologySignature(
                        Id: technology.Id,
                        DisplayName: technology.DisplayName,
                        Artifacts: technology.Artifacts
                            .Select(
                                artifact =>
                                    new ArtifactSignature(
                                        Pattern: artifact.Pattern,
                                        Type: artifact.Type
                                    )
                            )
                            .ToArray()
                    )
            )
            .OrderBy(
                signature => signature.Id,
                StringComparer.Ordinal
            )
            .ToArray();
    }

    private static void Validate(TechnologyCatalog catalog)
    {
        var duplicateIds = catalog.Technologies
            .GroupBy(
                technology => technology.Id,
                StringComparer.Ordinal
            )
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicateIds.Length > 0)
        {
            throw new InvalidDataException(
                $"Duplicate technology IDs: {string.Join(", ", duplicateIds)}."
            );
        }
    }
}
