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
                if( video.Duration == null)
                {
                    Console.WriteLine($"视频信息获取失败 {inputVideoData.url}");
                    break;
                }
                if (File.Exists(Options.GetVideoSavePath(video)))
                {
                    Console.WriteLine($"视频已存在：{Options.GetVideoSavePath(video)}");
                    break;
                }
                if (!string.IsNullOrEmpty(inputVideoData.earliest) && (video.UploadDate.DateTime - inputVideoData.earliestDate).TotalHours >= 0)
                {
                    Console.WriteLine($"视频上传时间小于 {inputVideoData.earliest}");
                    break;
                }
                var duration = video.Duration.Value.TotalSeconds;
                if(duration > Options.Default.ConfigData.download_max_duration)
                {
                    Console.WriteLine($"视频时长超过限制 {inputVideoData.url}");
                    break;
                }
                Console.WriteLine($"开始下载 {inputVideoData.url}");
                var option = await videoDownloader.GetBestDownloadOptionAsync(videoId,
                    new VideoDownloadPreference(Options.GetContainer(Options.GetVideoSavePath(video)), Options.Default.ConfigData.VideoQualityPreference));
                var progress = Options.GetProgressLog();
                await videoDownloader.DownloadVideoAsync(Options.GetVideoSavePath(video), video, option, true,
                    progress);
                Options.SaveVideoConfig(video);
                Console.WriteLine($"下载完成{Options.GetVideoSavePath(video)}");
                break;
            }
            catch (Exception e)
            {
                tryCount++;
                Console.WriteLine($"报错:{e}\n正在尝试{tryCount}/{Options.Default.ConfigData.max_retry}");
                await Task.Delay(1000);
                if (tryCount >= Options.Default.ConfigData.max_retry)
                {
                    Error = true;
                    Console.WriteLine($"下载失败{inputVideoData.url}");
                    break;
                }
            }
        }
    }
}