using System;
using System.Collections.Generic;
using System.Linq;
using YoutubeExplode.Videos.Streams;

namespace YoutubeDownloader.Core.Downloading;

public record VideoDownloadPreference(
    Container PreferredContainer,
    VideoQualityPreference PreferredVideoQuality
)
{
    public VideoDownloadOption? TryGetBestOption(IReadOnlyList<VideoDownloadOption> options)
    {
        // Short-circuit for audio-only formats
        if (PreferredContainer.IsAudioOnly)
            return options.FirstOrDefault(o => o.Container == PreferredContainer);

        var orderedOptions = options.OrderBy(o => o.VideoQuality).ToArray();

        VideoDownloadOption? preferredOption;
        switch (PreferredVideoQuality)
        {
            case VideoQualityPreference.Highest:
                preferredOption = orderedOptions.LastOrDefault(o => o.Container == PreferredContainer);
                break;
            case VideoQualityPreference.UpTo1080p:
                preferredOption = orderedOptions.Where(o => o.VideoQuality?.MaxHeight <= 1080)
                    .LastOrDefault(o => o.Container == PreferredContainer);
                break;
            case VideoQualityPreference.UpTo720p:
                preferredOption = orderedOptions.Where(o => o.VideoQuality?.MaxHeight <= 720)
                    .LastOrDefault(o => o.Container == PreferredContainer);
                break;
            case VideoQualityPreference.UpTo480p:
                preferredOption = orderedOptions.Where(o => o.VideoQuality?.MaxHeight <= 480)
                    .LastOrDefault(o => o.Container == PreferredContainer);
                break;
            case VideoQualityPreference.UpTo360p:
                preferredOption = orderedOptions.Where(o => o.VideoQuality?.MaxHeight <= 360)
                    .LastOrDefault(o => o.Container == PreferredContainer);
                break;
            case VideoQualityPreference.Lowest:
                preferredOption = orderedOptions.FirstOrDefault(o => o.Container == PreferredContainer && o.VideoQuality != null);
                break;
            default:
                throw new InvalidOperationException($"Unknown video quality preference '{PreferredVideoQuality}'.");
        }

        return preferredOption
               ?? orderedOptions.FirstOrDefault(o => o.Container == PreferredContainer);
    }
}
