namespace Downloader;

class Program
{
    static async Task Main(string[] args)
    {
        // Options.Parse(args[0]);
        Options.Parse(args[0]);
        foreach (var userUrl in Options.Default.ConfigData.accounts)
        {
            if (!userUrl.enable) continue;
            await new DownloadAccount(userUrl).Download();
        }

        foreach (var inputVideoData in Options.Default.ConfigData.videos)
        {
            if(!inputVideoData.enable) continue;
            await new DownloadOneVideo(inputVideoData).DownloadVideo();
        }
    }
}
