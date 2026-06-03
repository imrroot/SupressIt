using System;
using System.IO;

namespace SupressIt.Helpers
{
    public enum SoundSlot
    {
        Idle,
        Search,
        Kill,
        Block
    }

    public static class SoundFileResolver
    {
        private static readonly string[] SupportedExtensions =
        {
            ".wav",
            ".mp3",
            ".ogg",
            ".m4a",
            ".aac",
            ".flac",
            ".wma"
        };

        public static string DefaultFolderPath { get; } =
            Path.Combine(AppContext.BaseDirectory, "Sound");

        public static void EnsureDefaultFolder()
        {
            Directory.CreateDirectory(DefaultFolderPath);
        }

        public static string Resolve(bool useDefaultFolder, SoundSlot slot, string manualPath)
        {
            if (!useDefaultFolder)
                return manualPath;

            return FindDefault(slot) ?? "";
        }

        public static string GetDisplayPath(bool useDefaultFolder, SoundSlot slot, string manualPath)
        {
            if (!useDefaultFolder)
                return manualPath;

            return FindDefault(slot) ?? Path.Combine(DefaultFolderPath, $"{GetDefaultName(slot)}.*");
        }

        public static string? FindDefault(SoundSlot slot)
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

        private static string GetDefaultName(SoundSlot slot) => slot switch
        {
            SoundSlot.Idle => "idle",
            SoundSlot.Search => "search",
            SoundSlot.Kill => "kill",
            SoundSlot.Block => "block",
            _ => ""
        };
    }
}
