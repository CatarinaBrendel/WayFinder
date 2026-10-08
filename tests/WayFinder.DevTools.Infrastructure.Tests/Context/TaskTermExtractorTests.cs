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
                "Fix stale project handling in doctor",
                "WayFinder"
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
                "Fix ProjectRegistry.Add when wayfinder.json already exists",
                "WayFinder"
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
                "Doctor doctor DOCTOR registry",
                "WayFinder"
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
            () => extractor.Extract(" ", "WayFinder")
        );
    }

    [Fact]
    public void Extract_RemovesLowInformationTaskLanguage()
    {
        var extractor =
            new TaskTermExtractor();

        var result =
            extractor.Extract(
                "Investigate how the noise system works",
                "DeadRoute"
            );

        Assert.Equal(
            ["noise", "system"],
            result
        );
    }

    [Fact]
    public void Extract_RemovesCurrentProjectName()
    {
        var extractor =
            new TaskTermExtractor();

        var result =
            extractor.Extract(
                "Investigate how DeadRoute's noise system works",
                "DeadRoute"
            );

        Assert.Equal(
            ["noise", "system"],
            result
        );
    }

    [Fact]
    public void Extract_RemovesCurrentProjectNameIgnoringCase()
    {
        var extractor =
            new TaskTermExtractor();

        var result =
            extractor.Extract(
                "Investigate deadroute noise",
                "DeadRoute"
            );

        Assert.Equal(
            ["noise"],
            result
        );
    }

    [Fact]
    public void Extract_NormalizesSeparatorBetweenLetterAndDigit()
    {
        var extractor =
            new TaskTermExtractor();

        var terms =
            extractor.Extract(
                "Explain malformed UTF-8",
                "TestProject"
            );

        Assert.Equal(
            ["malformed", "UTF8"],
            terms
        );
    }

    [Fact]
    public void Extract_DoesNotRemoveSeparatorBetweenWords()
    {
        var extractor =
            new TaskTermExtractor();

        var terms =
            extractor.Extract(
                "Investigate file-system behavior",
                "TestProject"
            );

        Assert.Contains(
            "file-system",
            terms
        );
    }

    [Fact]
    public void Extract_DeduplicatesNormalizedTerms()
    {
        var extractor =
            new TaskTermExtractor();

        var terms =
            extractor.Extract(
                "UTF-8 UTF8",
                "TestProject"
            );

        Assert.Equal(
            ["UTF8"],
            terms
        );
    }
}
