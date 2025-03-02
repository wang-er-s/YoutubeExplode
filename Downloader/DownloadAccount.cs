using YoutubeDownloader.Core.Downloading;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Videos.Streams;

namespace Downloader;

public class DownloadAccount
{
    private InputAccountData accountData;
    private YoutubeClient youtube;
    private VideoDownloader videoDownloader;
    
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
        Console.WriteLine($"开始解析频道{accountData.url}");
        var channel = await youtube.Channels.GetByHandleAsync(accountData.url);
        Console.WriteLine($"开始获取所有视频{accountData.url}");
        var videoList = await youtube.Channels.GetUploadsAsync(channel.Id);
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
                    if (File.Exists(Options.GetVideoSavePath(video, accountData))) continue;
                    if ((video.UploadDate.Date - accountData.earliestDate).TotalHours >= 0)
                    {
                        Console.WriteLine($"开始下载{video.Title}");
                        var option = await videoDownloader.GetBestDownloadOptionAsync(playlistVideo.Id,
                            new VideoDownloadPreference(Container.Mp4, VideoQualityPreference.UpTo1080p));
                        var progress = Options.GetProgressLog();
                        await videoDownloader.DownloadVideoAsync(Options.GetVideoSavePath(video, accountData), video,
                            option, true,
                            progress);
                        Options.SaveVideoConfig(video, accountData);
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
                    }
                }
            }

            if (needBreak) break;
        }
    }
}