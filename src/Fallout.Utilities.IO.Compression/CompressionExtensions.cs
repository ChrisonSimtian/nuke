using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Fallout.Common.Utilities.Collections;
using ICSharpCode.SharpZipLib.BZip2;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.Zip;
using SharpCompress.Common;
using SharpCompress.Readers;
using ZipFile = ICSharpCode.SharpZipLib.Zip.ZipFile;

namespace Fallout.Common.IO;

public static class CompressionExtensions
{
    public static void CompressTo(this AbsolutePath directory, AbsolutePath archiveFile, Func<AbsolutePath, bool> filter = null)
    {
        if (archiveFile.HasExtension(".zip"))
        {
            directory.ZipTo(archiveFile, filter);
        }
        else if (archiveFile.HasExtension(".tar.gz", ".tgz"))
        {
            directory.TarGZipTo(archiveFile, filter);
        }
        else if (archiveFile.HasExtension(".tar.bz2", ".tbz2", ".tbz"))
        {
            directory.TarBZip2To(archiveFile, filter);
        }
        else if (archiveFile.HasExtension(".tar.xz", ".txz"))
        {
            Assert.Fail($"Compressing a .tar.xz archive currently not supported. Archive file: '{Path.GetFileName(archiveFile)}'");
        }
        else
        {
            Assert.Fail($"Unknown archive extension for archive '{Path.GetFileName(archiveFile)}'");
        }
    }

    public static void UncompressTo(this AbsolutePath archiveFile, AbsolutePath directory)
    {
        if (archiveFile.HasExtension(".zip"))
        {
            archiveFile.UnZipTo(directory);
        }
        else if (archiveFile.HasExtension(".tar.gz", ".tgz"))
        {
            archiveFile.UnTarGZipTo(directory);
        }
        else if (archiveFile.HasExtension(".tar.bz2", ".tbz2", ".tbz"))
        {
            archiveFile.UnTarBZip2To(directory);
        }
        else if (archiveFile.HasExtension(".tar.xz", ".txz"))
        {
            archiveFile.UnTarXzTo(directory);
        }
        else
        {
            Assert.Fail($"Unknown archive extension for archive '{Path.GetFileName(archiveFile)}'");
        }
    }

    public static void ZipTo(
        this AbsolutePath directory,
        AbsolutePath archiveFile,
        Func<AbsolutePath, bool> filter = null,
        CompressionLevel compressionLevel = CompressionLevel.Optimal,
        FileMode fileMode = FileMode.CreateNew)
    {
        archiveFile.Parent.CreateDirectory();

        filter ??= _ => true;
        var files = directory.GetFiles(depth: int.MaxValue).Where(filter).ToList();

        using var fileStream = File.Open(archiveFile, fileMode, FileAccess.ReadWrite);
        using var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Create);

        void AddFile(AbsolutePath file)
        {
            var relativePath = directory.GetRelativePathTo(file);
            var entryName = ZipEntry.CleanName(relativePath);
            zipArchive.CreateEntryFromFile(file, entryName, compressionLevel);
        }

        files.ForEach(AddFile);
    }

    public static void UnZipTo(this AbsolutePath archiveFile, AbsolutePath directory)
    {
        using var fileStream = File.OpenRead(archiveFile);
        using var zipFile = new ZipFile(fileStream);

        var entries = zipFile.Cast<ZipEntry>().Where(x => !x.IsDirectory);

        void HandleEntry(ZipEntry entry)
        {
            var file = directory / entry.Name;
            Directory.CreateDirectory(file.Parent.NotNull());

            using var entryStream = zipFile.GetInputStream(entry);
            using var outputStream = File.Open(file, FileMode.Create);
            entryStream.CopyTo(outputStream);
        }

        entries.ForEach(HandleEntry);
    }

    public static void TarGZipTo(
        this AbsolutePath baseDirectory,
        AbsolutePath archiveFile,
        IEnumerable<AbsolutePath> files,
        FileMode fileMode = FileMode.CreateNew)
    {
        CompressTar(baseDirectory, archiveFile, files.ToList(), fileMode, x => new GZipOutputStream(x));
    }

    public static void TarGZipTo(
        this AbsolutePath directory,
        AbsolutePath archiveFile,
        Func<AbsolutePath, bool> filter = null,
        FileMode fileMode = FileMode.CreateNew)
    {
        filter ??= _ => true;
        var files = directory.GetFiles(depth: int.MaxValue).Where(filter);
        directory.TarGZipTo(archiveFile, files, fileMode);
    }

    public static void TarBZip2To(
        this AbsolutePath directory,
        AbsolutePath archiveFile,
        IEnumerable<AbsolutePath> files,
        FileMode fileMode = FileMode.CreateNew)
    {
        CompressTar(directory, archiveFile, files.ToList(), fileMode, x => new BZip2OutputStream(x));
    }

    public static void TarBZip2To(
        this AbsolutePath directory,
        AbsolutePath archiveFile,
        Func<AbsolutePath, bool> filter = null,
        FileMode fileMode = FileMode.CreateNew)
    {
        filter ??= _ => true;
        var files = directory.GetFiles(depth: int.MaxValue).Where(filter);
        directory.TarBZip2To(archiveFile, files, fileMode);
    }

    public static void UnTarGZipTo(this AbsolutePath archiveFile, AbsolutePath directory)
    {
        UncompressTar(archiveFile, directory, x => new GZipInputStream(x));
    }

    public static void UnTarBZip2To(this AbsolutePath archiveFile, AbsolutePath directory)
    {
        UncompressTar(archiveFile, directory, x => new BZip2InputStream(x));
    }

    public static void UnTarXzTo(this AbsolutePath archive, AbsolutePath directory)
    {
        using Stream stream = File.OpenRead(archive);
        using var reader = ReaderFactory.OpenReader(stream);

        while (reader.MoveToNextEntry())
        {
            if (reader.Entry.IsDirectory)
            {
                continue;
            }

            reader.WriteEntryToDirectory(directory, new ExtractionOptions
            {
                ExtractFullPath = true,
                Overwrite = true
            });
        }
    }

    private static void CompressTar(
        AbsolutePath baseDirectory,
        AbsolutePath archiveFile,
        IReadOnlyCollection<AbsolutePath> files,
        FileMode fileMode,
        Func<Stream, Stream> outputStreamFactory)
    {
        archiveFile.Parent.CreateDirectory();

        using var fileStream = File.Open(archiveFile, fileMode, FileAccess.ReadWrite);
        using var outputStream = outputStreamFactory(fileStream);
        using var tarArchive = TarArchive.CreateOutputTarArchive(outputStream);

        void AddFile(AbsolutePath file)
        {
            var entry = TarEntry.CreateEntryFromFile(file);
            entry.Name = baseDirectory.GetUnixRelativePathTo(file);

            tarArchive.WriteEntry(entry, recurse: false);
        }

        files.ForEach(AddFile);
    }

    private static void UncompressTar(AbsolutePath archiveFile, AbsolutePath directory, Func<Stream, Stream> inputStreamFactory)
    {
        using var fileStream = File.OpenRead(archiveFile);
        using var inputStream = inputStreamFactory(fileStream);
        using var tarArchive = TarArchive.CreateInputTarArchive(inputStream, nameEncoding: null);

        directory.CreateDirectory();

        tarArchive.ExtractContents(directory);
    }
}
