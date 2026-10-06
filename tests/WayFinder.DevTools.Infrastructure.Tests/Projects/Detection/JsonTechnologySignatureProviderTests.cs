using WayFinder.DevTools.Infrastructure.Projects.Detection;

namespace WayFinder.DevTools.Infrastructure.Tests.Projects.Detection;

public sealed class JsonTechnologySignatureProviderTests
{
    [Fact]
    public void GetSignatures_LoadsEmbeddedCatalog()
    {
        var provider = new JsonTechnologySignatureProvider();

        var signatures = provider.GetSignatures();

        Assert.NotEmpty(signatures);
    }

    [Fact]
    public void GetSignatures_ReturnsKnownBuiltInTechnologies()
    {
        var provider = new JsonTechnologySignatureProvider();

        var signatures = provider.GetSignatures();

        Assert.Contains(
            signatures,
            signature => signature.Id == "dotnet"
        );

        Assert.Contains(
            signatures,
            signature => signature.Id == "node"
        );

        Assert.Contains(
            signatures,
            signature => signature.Id == "swift"
        );
    }

    [Fact]
    public void GetSignatures_ReturnsDeterministicOrder()
    {
        var provider = new JsonTechnologySignatureProvider();

        var signatures = provider.GetSignatures();

        var ids = signatures
            .Select(signature => signature.Id)
            .ToArray();

        var expected = ids
            .OrderBy(
                id => id,
                StringComparer.Ordinal
            )
            .ToArray();

        Assert.Equal(expected, ids);
    }

    [Fact]
    public void GetSignatures_ContainsValidDefinitions()
    {
        var provider = new JsonTechnologySignatureProvider();

        var signatures = provider.GetSignatures();

        Assert.All(
            signatures,
            signature =>
            {
                Assert.False(
                    string.IsNullOrWhiteSpace(signature.Id)
                );

                Assert.False(
                    string.IsNullOrWhiteSpace(signature.DisplayName)
                );

                Assert.NotEmpty(signature.Artifacts);

                Assert.All(
                    signature.Artifacts,
                    artifact =>
                    {
                        Assert.False(
                            string.IsNullOrWhiteSpace(
                                artifact.Pattern
                            )
                        );

                        Assert.False(
                            string.IsNullOrWhiteSpace(
                                artifact.Type
                            )
                        );
                    }
                );
            }
        );
    }
}
