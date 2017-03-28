using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TwitchEmoteDownloader.Entities;

namespace TwitchEmoteDownloader
{
    class Program
    {
        static void Main(string[] args)
        {
            DownloadTwitchEmotes();
            DownloadBttvEmotes();
        }

        private static void DownloadTwitchEmotes()
        {
            var wc = new WebClient();
            var result = wc.DownloadString("https://api.twitch.tv/kraken/chat/emoticons");

            var emoticonResult = JsonConvert.DeserializeObject<EmoticonResponseContract>(result);


            var globalEmotes = emoticonResult.emoticons.Where(q => q.images.Any(p => p.emoticon_set == null));

            if (!globalEmotes.Any()) return;

            var wrapper = new EmoticonWrapper();

            Parallel.For(0, globalEmotes.Count(), (i) =>
            {
                var mote = globalEmotes.ElementAt(i);
                using (var client = new WebClient())
                {
                    mote.images.ForEach((m) =>
                    {
                        if (m.emoticon_set == null)
                        {
                            client.DownloadFile(m.url, Directory.GetCurrentDirectory() + "/Emoticons/Global/" + m.url.Substring(m.url.LastIndexOf("/") + 1));
                            wrapper.Emoticons.Add(new Emo()
                            {
                                Name = mote.regex,
                                ImageName = m.url.Substring(m.url.LastIndexOf("/") + 1)
                            });
                        }
                    });
                }
            });

            Debug.WriteLine(JsonConvert.SerializeObject(wrapper));
        }

        private static void DownloadBttvEmotes()
        {
            var wc = new WebClient();
            var result = wc.DownloadString("https://api.betterttv.net/emotes");

            var emoticonResult = JsonConvert.DeserializeObject<BttvEmoticonResponseContract>(result);


            var globalEmotes = emoticonResult.emotes.Where(q => q.channel == null);

            if (!globalEmotes.Any()) return;

            var wrapper = new EmoticonWrapper();

            Parallel.For(0, globalEmotes.Count(), (i) =>
            {
                var mote = globalEmotes.ElementAt(i);
                using (var client = new WebClient())
                {
                    mote.regex = mote.regex.Replace(":", "");
                    mote.url = "http:" + mote.url;

                    client.DownloadFile(mote.url, Directory.GetCurrentDirectory() + "/Emoticons/BTTV/" + mote.url + ".png");
                }

                wrapper.Emoticons.Add(new Emo()
                {
                    Name = mote.regex,
                    ImageName = mote.regex
                });

            });

            Debug.WriteLine(JsonConvert.SerializeObject(wrapper));
        }
    }

    class EmoticonWrapper
    {
        public List<Emo> Emoticons = new List<Emo>();
    }

    class Emo
    {
        public string Name { get; set; }
        public string ImageName { get; set; }
    }
}
