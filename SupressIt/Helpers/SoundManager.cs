using System;
using System.IO;
using System.Windows.Media;
using SupressIt.Models;

namespace SupressIt.Helpers
{
    public enum SoundHint { Normal, Searching, Kill, Block }

    public interface ISoundPlayer
    {
        void Play(SoundHint hint);
    }

    public sealed class SoundManager : ISoundPlayer
    {
        private readonly AppSettings _settings;
        private readonly MediaPlayer _player = new();

        public SoundManager(AppSettings settings)
        {
            _settings = settings;
        }

        public void Play(SoundHint hint)
        {
            if (!_settings.SoundEnabled)
                return;

            var path = GetPath(hint);
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path) && PlayFile(path))
                return;

            if (hint is SoundHint.Kill or SoundHint.Block)
                KillSound.Play();
        }

        private string GetPath(SoundHint hint) => hint switch
        {
            SoundHint.Kill => SoundFileResolver.Resolve(_settings.UseDefaultSoundFolder, SoundSlot.Kill, _settings.SoundKillPath),
            SoundHint.Block => SoundFileResolver.Resolve(_settings.UseDefaultSoundFolder, SoundSlot.Block, _settings.SoundBlockPath),
            SoundHint.Searching => SoundFileResolver.Resolve(_settings.UseDefaultSoundFolder, SoundSlot.Search, _settings.SoundSearchingPath),
            _ => SoundFileResolver.Resolve(_settings.UseDefaultSoundFolder, SoundSlot.Idle, _settings.SoundNormalPath)
        };

        private bool PlayFile(string path)
        {
            try
            {
                _player.Stop();
                _player.Open(new Uri(path, UriKind.Absolute));
                _player.Volume = Math.Clamp(_settings.SoundVolume, 0, 1);
                _player.SpeedRatio = Math.Max(0.1, _settings.SoundSpeed);
                _player.Play();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
