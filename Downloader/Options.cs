using System.Net;
using Newtonsoft.Json;
using YoutubeDownloader.Core.Downloading;
using YoutubeExplode.Videos;

namespace Downloader;

public class Options
{
    public static Options Default;
    public InputDownloadConfigData ConfigData;
    public List<Cookie> Cookies;

    public static void Parse(string configFilePath)
    {
        string content = File.ReadAllText(configFilePath);
        // content = """
        //     {
        //         "max_retry": 5,
        //         "root": "C:/Users/18530/Desktop/Video/TalkShow",
        //         "cookies_file": "C:/Users/18530/Desktop/Video/Downloader/ytb_cookies.txt",
        //         "download_max_duration": 10,
        //         "accounts_enable": "true",
        //         "accounts": [
        //         ],
        //         "videos_enable": "true",
        //         "videos": [
        //         {
        //             "url": "https://www.youtube.com/watch?v=IY5NfYlYtnU&ab_channel=TheDailyShow",
        //             "enable": true
        //         }
        //         ]
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

            return Path.Combine(Options.Default.ConfigData.root, dir, video.Id.Value, video.Id.Value + (Default.ConfigData.only_audio ? ".mp3" : ".mp4"));
        }
        else
        {
            var path = Options.Default.ConfigData.save_format;
            path = path.Replace("{author}", video.Author.ChannelTitle);
            path = path.Replace("{video_id}", video.Id.Value);
            return Path.GetFullPath(Path.Combine(Options.Default.ConfigData.root, path));
        }
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
            view_count = video.Engagement.ViewCount,
            like_count = video.Engagement.LikeCount,
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
    public bool only_audio { get; set; }
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
    public long view_count { get; set; } = 0;
    public long like_count { get; set; } = 0;
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

    public DateTime latestDate
    {
        get
        {
            if (string.IsNullOrEmpty(latest))
            {
                return new DateTime(9999, 1, 1);
            }
            var strs = latest.Split('/');
            return new DateTime(int.Parse(strs[0]), int.Parse(strs[1]), int.Parse(strs[2]));
        }
    }
    public string latest{ get; set; }
    public bool enable { get; set; } = true;
    public bool isStar { get; set; } = false;
    public float download_max_duration { get; set; } = -1;
    public float download_min_duration { get; set; } = -1;
}

public class InputVideoData
{
    public string url { get; set; } = string.Empty;
    public bool enable { get; set; } = true;
}