using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TwitchEmoteDownloader.Entities;

namespace TwitchEmoteDownloader
{
    public class Application
    {
        public void Run()
        {
            var twitchResult = DownloadTwitchEmotes();
            var bttvResult = DownloadBttvEmotes();
            ProcessResults(twitchResult, bttvResult);
        }

        private List<LocalEmote> DownloadTwitchEmotes()
        {
            var httpClient = new WebClient();
            var webResult = httpClient.DownloadString("https://api.twitch.tv/kraken/chat/emoticons");
            var emoticonResult = JsonConvert.DeserializeObject<EmoticonResponseContract>(webResult);
            var globalEmotes = emoticonResult.emoticons.Where(q => q.images.Any(p => p.emoticon_set == null));

            if (!globalEmotes.Any())
                return null;

            var results = new List<LocalEmote>();

            Parallel.For(0, globalEmotes.Count(), (i) =>
            {
                var emote = globalEmotes.ElementAt(i);
                using (var client = new WebClient())
                {
                    emote.images.ForEach((m) =>
                    {
                        if (m.emoticon_set == null)
                        {
                            // HTML Decode is not an option due the values being escaped in regex form, sanitize as neccesary
                            emote.regex = emote.regex.Replace(@"\&lt\;", "<");
                            emote.regex = emote.regex.Replace(@"\&gt\;", ">");
                            // Generate 100 valid values of the Regex value and pick the shortest one
                            var settings = new Rex.RexSettings(emote.regex);
                            settings.encoding = Rex.CharacterEncoding.ASCII;
                            settings.k = 100;

                            var minimulViableRegexValue = Rex.RexEngine.GenerateMembers(settings).OrderBy(s => s.Length).First();

                            client.DownloadFile(m.url, Directory.GetCurrentDirectory() + "/Emoticons/Global/" + m.url.Substring(m.url.LastIndexOf("/") + 1));
                            results.Add(new LocalEmote()
                            {
                                Name = minimulViableRegexValue,
                                ImageName = string.Format("Global/{0}", m.url.Substring(m.url.LastIndexOf("/") + 1))
                            });
                        }
                    });
                }
            });
            var orderedResults = results.OrderBy(x =>
            {
                // Smiles
                if (Regex.IsMatch(x.Name, "[:|;|>|<|>]"))
                    return 0;

                // Text emotes
                if (Regex.IsMatch(x.Name, "[a-zA-Z]"))
                    return 1;

                //Everything else
                return 2;
            });
            return orderedResults.ToList();

        }

        private List<LocalEmote> DownloadBttvEmotes()
        {

            var httpClient = new WebClient();
            var webResult = httpClient.DownloadString("https://api.betterttv.net/emotes");
            var emoticonResult = JsonConvert.DeserializeObject<BttvEmoticonResponseContract>(webResult);
            var globalEmotes = emoticonResult.emotes.Where(q => q.channel == null);

            if (!globalEmotes.Any())
                return null;

            var results = new List<LocalEmote>();
            var bttvUrlRegex = new Regex(@"^(?:[^\/]*\/){4}([^\/]*)");

            Parallel.For(0, globalEmotes.Count(), (i) =>
            {
                var emote = globalEmotes.ElementAt(i);
                var matches = bttvUrlRegex.Match(emote.url);
                var name = matches.Groups[1];

                using (var client = new WebClient())
                {
                    emote.url = "http:" + emote.url;
                    client.DownloadFile(emote.url, Directory.GetCurrentDirectory() + "/Emoticons/BTTV/" + name + ".png");
                }

                results.Add(new LocalEmote()
                {
                    Name = emote.regex,
                    ImageName = string.Format("BTTV/{0}.{1}", name.ToString(), "png")
                });
            });
            return results;
        }

        private void ProcessResults(List<LocalEmote> twitchResults, List<LocalEmote> bttvResults)
        {
            var generationResult = new GeneratorResult() { EmoticonLocal = twitchResults, BTTVLocal = bttvResults };
            var result = JsonConvert.SerializeObject(generationResult);
            File.WriteAllText("./GlobalEmoticons.json", result);
        }

    }
}
