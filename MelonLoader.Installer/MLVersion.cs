namespace MelonLoader.Installer;

public readonly struct MLVersion
{
    public required int Id { get; init; }
    public required string VersionName { get; init; }
    public required bool IsArtifact { get; init; }
    public required bool IsX86 { get; init; }
    public required string DownloadUrl { get; init; }

    public readonly override string ToString()
        => VersionName;
}
