using System; using System.IO; using System.Text.Json;
using SupressIt.Models;
namespace SupressIt.Helpers
{
    public static class SettingsStore
    {
        static readonly string Path_ = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SupressIt","settings.json");
        public static AppSettings Load()
        {
            try { if(!File.Exists(Path_)) return new AppSettings();
                  return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(Path_)) ?? new AppSettings(); }
            catch { return new AppSettings(); }
        }
        public static void Save(AppSettings s)
        {
            try { Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path_)!);
                  File.WriteAllText(Path_, JsonSerializer.Serialize(s, new JsonSerializerOptions{WriteIndented=true})); }
            catch { }
        }
    }
}
