namespace WayFinder.DevTools.Application.Projects.Detection;

public interface ITechnologySignatureProvider
{
    IReadOnlyCollection<TechnologySignature> GetSignatures();
}
