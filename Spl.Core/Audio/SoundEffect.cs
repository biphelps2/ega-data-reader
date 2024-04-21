using System;
using System.IO;
using SDL2;

namespace Spl.Core.Audio
{
    public class SoundEffect
    {
        public IntPtr SoundEffectPtr;
        public bool CanPlay;

        private SoundEffect()
        {
        }

        public void Play()
        {
            if (CanPlay)
            {
                SDL_mixer.Mix_PlayChannel(-1, SoundEffectPtr, 0);
            }
        }

        public static SoundEffect FromFile(string path, float volume = 1f)
        {
            var canPlay = true;

            var soundEffectPtr = SDL_mixer.Mix_LoadWAV(Path.Join(SdlGame.BasePath, path));
            if (soundEffectPtr == IntPtr.Zero)
            {
                BasicLogger.LogError($"Failed to load sound effect! SDL_mixer Error: {SDL_mixer.Mix_GetError()}");
                canPlay = false;
            }
            else
            {
                // Set volume.
                SDL_mixer.Mix_VolumeChunk(soundEffectPtr, (int)(volume * 128));
            }

            return new SoundEffect
            {
                CanPlay = canPlay,
                SoundEffectPtr = soundEffectPtr
            };
        }
    }
}
