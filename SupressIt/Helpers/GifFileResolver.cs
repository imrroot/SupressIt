using System;
using System.IO;

namespace SupressIt.Helpers
{
    public enum GifSlot
    {
        Idle,
        Search,
        Kill,
        Block
    }

    public static class GifFileResolver
    {
        private static readonly string[] SupportedExtensions =
        {
            ".gif",
            ".webm",
            ".mp4",
            ".avi"
        };

        public static string DefaultFolderPath { get; } =
            Path.Combine(AppContext.BaseDirectory, "GIF");

        public static void EnsureDefaultFolder()
        {
            Directory.CreateDirectory(DefaultFolderPath);
        }

        public static string Resolve(bool useDefaultFolder, GifSlot slot, string manualPath)
        {
            if (!useDefaultFolder)
                return manualPath;

            return FindDefault(slot) ?? "";
        }

        public static string GetDisplayPath(bool useDefaultFolder, GifSlot slot, string manualPath)
        {
            if (!useDefaultFolder)
                return manualPath;

            return FindDefault(slot) ?? Path.Combine(DefaultFolderPath, $"{GetDefaultName(slot)}.*");
        }

        public static string? FindDefault(GifSlot slot)
        {
            EnsureDefaultFolder();

            var name = GetDefaultName(slot);
            foreach (var extension in SupportedExtensions)
            {
                var path = Path.Combine(DefaultFolderPath, name + extension);
                if (File.Exists(path))
                    return path;
            }

            return null;
        }

        private static string GetDefaultName(GifSlot slot) => slot switch
        {
            GifSlot.Idle => "idle",
            GifSlot.Search => "search",
            GifSlot.Kill => "kill",
            GifSlot.Block => "block",
            _ => ""
        };
    }
}
