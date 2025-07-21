using NoMercyBot.Database.Models.ChatMessage;
using NoMercyBot.Globals.Information;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using NoMercyBot.Services.Interfaces;

namespace NoMercyBot.Services.Other;

public class HtmlMetadataService: IService
{
    private readonly PermissionService _permissionService;
    
    private readonly HttpClient _httpClient;
    private Uri? _uri;
    private string? SiteTitle { get; set; } = "No title";
    private string? SiteDescription { get; set; }
    private string? SiteImageUrl { get; set; }
    
    private readonly string[] _trustedDomains = ["youtube.com", "youtu.be", "twitch.tv", "twitter.com", "instagram.com"];
    private readonly string[] _trustedImageDomains = ["imgur.com", "i.imgur.com", "cdn.discordapp.com", "twimg.com"];
    private readonly string[] _trustedUsers = ["ljtech", "stoney_eagle", "vvvvvedma_anna"];

    public HtmlMetadataService(PermissionService permissionService)
    {
        _permissionService = permissionService;
        
        // HttpClientHandler handler = new()
        // {
        //     Proxy = new WebProxy
        //     {
        //         Address = new("socks5://192.168.0.51:12345")
        //     }
        // };

        _httpClient = new();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", 
            $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/99.0.4844.74 Safari/537.36 Edg/99.0.1150.46 {Config.UserAgent}");
    }
    
    public async Task<HtmlPreviewCustomContent> MakeComponent(Uri uri)
    {
        _uri = uri;
        
        await DecorateOgData();
        await DecorateYoutube();
        await DecorateTwitch();
        
        return new(){
            Host = uri.Host,
            ImageUrl = SiteImageUrl,
            Title = SiteTitle,
            Description = SiteDescription,
        };
    }

    // public async Task<HtmlPreviewCustomContent?> GetMetadataAsync(Uri uri, string username, bool isBroadcaster, bool isModerator, bool isVip)
    // {
    //     try
    //     {
    //         HttpResponseMessage response = await _httpClient.GetAsync(uri);
    //         if (!response.IsSuccessStatusCode)
    //             return null;
    //
    //         string? contentType = response.Content.Headers.ContentType?.MediaType;
    //
    //         if (contentType == "text/html")
    //         {
    //             return await ProcessHtmlContent(response, uri);
    //         }
    //         else if (contentType?.StartsWith("image/") == true)
    //         {
    //             return await ProcessImageContent(response, uri, username, isBroadcaster, isModerator, isVip);
    //         }
    //
    //         return null;
    //     }
    //     catch
    //     {
    //         return null;
    //     }
    // }

    // private async Task<HtmlPreviewCustomContent?> ProcessHtmlContent(HttpResponseMessage response, Uri uri)
    // {
    //     string html = await response.Content.ReadAsStringAsync();
    //     var doc = new HtmlDocument();
    //     doc.LoadHtml(html);
    //
    //     NameValueCollection query = HttpUtility.ParseQueryString(uri.Query);
    //     string? youtubeVideoId = null;
    //
    //     // Check for YouTube URLs
    //     if (uri.Host.EndsWith("youtu.be") || (uri.Host.EndsWith("youtube.com") && query.AllKeys.Contains("v")))
    //     {
    //         youtubeVideoId = uri.Host.EndsWith("youtu.be") ? uri.LocalPath.Substring(1) : query["v"];
    //     }
    //
    //     var titleTag = doc.QuerySelector("title");
    //     var titleOgTag = doc.QuerySelector("meta[property=og:title]");
    //     var descriptionTag = doc.QuerySelector("meta[name=description]");
    //     var descriptionOgTag = doc.QuerySelector("meta[property=og:description]");
    //     var imageOgTag = doc.QuerySelector("meta[property=og:image]")?.GetAttributeValue("content", "");
    //
    //     if (!string.IsNullOrWhiteSpace(imageOgTag) && Uri.IsWellFormedUriString(imageOgTag, UriKind.Relative))
    //     {
    //         imageOgTag = uri.Scheme + "://" + uri.Host + imageOgTag;
    //     }
    //
    //     var title = titleOgTag?.GetAttributeValue("content", "") ?? titleTag?.InnerText;
    //     var description = descriptionOgTag?.GetAttributeValue("content", "") ?? descriptionTag?.GetAttributeValue("content", "");
    //
    //     if (string.IsNullOrWhiteSpace(title))
    //         return null;
    //
    //     return new()
    //     {
    //         Host = uri.Host,
    //         Title = title,
    //         Description = description,
    //         ImageUrl = imageOgTag,
    //         YoutubeVideoId = youtubeVideoId
    //     };
    // }
    //
    // private async Task<HtmlPreviewCustomContent?> ProcessImageContent(HttpResponseMessage response, Uri uri, string username, bool isBroadcaster, bool isModerator, bool isVip)
    // {
    //     // Only allow trusted users to embed images
    //     if (!isBroadcaster && !isModerator && !isVip && !_trustedUsers.Contains(username.ToLower()))
    //         return null;
    //
    //     byte[] bytes = await response.Content.ReadAsByteArrayAsync();
    //     string base64 = $"data:{response.Content.Headers.ContentType.MediaType};base64," + Convert.ToBase64String(bytes);
    //
    //     return new()
    //     {
    //         Host = uri.Host,
    //         ImageUrl = base64,
    //         IsDirectImage = true
    //     };
    // }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private async Task DecorateOgData()
    {
        if (_uri == null) return;

        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(_uri);
            if (!response.IsSuccessStatusCode) return;

            string? contentType = response.Content.Headers.ContentType?.MediaType;
            if (contentType != "text/html") return;

            string html = await response.Content.ReadAsStringAsync();
            HtmlDocument doc = new();
            doc.LoadHtml(html);

            // Get OpenGraph meta tags
            HtmlNode? titleOgTag = doc.QuerySelector("meta[property='og:title']");
            HtmlNode? descriptionOgTag = doc.QuerySelector("meta[property='og:description']");
            HtmlNode? imageOgTag = doc.QuerySelector("meta[property='og:image']");

            // Fallback to standard HTML tags if OG tags don't exist
            HtmlNode? titleTag = doc.QuerySelector("title");
            HtmlNode? descriptionTag = doc.QuerySelector("meta[name='description']");

            string? title = titleOgTag?.GetAttributeValue("content", "") ?? titleTag?.InnerText.Trim();
            if (!string.IsNullOrWhiteSpace(title))
            {
                SiteTitle = title;
            }

            string? description = descriptionOgTag?.GetAttributeValue("content", "") ?? 
                                 descriptionTag?.GetAttributeValue("content", "");
            if (!string.IsNullOrWhiteSpace(description))
            {
                SiteDescription = description;
            }

            string? imageUrl = imageOgTag?.GetAttributeValue("content", "");
            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                // Convert relative URLs to absolute
                if (Uri.IsWellFormedUriString(imageUrl, UriKind.Relative))
                {
                    imageUrl = _uri.Scheme + "://" + _uri.Host + imageUrl;
                }
                SiteImageUrl = imageUrl;
            }
        }
        catch (Exception)
        {
            // Keep default values if fetching fails
        }
    }

    private async Task DecorateYoutube()
    {
        // if is youtube video resolve video
        // if is short tell user to stop doomscrolling with snarky reply
        // if is youtube channel resolve channel
        await Task.CompletedTask;
    }

    private async Task DecorateTwitch()
    {
        // if is channel resolve shoutout
        // if is video resolve video
        await Task.CompletedTask;
    }
}