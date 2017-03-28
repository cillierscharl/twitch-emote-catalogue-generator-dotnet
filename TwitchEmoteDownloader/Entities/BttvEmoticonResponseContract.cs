using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchEmoteDownloader.Entities
{
    public class BttvEmoticonResponseContract
    {
        public int status { get; set; }
        public List<Emote> emotes { get; set; }
    }

    public class Emote
    {
        public string url { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public string imageType { get; set; }
        public string regex { get; set; }
        public string channel { get; set; }
        public string emoticon_set { get; set; }
    }
}
