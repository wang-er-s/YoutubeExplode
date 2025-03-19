using System.Net;
using Newtonsoft.Json;
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
        Default = new Options();
        Default.ConfigData = JsonConvert.DeserializeObject<InputDownloadConfigData>(content)!;
        if (!string.IsNullOrEmpty(Options.Default.ConfigData.cookies_file))
        {
            Default.Cookies = LoadCookies(Options.Default.ConfigData.cookies_file);
        }
    }
    
    public static string GetVideoSavePath(Video video, InputAccountData? url = null)
    {
        string dir = video.Author.ChannelTitle;
        if (url != null)
        {
            dir = string.IsNullOrEmpty(url.mark) ? url.userName : url.mark;
        }

        return Path.Combine(Options.Default.ConfigData.root, dir, video.Id.Value, video.Id.Value + ".mp4");
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
            date = video.UploadDate.ToString("yyyy-MM-dd HH:mm:ss"),
            view_count = video.Engagement.ViewCount,
            like_count = video.Engagement.LikeCount,
            duration = video.Duration != null ? (int)video.Duration.Value.TotalSeconds : 0
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
    public long view_count { get; set; } = 0;
    public long like_count { get; set; } = 0;
    public long duration { get; set; } = 0;
}

public class InputAccountData
{
    public string mark { get; set; } = string.Empty;
    public string userName { get; set; } = string.Empty;
    public string url { get; set; } = string.Empty;
    public string earliest{ get; set; }
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
}

public class InputVideoData
{
    public string url { get; set; } = string.Empty;
    public bool enable { get; set; } = true;
}