using System.Net;
using Newtonsoft.Json;
using YoutubeDownloader.Core.Downloading;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace Downloader;

public class Options
{
    public static Options Default;
    public InputDownloadConfigData ConfigData;
    public List<Cookie> Cookies;

    public static void Parse(string configFilePath)
    {
        string content = File.ReadAllText(configFilePath);
        // string content = """
        //     {
        //         "max_retry": 5,
        //         "root": "C:/Users/ss/Desktop/VideoDownload",
        //         "cookies_file": "C:/Users/ss/Desktop/VideoDownload/ytb_cookies.txt",
        //         "download_max_duration": 10000,
        //         "accounts_enable": "true",
        //         "save_format": "aa.mp4",
        //         "accounts": [
        //         ],
        //         "videos_enable": "true",
        //         "videos": [
        //         {
        //             "url": "https://www.youtube.com/watch?v=IY5NfYlYtnU",
        //             "enable": true
        //         }
        //         ],
        //         "quality": 1080
        //     }
        //     """;
        Default = new Options();
        Default.ConfigData = JsonConvert.DeserializeObject<InputDownloadConfigData>(content)!;
        if (!string.IsNullOrEmpty(Options.Default.ConfigData.cookies_file))
        {
            Default.Cookies = LoadCookies(Options.Default.ConfigData.cookies_file);
        }
    }
    
    public static string GetVideoSavePath(Video video, InputAccountData? url = null)
    {
        if(string.IsNullOrEmpty(Options.Default.ConfigData.save_format))
        {
            string dir = video.Author.ChannelTitle;
            if (url != null)
            {
                dir = string.IsNullOrEmpty(url.mark) ? url.userName : url.mark;
            }

            return Path.Combine(Options.Default.ConfigData.root, dir, video.Id.Value, video.Id.Value + ".mp4");
        }
        else
        {
            var path = Options.Default.ConfigData.save_format;
            path = path.Replace("{author}", video.Author.ChannelTitle);
            path = path.Replace("{video_id}", video.Id.Value);
            return Path.GetFullPath(Path.Combine(Options.Default.ConfigData.root, path));
        }
    }

    public static Container GetContainer(string savePath)
    {
        if(savePath.EndsWith(".mp4"))
        {
            return Container.Mp4;
        }
        return Container.Mp3;
    }
    
    private static string GetVideoConfigSavePath(Video video, InputAccountData? url = null)
    {
        var saveDir = Path.GetDirectoryName(GetVideoSavePath(video, url));
        var configFile = Path.Combine(saveDir, "data.json");
        return configFile;
    }
    
    public static void SaveVideoConfig(Video video, InputAccountData? accountData = null)
    {
        var configFile = GetVideoConfigSavePath(video, accountData);
                        
        SaveVideoConfig config = new SaveVideoConfig()
        {
            id = video.Id.Value,
            name = video.Title,
            url = video.Url,
            author = video.Author.ChannelTitle,
            date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            videoDate = video.UploadDate.ToString("yyyy-MM-dd HH:mm:ss"),
            duration = video.Duration != null ? (int)video.Duration.Value.TotalSeconds : 0,
            isStar = accountData?.isStar ?? false
        };
        Directory.CreateDirectory(Path.GetDirectoryName(configFile));
        File.WriteAllText(configFile, JsonConvert.SerializeObject(config, Formatting.Indented));
    }
    
    public static IProgress<double> GetProgressLog()
    {
        Progress<double> progress = new Progress<double>(value =>
        {
            Console.WriteLine($"进度: {value * 100:.0}%"); // 在 UI 线程上更新 UI
        });
        return progress;
    }

    static List<Cookie> LoadCookies(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Cookie file not found. {filePath}");
            return null;
        }

        var cookiesDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(filePath));
        List<Cookie> result = new();
        foreach (var kv in cookiesDic)
        {
            Cookie cookie = new Cookie(kv.Key, kv.Value)
            {
                Domain =  ".youtube.com",
                Path = "/",
                Secure = true,
            };
            result.Add(cookie);
        }
        return result;
    }
}

public class InputDownloadConfigData
{
    public string cookies_file { get; set; }
    public int max_retry { get; set; }
    public string root { get; set; } = string.Empty;
    public float download_max_duration { get; set; }
    public bool accounts_enable { get; set; }
    public bool videos_enable { get; set; }
    public int quality { get; set; }
    public string save_format { get; set; } = string.Empty;

    public VideoQualityPreference VideoQualityPreference
    {
        get
        {
            return quality switch
            {
                < 360 => VideoQualityPreference.Lowest,
                < 480 => VideoQualityPreference.UpTo360p,
                < 720 => VideoQualityPreference.UpTo480p,
                < 1080 => VideoQualityPreference.UpTo720p,
                1080 => VideoQualityPreference.UpTo1080p,
                _ => VideoQualityPreference.Highest
            };
        }
    }
    public List<InputAccountData> accounts { get; set; } = new List<InputAccountData>();
    public List<InputVideoData> videos { get; set; } = new List<InputVideoData>();
}

public class SaveVideoConfig
{
    public string id { get; set; } = String.Empty;
    public string name { get; set; } = String.Empty;
    public string url { get; set; } = String.Empty;
    public string author { get; set; } = String.Empty;
    public string date { get; set; } = String.Empty;
    public string videoDate { get; set; } = String.Empty;
    public long duration { get; set; } = 0;
    public bool isStar { get; set; } = false;
}

public class InputAccountData
{
    public string mark { get; set; } = string.Empty;
    public string userName { get; set; } = string.Empty;
    public string url { get; set; } = string.Empty;
    public string earliest{ get; set; }
    public string videoFilter{ get; set; } = string.Empty;
    public DateTime earliestDate
    {
        get
        {
            var strs = earliest.Split('/');
            return new DateTime(int.Parse(strs[0]), int.Parse(strs[1]), int.Parse(strs[2]));
        }
    }

    public bool enable { get; set; } = true;
    public bool isStar { get; set; } = false;
    public float download_max_duration { get; set; } = -1;
    public float download_min_duration { get; set; } = -1;
}

public class InputVideoData
{
    public string url { get; set; } = string.Empty;
    public bool enable { get; set; } = true;
    public string earliest{ get; set; } = string.Empty;
    public DateTime earliestDate
    {
        get
        {
            var strs = earliest.Split('/');
            return new DateTime(int.Parse(strs[0]), int.Parse(strs[1]), int.Parse(strs[2]));
        }
    }
}