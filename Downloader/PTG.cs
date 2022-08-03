using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Downloader
{
    class Profile
    {
        public List<Entry> entry = new List<Entry>();
    }
    class Entry
    {
        public string Alias { get; set; }
        public string WebSite { get; set; }
    }
    class PTG
    {
        public async static Task<Profile> GirlAliases(string url)
        {
            try
            {
                string[] import = File.ReadAllLines(@"C:\Users\dmytr\source\repos\Downloader\Downloader\bin\x64\Debug\PrefixId.csv");
                var viperSites = import.ToList().Select(x =>
                {
                    if (string.IsNullOrWhiteSpace(x) || !x.Contains("_"))
                        return x.ToUpper();
                    if (x.Contains("_"))
                        return x.Substring(0, x.IndexOf('_')).ToUpper();
                    return "";
                }).ToList();
                string html = await ReturnHtml(url);
                HtmlDocument hDoc = new HtmlDocument();
                hDoc.LoadHtml(html);
                var _sitenameNodes = hDoc.DocumentNode.SelectNodes("//*[@id=\"info\"]//td[1]//ul//strong");
                List<HtmlNode> sitenameNodes = new List<HtmlNode>();
                _sitenameNodes.ToList().ForEach(node =>
                {
                    var n = node.ParentNode.Name == "a" ? node.ParentNode : node;
                    var sibling = n.NextSibling;
                    do
                    {
                        if (sibling.GetAttributeValue("class", "") == "vert")
                        {
                            sitenameNodes.Add(node);
                            break;
                        }
                        sibling = sibling.NextSibling;
                    }
                    while (sibling.Name != "br");

                });
                var aliasNodes = hDoc.DocumentNode.SelectNodes("//*[@id=\"info\"]//td[1]//ul//*[@class=\"vert\"]");
                //var type = hDoc.DocumentNode.SelectNodes("//*[@id=\"info\"]//td[1]//ul//*[@class=\"bleu\"]");
                Profile profile = new Profile();
                for (int i = 0; i < sitenameNodes.Count; i++)
                {
                    string alias = aliasNodes[i].InnerText.Trim();
                    string sitename = sitenameNodes[i].InnerText.Trim().Replace("-", "").Trim();
                    if (string.IsNullOrWhiteSpace(alias))
                        continue;
                    //Console.WriteLine(sitename);
                    if (viperSites.Find(t => t.ToUpper() == sitename.ToUpper()) != null)
                    {
                        int index = viperSites.IndexOf(sitename.ToUpper());
                        string[] aliases = alias.Split(',');
                        foreach (string al in aliases)
                        {
                            Entry entry = new Entry
                            {
                                WebSite = import[index],
                                Alias = al.Trim()
                            };
                            profile.entry.Add(entry);
                        }

                    }
                }
                return profile;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return new Profile();
            }

        }


        private async static Task<string> ReturnHtml(string url)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var req0 = WebRequest.CreateHttp(new Uri(url));
                    req0.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:77.0) Gecko/20100101 Firefox/77.0";
                    req0.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                    req0.Headers.Add("Accept-Language: uk-UA,uk;q=0.8,en-US;q=0.5,en;q=0.3");
                    req0.Timeout = 15000;
                    req0.AllowAutoRedirect = false;
                    req0.KeepAlive = true;
                    using (var res0 = (HttpWebResponse)req0.GetResponse())
                    {
                        using (var stream = new StreamReader(res0.GetResponseStream()))
                        {
                            return stream.ReadToEnd();
                        }
                    }
                }
                catch
                {
                    using (HttpClient client = new HttpClient())
                    {
                        var stream = client.GetStreamAsync(new Uri(url)).Result;
                        using (StreamReader sr = new StreamReader(stream))
                            return sr.ReadToEnd();
                    }
                }
            });
        }
    }
}
