namespace WayFinder.DevTools.Application.Context.Estimation;

public interface ITokenEstimator
{
    int Estimate(
        string text
    );
}
