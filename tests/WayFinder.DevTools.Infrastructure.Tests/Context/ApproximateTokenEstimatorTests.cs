using WayFinder.DevTools.Infrastructure.Context.Estimation;
using Xunit;

namespace WayFinder.DevTools.Infrastructure.Tests.Context.Estimation;

public sealed class ApproximateTokenEstimatorTests
{
    private readonly ApproximateTokenEstimator _estimator =
        new();

    [Theory]
    [InlineData("", 0)]
    [InlineData("a", 1)]
    [InlineData("abcd", 1)]
    [InlineData("abcde", 2)]
    [InlineData("abcdefgh", 2)]
    [InlineData("abcdefghi", 3)]
    public void Estimate_ReturnsApproximateTokenCount(
        string text,
        int expected
    )
    {
        var result =
            _estimator.Estimate(text);

        Assert.Equal(
            expected,
            result
        );
    }

    [Fact]
    public void Estimate_RejectsNull()
    {
        Assert.Throws<ArgumentNullException>(
            () => _estimator.Estimate(null!)
        );
    }
}
