using System.Security.Cryptography;
using System.Text;
using StudyTracker.Core.Status;

namespace StudyTracker.Infrastructure.Status;

public sealed class StatusDataStore
{
    public const string FileName = "status-dados.js";

    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    public StatusDataStore(StudyFolder folder)
    {
        ArgumentNullException.ThrowIfNull(folder);
        FilePath = Path.Combine(folder.RootPath, FileName);
    }

    public string FilePath { get; }

    public async Task<StatusSnapshot> LoadAsync(CancellationToken cancellationToken = default)
    {
        var bytes = await ReadBytesAsync(cancellationToken);
        var file = StatusDataFile.Parse(Decode(bytes));
        return new StatusSnapshot(file, Hash(bytes));
    }

    // Writes only if the file is still the one the snapshot was read from, so edits made elsewhere are never lost.
    public async Task<StatusSnapshot> SaveAsync(StatusSnapshot basedOn, StatusData data, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(basedOn);
        ArgumentNullException.ThrowIfNull(data);

        var onDisk = await ReadBytesAsync(cancellationToken);
        if (Hash(onDisk) != basedOn.Version)
            throw new StatusDataConflictException();

        var bytes = Utf8NoBom.GetBytes(StatusDataFile.Format(data, basedOn.File));
        var tempPath = FilePath + ".tmp";
        await File.WriteAllBytesAsync(tempPath, bytes, cancellationToken);
        File.Move(tempPath, FilePath, overwrite: true);

        return new StatusSnapshot(StatusDataFile.Parse(Decode(bytes)), Hash(bytes));
    }

    public async Task<bool> HasChangedAsync(StatusSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return Hash(await ReadBytesAsync(cancellationToken)) != snapshot.Version;
    }

    private async Task<byte[]> ReadBytesAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(FilePath))
            throw new FileNotFoundException($"{FileName} was not found in the study folder.", FilePath);
        return await File.ReadAllBytesAsync(FilePath, cancellationToken);
    }

    private static string Decode(byte[] bytes) =>
        new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(bytes).TrimStart('﻿');

    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
}

public sealed class StatusSnapshot
{
    internal StatusSnapshot(StatusDataFile file, string version)
    {
        File = file;
        Version = version;
    }

    internal StatusDataFile File { get; }

    public StatusData Data => File.Data;

    public string Version { get; }
}
