using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Downloader
{
    public class Members
    {
        public string LastUpdate { get; set; }
        public List<Member> Elements { get; set; }
        public Members()
        {
            Elements = new List<Member>();
        }
    }
    public class Member : ITitle, ILocalInfo
    {
        public string Title {get;set;}
        public int Subs { get; set; }
        public int NumberOfVideos { get; set; }
        public int ProcessedNumberOfVideos { get; set; }
        public bool IsLocal { get; set; }
        public string LocalPath { get; set; }
        public string Link { get; set; }
    }
}
