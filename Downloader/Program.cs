﻿using Newtonsoft.Json;

namespace Downloader;

class Program
{
    static async Task Main(string[] args)
    {
        Options.Parse(args[0]);
        List<string> errorVideos = new List<string>();
        if (Options.Default.ConfigData.accounts_enable)
        {
            int index = 0;
            foreach (var userUrl in Options.Default.ConfigData.accounts)
            {
                Console.WriteLine($"开始下载账号：{++index} / {Options.Default.ConfigData.accounts.Count}");
                if (!userUrl.enable) continue;
                var downloadAccount = new DownloadAccount(userUrl);
                await downloadAccount.Download();
                errorVideos.AddRange(downloadAccount.ErrorVideo);
            }
        }

        if (Options.Default.ConfigData.videos_enable)
        {
            int index = 0;
            foreach (var inputVideoData in Options.Default.ConfigData.videos)
            {
                Console.WriteLine($"开始下载视频：{++index} / {Options.Default.ConfigData.videos.Count}");
                if (!inputVideoData.enable) continue;
                var downVideo = new DownloadOneVideo(inputVideoData);
                await downVideo.DownloadVideo();
                if (downVideo.Error)
                    errorVideos.Add(inputVideoData.url);
            }
        }

        List<InputVideoData> videos = new List<InputVideoData>();
        if (errorVideos.Count > 0)
        {
            Console.WriteLine("再次下载失败的视频。。。");
            int index = 0;
            foreach (var errorVideo in errorVideos)
            {
                Console.WriteLine($"重新下载失败视频：{++index} / {errorVideos.Count}");
                var inputVideoData = new InputVideoData() { url = errorVideo, enable = true };
                var downVideo = new DownloadOneVideo(inputVideoData);
                await downVideo.DownloadVideo();
                if (downVideo.Error)
                    videos.Add(inputVideoData);
            }
        }
        Console.WriteLine($"下载失败视频数量 ： \n{JsonConvert.SerializeObject(videos)}\n");
    }
}
