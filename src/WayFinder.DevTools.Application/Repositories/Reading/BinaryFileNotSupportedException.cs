namespace WayFinder.DevTools.Application.Repositories.Reading;

public sealed class BinaryFileNotSupportedException(
    string path
) : Exception(
    $"The file '{path}' appears to be binary and cannot be read as repository text."
);
