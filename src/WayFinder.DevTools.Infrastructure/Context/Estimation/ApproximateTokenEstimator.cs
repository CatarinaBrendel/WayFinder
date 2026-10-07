using WayFinder.DevTools.Application.Context.Estimation;

namespace WayFinder.DevTools.Infrastructure.Context.Estimation;

public sealed class ApproximateTokenEstimator
    : ITokenEstimator
{
    public int Estimate(
        string text
    )
    {
        ArgumentNullException.ThrowIfNull(
            text
        );

        if (text.Length == 0)
        {
            return 0;
        }

        return (text.Length + 3) / 4;
    }
}
