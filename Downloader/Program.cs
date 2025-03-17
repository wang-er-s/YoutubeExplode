using Newtonsoft.Json;

namespace Downloader;

class Program
{
    static async Task Main(string[] args)
    {
        Options.Parse(args[0]);
        List<string> errorVideos = new List<string>();
        foreach (var userUrl in Options.Default.ConfigData.accounts)
        {
            if (!userUrl.enable) continue;
            var downloadAccount = new DownloadAccount(userUrl);
            await downloadAccount.Download();
            errorVideos.AddRange(downloadAccount.ErrorVideo);
        }

        foreach (var inputVideoData in Options.Default.ConfigData.videos)
        {
            if(!inputVideoData.enable) continue;
            var downVideo = new DownloadOneVideo(inputVideoData);
            await downVideo.DownloadVideo();
            if(downVideo.Error)
                errorVideos.Add(inputVideoData.url);
        }
    
        List<InputVideoData> videos = new List<InputVideoData>();
        foreach (var errorVideo in errorVideos)
        {
            videos.Add(new InputVideoData(){url = errorVideo, enable = true});
        }
        Console.WriteLine($"下载失败视频数量 ： \n{JsonConvert.SerializeObject(videos)}\n");
    }
}
