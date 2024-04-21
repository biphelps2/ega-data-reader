using System;
using System.IO;
using SDL2;

namespace Spl.Core.Audio
{
    public class Song
    {
        public IntPtr SongPtr;
        public bool CanPlay;

        private Song()
        {
        }

        public void Play()
        {
            if (CanPlay)
            {
                // If we don't check this, we get the "start again" cut.
                if(SDL_mixer.Mix_PlayingMusic() == 0 )
                {
                    //Play the music
                    SDL_mixer.Mix_PlayMusic(SongPtr, -1);
                }
            }
        }

        public static Song FromFile(string path)
        {
            var canPlay = true;

            var musicPtr = SDL_mixer.Mix_LoadMUS(Path.Join(SdlGame.BasePath, path));
            if (musicPtr == IntPtr.Zero)
            {
                BasicLogger.LogError($"Failed to load music! SDL_mixer Error: {SDL_mixer.Mix_GetError()}");
                canPlay = false;
            }

            return new Song
            {
                CanPlay = canPlay,
                SongPtr = musicPtr
            };
        }
    }
}
