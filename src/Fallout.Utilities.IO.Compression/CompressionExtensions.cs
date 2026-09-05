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
    /// <summary>
    ///     Compresses <paramref name="directory" /> into <paramref name="archiveFile" />, choosing the compression format based on the
    ///     archive file's extension.
    /// </summary>
    /// <param name="directory">The directory whose contents should be compressed.</param>
    /// <param name="archiveFile">
    ///     The archive file to create. Its extension determines the compression format (e.g. <c>.zip</c>,
    ///     <c>.tar.gz</c>, <c>.tar.bz2</c>).
    /// </param>
    /// <param name="filter">
    ///     An optional predicate used to filter which files are included in the archive. If <c>null</c>, all files are
    ///     included.
    /// </param>
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
            Assert.Fail(
                $"Compressing a .tar.xz archive currently not supported. Archive file: '{Path.GetFileName(archiveFile)}'");
        }
        else
        {
            Assert.Fail($"Unknown archive extension for archive '{Path.GetFileName(archiveFile)}'");
        }
    }

    /// <summary>
    ///     Uncompresses <paramref name="archiveFile" /> into <paramref name="directory" />, choosing the decompression format based on
    ///     the archive file's extension.
    /// </summary>
    /// <param name="archiveFile">
    ///     The archive file to extract. Its extension determines the decompression format (e.g. <c>.zip</c>,
    ///     <c>.tar.gz</c>, <c>.tar.bz2</c>, <c>.tar.xz</c>).
    /// </param>
    /// <param name="directory">The directory into which the archive's contents are extracted.</param>
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

    /// <summary>
    ///     Compresses <paramref name="directory" /> into a ZIP archive at <paramref name="archiveFile" />.
    /// </summary>
    /// <param name="directory">The directory whose contents should be added to the ZIP archive.</param>
    /// <param name="archiveFile">The ZIP archive file to create.</param>
    /// <param name="filter">
    ///     An optional predicate used to filter which files are included in the archive. If <c>null</c>, all files are
    ///     included.
    /// </param>
    /// <param name="compressionLevel">The compression level to use for the archive entries.</param>
    /// <param name="fileMode">The <see cref="FileMode" /> used to open the archive file.</param>
    public static void ZipTo(
        this AbsolutePath directory,
        AbsolutePath archiveFile,
        Func<AbsolutePath, bool> filter = null,
        CompressionLevel compressionLevel = CompressionLevel.Optimal,
        FileMode fileMode = FileMode.CreateNew)
    {
        archiveFile.Parent.CreateDirectory();

        filter ??= _ => true;
        List<AbsolutePath> files = directory.GetFiles(depth: int.MaxValue).Where(filter).ToList();

        using FileStream fileStream = File.Open(archiveFile, fileMode, FileAccess.ReadWrite);
        using ZipArchive zipArchive = new(fileStream, ZipArchiveMode.Create);

        void AddFile(AbsolutePath file)
        {
            RelativePath relativePath = directory.GetRelativePathTo(file);
            string entryName = ZipEntry.CleanName(relativePath);
            zipArchive.CreateEntryFromFile(file, entryName, compressionLevel);
        }

        files.ForEach(AddFile);
    }

    /// <summary>
    ///     Extracts the contents of a ZIP archive at <paramref name="archiveFile" /> into <paramref name="directory" />.
    ///     Destination directory is created, and conflicting files are overwritten.
    /// </summary>
    /// <param name="archiveFile">The ZIP archive file to extract.</param>
    /// <param name="directory">The directory into which the archive's contents are extracted.</param>
    public static void UnZipTo(this AbsolutePath archiveFile, AbsolutePath directory)
    {
        using FileStream fileStream = File.OpenRead(archiveFile);
        using ZipFile zipFile = new(fileStream);

        IEnumerable<ZipEntry> entries = zipFile.Cast<ZipEntry>().Where(x => !x.IsDirectory);

        void HandleEntry(ZipEntry entry)
        {
            AbsolutePath file = directory / entry.Name;
            Directory.CreateDirectory(file.Parent.NotNull());

            using Stream entryStream = zipFile.GetInputStream(entry);
            using FileStream outputStream = File.Open(file, FileMode.Create);
            entryStream.CopyTo(outputStream);
        }

        entries.ForEach(HandleEntry);
    }

    /// <summary>
    ///     Compresses the given <paramref name="files" /> into a gzip-compressed tar archive at <paramref name="archiveFile" />.
    /// </summary>
    /// <param name="baseDirectory">The base directory used to compute the relative entry names of the archived files.</param>
    /// <param name="archiveFile">The tar.gz archive file to create.</param>
    /// <param name="files">The files to add to the archive.</param>
    /// <param name="fileMode">The <see cref="FileMode" /> used to open the archive file.</param>
    public static void TarGZipTo(
        this AbsolutePath baseDirectory,
        AbsolutePath archiveFile,
        IEnumerable<AbsolutePath> files,
        FileMode fileMode = FileMode.CreateNew) =>
        CompressTar(baseDirectory, archiveFile, files.ToList(), fileMode, x => new GZipOutputStream(x));

    /// <summary>
    ///     Compresses <paramref name="directory" /> into a gzip-compressed tar archive at <paramref name="archiveFile" />.
    /// </summary>
    /// <param name="directory">The directory whose contents should be added to the archive.</param>
    /// <param name="archiveFile">The tar.gz archive file to create.</param>
    /// <param name="filter">
    ///     An optional predicate used to filter which files are included in the archive. If <c>null</c>, all files are
    ///     included.
    /// </param>
    /// <param name="fileMode">The <see cref="FileMode" /> used to open the archive file.</param>
    public static void TarGZipTo(
        this AbsolutePath directory,
        AbsolutePath archiveFile,
        Func<AbsolutePath, bool> filter = null,
        FileMode fileMode = FileMode.CreateNew)
    {
        filter ??= _ => true;
        IEnumerable<AbsolutePath> files = directory.GetFiles(depth: int.MaxValue).Where(filter);
        directory.TarGZipTo(archiveFile, files, fileMode);
    }

    /// <summary>
    ///     Compresses the given <paramref name="files" /> into a bzip2-compressed tar archive at <paramref name="archiveFile" />.
    /// </summary>
    /// <param name="directory">The base directory used to compute the relative entry names of the archived files.</param>
    /// <param name="archiveFile">The tar.bz2 archive file to create.</param>
    /// <param name="files">The files to add to the archive.</param>
    /// <param name="fileMode">The <see cref="FileMode" /> used to open the archive file.</param>
    public static void TarBZip2To(
        this AbsolutePath directory,
        AbsolutePath archiveFile,
        IEnumerable<AbsolutePath> files,
        FileMode fileMode = FileMode.CreateNew) =>
        CompressTar(directory, archiveFile, files.ToList(), fileMode, x => new BZip2OutputStream(x));

    /// <summary>
    ///     Compresses <paramref name="directory" /> into a bzip2-compressed tar archive at <paramref name="archiveFile" />.
    /// </summary>
    /// <param name="directory">The directory whose contents should be added to the archive.</param>
    /// <param name="archiveFile">The tar.bz2 archive file to create.</param>
    /// <param name="filter">
    ///     An optional predicate used to filter which files are included in the archive. If <c>null</c>, all files are
    ///     included.
    /// </param>
    /// <param name="fileMode">The <see cref="FileMode" /> used to open the archive file.</param>
    public static void TarBZip2To(
        this AbsolutePath directory,
        AbsolutePath archiveFile,
        Func<AbsolutePath, bool> filter = null,
        FileMode fileMode = FileMode.CreateNew)
    {
        filter ??= _ => true;
        IEnumerable<AbsolutePath> files = directory.GetFiles(depth: int.MaxValue).Where(filter);
        directory.TarBZip2To(archiveFile, files, fileMode);
    }

    /// <summary>
    ///     Extracts the contents of a gzip-compressed tar archive at <paramref name="archiveFile" /> into <paramref name="directory" />.
    ///     Destination directory is created, and conflicting files are overwritten.
    /// </summary>
    /// <param name="archiveFile">The tar.gz archive file to extract.</param>
    /// <param name="directory">The directory into which the archive's contents are extracted.</param>
    public static void UnTarGZipTo(this AbsolutePath archiveFile, AbsolutePath directory) =>
        UncompressTar(archiveFile, directory, x => new GZipInputStream(x));

    /// <summary>
    ///     Extracts the contents of a bzip2-compressed tar archive at <paramref name="archiveFile" /> into <paramref name="directory" />.
    ///     Destination directory is created, and conflicting files are overwritten.
    /// </summary>
    /// <param name="archiveFile">The tar.bz2 archive file to extract.</param>
    /// <param name="directory">The directory into which the archive's contents are extracted.</param>
    public static void UnTarBZip2To(this AbsolutePath archiveFile, AbsolutePath directory) =>
        UncompressTar(archiveFile, directory, x => new BZip2InputStream(x));

    /// <summary>
    ///     Extracts the contents of an xz-compressed tar archive at <paramref name="archive" /> into <paramref name="directory" />.
    ///     Destination directory is created, and conflicting files are skipped.
    /// </summary>
    /// <param name="archive">The tar.xz archive file to extract.</param>
    /// <param name="directory">The directory into which the archive's contents are extracted.</param>
    public static void UnTarXzTo(this AbsolutePath archive, AbsolutePath directory)
    {
        using Stream stream = File.OpenRead(archive);
        using IReader reader = ReaderFactory.OpenReader(stream);

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

    /// <summary>
    ///     Compresses the given <paramref name="files" /> into a tar archive at <paramref name="archiveFile" />, wrapping the underlying
    ///     file stream with the compression stream produced by <paramref name="outputStreamFactory" />.
    /// </summary>
    /// <param name="baseDirectory">The base directory used to compute the relative entry names of the archived files.</param>
    /// <param name="archiveFile">The archive file to create.</param>
    /// <param name="files">The files to add to the archive.</param>
    /// <param name="fileMode">The <see cref="FileMode" /> used to open the archive file.</param>
    /// <param name="outputStreamFactory">A factory that wraps the raw archive file stream with the desired compression stream.</param>
    private static void CompressTar(
        AbsolutePath baseDirectory,
        AbsolutePath archiveFile,
        IReadOnlyCollection<AbsolutePath> files,
        FileMode fileMode,
        Func<Stream, Stream> outputStreamFactory)
    {
        archiveFile.Parent.CreateDirectory();

        using FileStream fileStream = File.Open(archiveFile, fileMode, FileAccess.ReadWrite);
        using Stream outputStream = outputStreamFactory(fileStream);
        using TarArchive tarArchive = TarArchive.CreateOutputTarArchive(outputStream);

        void AddFile(AbsolutePath file)
        {
            TarEntry entry = TarEntry.CreateEntryFromFile(file);
            entry.Name = baseDirectory.GetUnixRelativePathTo(file);

            tarArchive.WriteEntry(entry, false);
        }

        files.ForEach(AddFile);
    }

    /// <summary>
    ///     Extracts the contents of a tar archive at <paramref name="archiveFile" /> into <paramref name="directory" />, wrapping the
    ///     underlying file stream with the decompression stream produced by <paramref name="inputStreamFactory" />.
    /// </summary>
    /// <param name="archiveFile">The archive file to extract.</param>
    /// <param name="directory">The directory into which the archive's contents are extracted.</param>
    /// <param name="inputStreamFactory">A factory that wraps the raw archive file stream with the desired decompression stream.</param>
    private static void UncompressTar(AbsolutePath archiveFile, AbsolutePath directory, Func<Stream, Stream> inputStreamFactory)
    {
        using FileStream fileStream = File.OpenRead(archiveFile);
        using Stream inputStream = inputStreamFactory(fileStream);
        using TarArchive tarArchive = TarArchive.CreateInputTarArchive(inputStream, null);

        directory.CreateDirectory();

        tarArchive.ExtractContents(directory);
    }
}
