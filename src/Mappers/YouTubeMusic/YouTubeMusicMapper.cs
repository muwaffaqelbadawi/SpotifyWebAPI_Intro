using System;
using System.Linq;
using System.Threading.Tasks;
using MusicTransify.src.Dtos.YouTubeMusic;
using MusicTransify.src.Models.YouTubeMusic;

namespace MusicTransify.src.Mappers.YouTubeMusic
{
    public class YouTubeMusicMapper
    {
        public YouTubeMusicDto ToSpotifyDto(YouTubeMusicData youTubeMusic)
        {
            return new YouTubeMusicDto
            {
                Title = youTubeMusic.Title,
                Artist = youTubeMusic.Artist,
                Album = youTubeMusic.Album,
                Duration = youTubeMusic.Duration,
                SourceId = youTubeMusic.SourceId,
                ISRC = youTubeMusic.ISRC,
            };
        }

    }
}