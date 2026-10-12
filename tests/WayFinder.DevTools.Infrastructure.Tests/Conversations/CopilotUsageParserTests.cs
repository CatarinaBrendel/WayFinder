using WayFinder.DevTools.Infrastructure.Conversations;

namespace WayFinder.DevTools.Infrastructure.Tests.Conversations;

public sealed class CopilotUsageParserTests
{
    [Fact]
    public void Parse_ExtractsUsageStatistics()
    {
        const string json = """
        {
          "totalPremiumRequestCost": 0,
          "tokenDetails": {
            "input": {
              "tokenCount": 613
            },
            "cache_read": {
              "tokenCount": 12928
            },
            "output": {
              "tokenCount": 132
            }
          },
          "totalApiDurationMs": 4798,
          "currentModel": "gpt-5-mini"
        }
        """;

        var usage = CopilotUsageParser.Parse(json);

        Assert.Equal(13541, usage.InputTokens);
        Assert.Equal(132, usage.OutputTokens);
        Assert.Equal(12928, usage.CachedInputTokens);
        Assert.Equal("gpt-5-mini", usage.Model);
        Assert.Equal(4798, usage.DurationMs);
        Assert.Equal(0, usage.PremiumRequestCost);
    }
}
