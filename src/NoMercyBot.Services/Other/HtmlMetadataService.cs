﻿using NoMercyBot.Database.Models.ChatMessage;
using NoMercyBot.Globals.Information;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using NoMercyBot.Services.Interfaces;

namespace NoMercyBot.Services.Other;

public class HtmlMetadataService : IService
{
    private readonly PermissionService _permissionService;

    private readonly HttpClient _httpClient;
    private Uri? _uri;
    private string? SiteTitle { get; set; } = "No title";
    private string? SiteDescription { get; set; }
    private string? SiteImageUrl { get; set; }

    private readonly string[] _trustedDomains =
        ["youtube.com", "youtu.be", "twitch.tv", "twitter.com", "instagram.com"];

    private readonly string[] _trustedImageDomains = ["imgur.com", "i.imgur.com", "cdn.discordapp.com", "twimg.com"];
    private readonly string[] _trustedUsers = ["ljtech", "stoney_eagle", "kanawanagasaki"];

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

    public async Task<HtmlPreviewCustomContent> MakeComponent(Uri uri, bool permitted)
    {
        _uri = uri;
        SiteTitle = "No title";
        SiteDescription = null;
        SiteImageUrl = null;

        await DecorateOgData(permitted);
        await DecorateYoutube();
        await DecorateTwitch();

        return new()
        {
            Host = _uri.Host,
            ImageUrl = permitted
                ? SiteImageUrl
                : null,
            Title = SiteTitle,
            Description = SiteDescription
        };
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private async Task DecorateOgData(bool permitted)
    {
        if (_uri == null) return;

        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(_uri);
            if (!response.IsSuccessStatusCode) return;

            string? contentType = response.Content.Headers.ContentType?.MediaType;

            if (contentType == "text/html")
            {
                await ProcessHtmlContent(response, _uri);
                return;
            }

            if (contentType?.StartsWith("image/") == true && permitted)
            {
                await ProcessImageContent(response, _uri);
                return;
            }
        }
        catch (Exception)
        {
            // Keep default values if fetching fails
        }
    }

    private async Task ProcessImageContent(HttpResponseMessage response, Uri uri)
    {
        string imageUrl = uri.ToString();
        // Convert relative URLs to absolute
        if (Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute))
        {
            byte[] bytes = await response.Content.ReadAsByteArrayAsync();
            string base64 = $"data:{response.Content.Headers.ContentType?.MediaType};base64," +
                            Convert.ToBase64String(bytes);
            SiteImageUrl = base64;
        }
    }

    private async Task ProcessHtmlContent(HttpResponseMessage response, Uri uri)
    {
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
        if (!string.IsNullOrWhiteSpace(title)) SiteTitle = title;

        string? description = descriptionOgTag?.GetAttributeValue("content", "") ??
                              descriptionTag?.GetAttributeValue("content", "");
        if (!string.IsNullOrWhiteSpace(description)) SiteDescription = description;

        string? imageUrl = imageOgTag?.GetAttributeValue("content", "");
        if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            // Convert relative URLs to absolute
            if (Uri.IsWellFormedUriString(imageUrl, UriKind.Relative))
                imageUrl = uri.Scheme + "://" + uri.Host + imageUrl;

            SiteImageUrl = imageUrl;
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

    public bool ValidateHtml(string fragmentText, out HtmlDocument htmlDocument)
    {
        htmlDocument = new();
        try
        {
            htmlDocument.LoadHtml(fragmentText);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}