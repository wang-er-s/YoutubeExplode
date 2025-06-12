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
                        Console.WriteLine($"视频信息获取失败 {playlistVideo.Url}");
                        break;
                    }

                    if (File.Exists(Options.GetVideoSavePath(video)))
                    {
                        Console.WriteLine($"视频已经下载 {Options.GetVideoSavePath(video)}");
                        break;
                    }

                    // 判断视频上传时间是否大于限制
                    if ((video.UploadDate.DateTime - Options.Default.ConfigData.earliestDate).TotalHours >= 0)
                    {
                        var duration = video.Duration.Value.TotalSeconds;
                        // 判断视频时长是否超过限制
                        if (accountData.download_max_duration > 0)
                        {
                            if (duration >= accountData.download_max_duration) break;
                        }
                        else
                        {
                            if (duration >= Options.Default.ConfigData.download_max_duration) break;
                        }
                        if (accountData.download_min_duration > 0 && duration <= accountData.download_min_duration) break;

                        if (!string.IsNullOrEmpty(accountData.videoFilter) && !video.Title.Contains(accountData.videoFilter)) break;

                        Console.WriteLine($"开始下载{video.Title}");
                        var savePath = Options.GetVideoSavePath(video);
                        var option = await videoDownloader.GetBestDownloadOptionAsync(playlistVideo.Id,
                            new VideoDownloadPreference(Options.GetContainer(savePath), Options.Default.ConfigData.VideoQualityPreference));
                        var progress = Options.GetProgressLog();
                        await videoDownloader.DownloadVideoAsync(savePath, video,option, true, progress);
                        Options.SaveVideoConfig(video, accountData.isStar);
                        Console.WriteLine($"下载完成{Options.GetVideoSavePath(video)}");
                        break;
                    }

                    needBreak = true;
                    break;
                }
                catch (Exception e)
                {
                    tryCount++;
                    Console.WriteLine($"报错:{e.Message.Split("\n")[0]}\n正在尝试{tryCount}/{Options.Default.ConfigData.max_retry}");
                    await Task.Delay(1000);
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