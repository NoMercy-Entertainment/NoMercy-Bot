using System.Text;
using NoMercyBot.Database.Models.ChatMessage;
using NoMercyBot.Services.Emotes;
using NoMercyBot.Services.Emotes.Dto;
using NoMercyBot.Services.Other;

namespace NoMercyBot.Services.Twitch;

public class TwitchMessageDecorator
{
    private readonly FrankerFacezService _frankerFacezService;
    private readonly BttvService _bttvService;
    private readonly SevenTvService _sevenTvService;
    private readonly HtmlMetadataService _htmlMetadataService;
    private readonly ParallelOptions _parallelOptions;
    
    private ChatMessage ChatMessage { get; set; }
    private List<ChatMessageFragment> _fragments = [];

    public TwitchMessageDecorator(
        FrankerFacezService frankerFacezService,
        BttvService bttvService,
        SevenTvService sevenTvService, 
        HtmlMetadataService htmlMetadataService, 
        CancellationToken cancellationToken = default)
    {
        _frankerFacezService = frankerFacezService;
        _bttvService = bttvService;
        _sevenTvService = sevenTvService;
        _htmlMetadataService = htmlMetadataService;
        
        ChatMessage = new();
        
        _parallelOptions = new()
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = (int)Math.Floor(Environment.ProcessorCount / 2.0)
        };
    }

    public async Task DecorateMessage(ChatMessage chatMessage)
    {
        ChatMessage = chatMessage;
        // Copy the fragments to a new list to avoid modifying the original message
        _fragments = chatMessage.Fragments.ToList();
        
        // Split up all text fragments into individual word fragments so that we can decorate them with emotes
        ExplodeTextFragments();
        
        DecorateTwitchEmotes();
        
        DecorateFrankerFaceEmotes();
        DecorateBttvEmotes();
        DecorateSevenTvEmotes();
        
        await DecorateUrlFragments();
        
        // Merge all consecutive text fragments back together to avoid having too many text fragments
        ImplodeTextFragments();
        
        DecorateCodeSnippet();
        
        // Overwrite the original fragments with the modified ones
        ChatMessage.Fragments = _fragments;
    }

    private void ExplodeTextFragments()
    {
        List<ChatMessageFragment> newFragments = [];

        foreach (ChatMessageFragment fragment in _fragments)
        {
            int index = _fragments.IndexOf(fragment);

            if (fragment.Type != "text")
            {
                newFragments.Add(fragment);
                
                if (_fragments.ElementAtOrDefault(index - 0)?.Text != " ")
                {
                    newFragments.Add(new()
                    {
                        Type = "text",
                        Text = " ",
                    });
                }
                
                continue;
            }

            string[] words = fragment.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (string word in words)
            {
                if (newFragments.Count > 0 || _fragments.ElementAtOrDefault(index - 0)?.Text != " ")
                {
                    newFragments.Add(new()
                    {
                        Type = "text",
                        Text = " ",
                    });
                }
                
                newFragments.Add(new()
                {
                    Type = "text",
                    Text = string.IsNullOrEmpty(word) 
                    // replace all versions of whitespace with a single space
                        ? $"{word}"
                        : word,
                });
                
                if(newFragments.LastOrDefault()?.Text != " ")
                {
                    newFragments.Add(new()
                    {
                        Type = "text",
                        Text = " ",
                    });
                }
            }
        }
        
        if(newFragments.FirstOrDefault()?.Text == " ")
        {
            newFragments.RemoveAt(0);
        }
        
        if(newFragments.LastOrDefault()?.Text == " ")
        {
            newFragments.RemoveAt(newFragments.Count - 1);
        }

        _fragments = newFragments;
    }
    
    private void ImplodeTextFragments()
    {
        List<ChatMessageFragment> newFragments = [];
        StringBuilder currentText = new();

        foreach (ChatMessageFragment fragment in _fragments)
        {
            if (fragment.Type == "text")
            {
                currentText.Append(fragment.Text);
            }
            else
            {
                if (currentText.Length > 0)
                {
                    newFragments.Add(new()
                    {
                        Type = "text",
                        Text = currentText.ToString()
                    });
                    currentText.Clear();
                }
                
                newFragments.Add(fragment);
            }
        }

        if (currentText.Length > 0)
        {
            newFragments.Add(new()
            {
                Type = "text",
                Text = currentText.ToString()
            });
        }
        
        if(newFragments.LastOrDefault()?.Text == " ")
        {
            newFragments.RemoveAt(newFragments.Count - 1);
        }

        _fragments = newFragments;
    }
    
    private void DecorateTwitchEmotes()
    {
        Parallel.ForEach(_fragments.ToList(), _parallelOptions, (fragment) =>
        {
            if (fragment.Type != "emote" || fragment.Emote is null) return;

            int index = _fragments.IndexOf(fragment);
            _fragments[index] = new()
            {
                Type = fragment.Type,
                Text = fragment.Text,
                Cheermote = fragment.Cheermote,
                Mention = fragment.Mention,
                Emote = new()
                {
                    Id = fragment.Emote.Id,
                    Format = fragment.Emote.Format,
                    OwnerId =  fragment.Emote.OwnerId,
                    EmoteSetId =  fragment.Emote.EmoteSetId,
                    Provider = "twitch",
                    Urls = new()
                    {
                        { "1", new($"https://static-cdn.jtvnw.net/emoticons/v2/{fragment.Emote.Id}/default/dark/1.0") },
                        { "2", new($"https://static-cdn.jtvnw.net/emoticons/v2/{fragment.Emote.Id}/default/dark/2.0") },
                        { "3", new($"https://static-cdn.jtvnw.net/emoticons/v2/{fragment.Emote.Id}/default/dark/3.0") }
                    }
                }
            };
        });
    }
    
    private void DecorateFrankerFaceEmotes()
    {
        Parallel.ForEach(_fragments.ToList(), _parallelOptions, fragment =>
        {
            if (fragment.Type != "text" || string.IsNullOrWhiteSpace(fragment.Text)) return;

            Emoticon? emote = _frankerFacezService.FrankerFacezEmotes
                .FirstOrDefault(e => e.Name.Equals(fragment.Text, StringComparison.OrdinalIgnoreCase));
            if (emote == null) return;

            int index = _fragments.IndexOf(fragment);
            _fragments[index] = new()
            {
                Type = "emote",
                Text = fragment.Text,
                Emote = new()
                {
                    Id = emote.Id.ToString(),
                    Provider = "frankerfacez",
                    Urls = emote.Urls,
                }
            };
        });
    }

    private void DecorateBttvEmotes()
    {
        Parallel.ForEach(_fragments.ToList(), _parallelOptions, fragment =>
        {
            if (fragment.Type != "text" || string.IsNullOrWhiteSpace(fragment.Text)) return;

            BttvEmote? emote = _bttvService.BttvEmotes
                .FirstOrDefault(e => e.Code.Equals(fragment.Text, StringComparison.OrdinalIgnoreCase));
            if (emote == null) return;

            int index = _fragments.IndexOf(fragment);
            _fragments[index] = new()
            {
                Type = "emote",
                Text = fragment.Text,
                Emote = new()
                {
                    Id = emote.Id,
                    Provider = "bttv",
                    Urls = new()
                    {
                        { "1", new($"https://cdn.betterttv.net/emote/{emote.Id}/1x") },
                        { "2", new($"https://cdn.betterttv.net/emote/{emote.Id}/2x") },
                        { "3", new($"https://cdn.betterttv.net/emote/{emote.Id}/3x") }
                    }
                }
            };
        });
    }

    private void DecorateSevenTvEmotes()
    {
        Parallel.ForEach(_fragments.ToList(), _parallelOptions, fragment =>
        {
            if (fragment.Type != "text" || string.IsNullOrWhiteSpace(fragment.Text)) return;

            SevenTvEmote? emote = _sevenTvService.SevenTvEmotes
                .FirstOrDefault(e => e.Name.Equals(fragment.Text, StringComparison.OrdinalIgnoreCase));
            if (emote == null) return;

            int index = _fragments.IndexOf(fragment);
            _fragments[index] = new()
            {
                Type = "emote",
                Text = fragment.Text,
                Emote = new()
                {
                    Id = emote.Id,
                    Provider = "7tv",
                    Urls = new()
                    {
                        { "1", new($"https://7tv.app/emotes/{emote.Id}/1x") },
                        { "2", new($"https://7tv.app/emotes/{emote.Id}/2x") },
                        { "3", new($"https://7tv.app/emotes/{emote.Id}/3x") },
                        { "4", new($"https://7tv.app/emotes/{emote.Id}/4x") },
                    }
                }
            };
        });
    }
    
    private void DecorateCodeSnippet()
    {
        // throw new NotImplementedException();
    }
    
    private async Task DecorateUrlFragments()
    {
        foreach (ChatMessageFragment fragment in _fragments.ToList())
        {
            if (fragment.Type != "text" || string.IsNullOrWhiteSpace(fragment.Text)) continue;

            // Check if the text is a URL
            if (!Uri.TryCreate(fragment.Text, UriKind.Absolute, out Uri? uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)) continue;
            
            int index = _fragments.IndexOf(fragment);
            _fragments[index] = new()
            {
                Type = "url",
                Text = fragment.Text,
                HtmlContent = await _htmlMetadataService.MakeComponent(uri),
            };
        }
    }
}