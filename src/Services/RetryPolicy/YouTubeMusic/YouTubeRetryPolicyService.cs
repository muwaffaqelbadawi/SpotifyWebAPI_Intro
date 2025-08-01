using System;
using Polly;
using MusicTransify.src.Infrastructure.RetryPolicy.YouTubeMusic;
using MusicTransify.src.Contracts.RetryPolicy.YouTubeMusic;

namespace MusicTransify.src.Services.RetryPolicy.YouTubeMusic
{
    public class YouTubeRetryPolicyService : IYouTubeMusicRetryPolicy
    {
        public IAsyncPolicy<HttpResponseMessage> RetryPolicy()
            => YouTubeMusicRetryPolicy.Default();
    }
}