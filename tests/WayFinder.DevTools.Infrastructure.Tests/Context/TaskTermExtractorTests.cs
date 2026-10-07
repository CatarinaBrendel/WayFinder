namespace WayFinder.DevTools.Infrastructure.Tests.Context;

using WayFinder.DevTools.Infrastructure.Context;
using Xunit;

public class TaskTermExtractorTests
{
    [Fact]
    public void Extract_RemovesStopWords()
    {
        var extractor =
            new TaskTermExtractor();

        var result =
            extractor.Extract(
                "Fix stale project handling in doctor"
            );

        Assert.Equal(
            ["stale", "project", "handling", "doctor"],
            result
        );
    }

    [Fact]
    public void Extract_PreservesTechnicalIdentifiers()
    {
        var extractor =
            new TaskTermExtractor();

        var result =
            extractor.Extract(
                "Fix ProjectRegistry.Add when wayfinder.json already exists"
            );

        Assert.Equal(
            [
                "ProjectRegistry.Add",
            "wayfinder.json",
            "already",
            "exists",
        ],
            result
        );
    }

    [Fact]
    public void Extract_RemovesDuplicatesIgnoringCase()
    {
        var extractor =
            new TaskTermExtractor();

        var result =
            extractor.Extract(
                "Doctor doctor DOCTOR registry"
            );

        Assert.Equal(
            ["Doctor", "registry"],
            result
        );
    }

    [Fact]
    public void Extract_RejectsEmptyTask()
    {
        var extractor =
            new TaskTermExtractor();

        Assert.Throws<ArgumentException>(
            () => extractor.Extract(" ")
        );
    }
}
