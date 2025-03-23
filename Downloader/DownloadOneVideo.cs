using YoutubeDownloader.Core.Downloading;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
namespace Downloader;

public class DownloadOneVideo
{
    private YoutubeClient youtube;
    private VideoDownloader videoDownloader;
    private InputVideoData inputVideoData;
    public bool Error { get; private set; }

    public DownloadOneVideo(InputVideoData videoData)
    {
        inputVideoData = videoData;
        youtube = new YoutubeClient();
        videoDownloader = new VideoDownloader(Options.Default.Cookies);
    }

    public async Task DownloadVideo()
    {
        int tryCount = 0;
        while (true)
        {
            try
            {
                var videoId = VideoId.Parse(inputVideoData.url);
                var video = await youtube.Videos.GetAsync(videoId);
                if(File.Exists(Options.GetVideoSavePath(video))) break;
                Console.WriteLine($"开始下载{video.Title}");
                var option = await videoDownloader.GetBestDownloadOptionAsync(videoId,
                    new VideoDownloadPreference(Container.Mp4, Options.Default.ConfigData.VideoQualityPreference));
                var progress = Options.GetProgressLog();
                await videoDownloader.DownloadVideoAsync(Options.GetVideoSavePath(video), video, option, true,
                    progress);
                Options.SaveVideoConfig(video);
                Console.WriteLine($"下载完成{video.Title}");
                break;
            }
            catch (Exception e)
            {
                tryCount++;
                Console.WriteLine($"报错:{e}\n正在尝试{tryCount}/{Options.Default.ConfigData.max_retry}");
                if (tryCount >= Options.Default.ConfigData.max_retry)
                {
                    Error = true;
                    Console.WriteLine($"下载失败{inputVideoData.url}");
                }
            }
        }
    }
}