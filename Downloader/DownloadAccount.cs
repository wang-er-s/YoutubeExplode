using YoutubeDownloader.Core.Downloading;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos.Streams;

namespace Downloader;

public class DownloadAccount
{
    private InputAccountData accountData;
    private YoutubeClient youtube;
    private VideoDownloader videoDownloader;
    public List<string> ErrorVideo {get; private set;} =  new List<string>();
    
    public DownloadAccount(InputAccountData accountData)
    {
        this.accountData = accountData;
        youtube = new YoutubeClient();
        videoDownloader = new VideoDownloader(Options.Default.Cookies);
    }

    public async Task Download()
    {
        var index = accountData.url.IndexOf("@", StringComparison.Ordinal);
        if (index < 0)
        {
            Console.WriteLine($"url error : {accountData.url}");
            return;
        }

        accountData.userName = accountData.url.Substring(index + 1);
        await DoDownload();
    }
    
    private async Task DoDownload()
    {
        IReadOnlyList<PlaylistVideo> videoList = null;
        try
        {
            Console.WriteLine($"开始解析频道{accountData.url}");
            var channel = await youtube.Channels.GetByHandleAsync(accountData.url);
            Console.WriteLine($"开始获取所有视频{accountData.url}");
            videoList = await youtube.Channels.GetUploadsAsync(channel.Id);
        }
        catch (Exception e)
        {
            Console.WriteLine($"解析频道失败 {e.Message}");
            return;
        }
        foreach (var playlistVideo in videoList)
        {
            int tryCount = 0;
            bool needBreak = false;
            while (true)
            {
                try
                {
                    Console.WriteLine($"获取视频信息：{playlistVideo.Title}...");
                    var video = await youtube.Videos.GetAsync(playlistVideo.Id);
                    if (video.Duration == null)
                    {
                        Console.WriteLine($"Null duration {video.Url}");
                        break;
                    }

                    if ((video.UploadDate.DateTime - accountData.earliestDate).TotalHours >= 0)
                    {
                        var duration = video.Duration.Value.TotalSeconds;
                        if (duration >= Options.Default.ConfigData.download_max_duration) break;
                        Options.SaveVideoConfig(video, accountData);
                        if (File.Exists(Options.GetVideoSavePath(video, accountData)))
                        {
                            Console.WriteLine($"视频已经下载 {Options.GetVideoSavePath(video, accountData)}");
                            break;
                        }
                        Console.WriteLine($"开始下载{video.Title}");
                        var option = await videoDownloader.GetBestDownloadOptionAsync(playlistVideo.Id,
                            new VideoDownloadPreference(Container.Mp4, VideoQualityPreference.UpTo480p));
                        var progress = Options.GetProgressLog();
                        await videoDownloader.DownloadVideoAsync(Options.GetVideoSavePath(video, accountData), video,
                            option, true,
                            progress);
                        Console.WriteLine($"下载完成{video.Title}");
                        break;
                    }

                    needBreak = true;
                    break;
                }
                catch (Exception e)
                {
                    tryCount++;
                    Console.WriteLine($"报错:{e}\n正在尝试{tryCount}/{Options.Default.ConfigData.max_retry}");
                    if (tryCount >= Options.Default.ConfigData.max_retry)
                    {
                        Console.WriteLine($"下载失败{playlistVideo.Title}");
                        ErrorVideo.Add(playlistVideo.Url);
                        break;
                    }
                }
            }

            if (needBreak) break;
        }
    }
}