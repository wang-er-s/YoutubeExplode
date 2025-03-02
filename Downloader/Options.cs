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
        // content = """
        //                  {
        //                  "max_retry": 3,
        //                  "cookies_file": "C:/Users/admin/Desktop/Video/www.youtube.com_cookies.txt",
        //                  "root" : "C:/Users/admin/Desktop/Video",
        //                  "accounts": [
        //                    {
        //                      "mark": "lolflix11",
        //                      "url": "https://www.youtube.com/@RonnyChieng",
        //                      "earliest": "2024/12/22",
        //                      "latest": "",
        //                      "enable": false
        //                    }
        //                  ],
        //                  "videos":[
        //                  {
        //                     "url":"https://www.youtube.com/watch?v=cQBqHSV6eOw&ab_channel=TheDailyShow"
        //                  }
        //                  ]
        //                  }
        //                  """;

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
            date = video.UploadDate.ToString("yyyy-MM-dd HH:mm:ss")
        };
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

        List<Cookie> result = new();
        foreach (var line in File.ReadLines(filePath))
        {
            // 跳过注释行和空行
            if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line))
                continue;

            // 分割行，获取 cookie 的属性
            var parts = line.Split('\t');
            if (parts.Length != 7)
                continue; // 确保行格式正确

            string domain = parts[0];
            bool isSecure = parts[1].Equals("TRUE", StringComparison.OrdinalIgnoreCase);
            string path = parts[2];
            long expires = long.Parse(parts[4]);
            string name = parts[5];
            string value = parts[6];

            // 创建 Cookie 并添加到 CookieContainer
            Cookie cookie = new Cookie(name, value)
            {
                Domain = domain,
                Path = path,
                Secure = isSecure,
                Expires = DateTime.FromFileTime(expires)
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