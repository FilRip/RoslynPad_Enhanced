﻿#nullable disable

using System.Security;

namespace RoslynPad.Build;

internal static class IOUtilities
{
    public static void PerformIO(Action action)
    {
        PerformIO<object>(() =>
        {
            action();
            return null;
        });
    }

    public static T PerformIO<T>(Func<T> function, T defaultValue = default)
    {
        try
        {
            return function();
        }
        catch (Exception e) when (IsNormalIOException(e))
        {
            // Nothing to do
        }

        return defaultValue;
    }

    public static async Task<T> PerformIOAsync<T>(Func<Task<T>> function, T defaultValue = default)
    {
        try
        {
            return await function().ConfigureAwait(false);
        }
        catch (Exception e) when (IsNormalIOException(e))
        {
            // Nothing to do
        }

        return defaultValue;
    }

    public static string CurrentDirectory => PerformIO(Directory.GetCurrentDirectory, ".");

    public static string NormalizeFilePath(string filename)
    {
        FileInfo fileInfo = new(filename);
        DirectoryInfo directoryInfo = fileInfo.Directory ?? throw new ArgumentException("Invalid path", nameof(filename));

        return Path.Combine(NormalizeDirectory(directoryInfo),
            directoryInfo.GetFiles(fileInfo.Name)[0].Name);
    }

    private static string NormalizeDirectory(DirectoryInfo dirInfo)
    {
        DirectoryInfo parentDirInfo = dirInfo.Parent;
        if (parentDirInfo == null)
        {
            return dirInfo.Name;
        }

        return Path.Combine(NormalizeDirectory(parentDirInfo),
            parentDirInfo.GetDirectories(dirInfo.Name)[0].Name);
    }

    public static IEnumerable<string> EnumerateFilesRecursive(string path, string searchPattern = "*")
    {
        return EnumerateDirectories(path).Aggregate(
            EnumerateFiles(path, searchPattern),
            (current, directory) => current.Concat(EnumerateFiles(directory, searchPattern)));
    }

    public static IEnumerable<string> ReadLines(string path)
    {
        IEnumerable<string> lines = PerformIO(() => File.ReadLines(path), []);
        using var enumerator = lines.GetEnumerator();
        while (PerformIO(enumerator.MoveNext))
        {
            yield return enumerator.Current;
        }
    }

    public static Task<string> ReadAllTextAsync(string path) =>
        PerformIOAsync(() => ReadAllTextInternalAsync(path), string.Empty);

    private static async Task<string> ReadAllTextInternalAsync(string path)
    {
        using var reader = File.OpenText(path);
        return await reader.ReadToEndAsync().ConfigureAwait(false);
    }

    public static IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*")
    {
        IEnumerable<string> files = PerformIO(() => Directory.EnumerateFiles(path, searchPattern),
            []);

        using var enumerator = files.GetEnumerator();
        while (PerformIO(enumerator.MoveNext))
        {
            yield return enumerator.Current;
        }
    }

    public static IEnumerable<string> EnumerateDirectories(string path, string searchPattern = "*")
    {
        IEnumerable<string> directories = PerformIO(() => Directory.EnumerateDirectories(path, searchPattern),
            []);

        using IEnumerator<string> enumerator = directories.GetEnumerator();
        while (PerformIO(enumerator.MoveNext))
        {
            yield return enumerator.Current;
        }
    }

    public static void DirectoryCopy(string source, string destination, bool overwrite, bool recursive = true)
    {
        foreach (string file in EnumerateFiles(source))
        {
            string destinationFile = Path.Combine(destination, Path.GetFileName(file));
            FileCopy(file, destinationFile, overwrite);
        }

        if (!recursive)
        {
            return;
        }

        foreach (string directory in EnumerateDirectories(source))
        {
            string destinationDirectory = Path.Combine(destination, Path.GetFileName(directory));
            Directory.CreateDirectory(destinationDirectory);
            DirectoryCopy(directory, destinationDirectory, overwrite);
        }
    }

    public static void FileCopy(string source, string destination, bool overwrite)
    {
        const int ERROR_ENCRYPTION_FAILED = unchecked((int)0x80071770);

        try
        {
            File.Copy(source, destination, overwrite);
        }
        catch (IOException ex) when (ex.HResult == ERROR_ENCRYPTION_FAILED)
        {
            using FileStream read = File.OpenRead(source);
            using FileStream write = new(destination, overwrite ? FileMode.Create : FileMode.CreateNew);
            read.CopyTo(write);
        }
    }

    public static bool IsNormalIOException(Exception e)
    {
        return e is IOException ||
               e is SecurityException ||
               e is ArgumentException ||
               e is UnauthorizedAccessException ||
               e is NotSupportedException ||
               e is InvalidOperationException;
    }
}
