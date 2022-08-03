using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Net;
using System.Diagnostics;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using CefSharp;
using CefSharp.Wpf;
using HtmlAgilityPack;
using System.Runtime.Serialization.Formatters.Binary;
using System.Globalization;
using System.Drawing;
using System.Threading;
using System.Net.Http;
using System.Xml.Serialization;
using AngleSharp;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace Downloader
{
    public class MySettings
    {
        public string GalleryWebsite { get; set; }
        public string Password { get; set; }
        public string Username { get; set; }
        public string VideoWebsite { get; set; }
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static string GalleryWebsite { get; set; }
        public static string VideoWebsite { get; set; }
        public static string Username { get; set; }
        public static string Password { get; set; }
        public static readonly string Player = @"C:\Program Files\DAUM\PotPlayer\PotPlayerMini64.exe";
        public static readonly string RootFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"AppData\Local\Downloader\temp");
        private string DirectLinksPath { get; set; } = Path.Combine(RootFolder, "DirectLinks.xml");
        private readonly CefSettings settings = new CefSettings();
        public MainWindow()
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "//Cef";
            settings.CachePath = path;
            Cef.Initialize(settings);
            InitializeComponent();
            SettingsDeserializer();
            Browser.FrameLoadEnd += WebBrowserFrameLoadEnded;
            Browser.MenuHandler = new SearchContextMenuHandler();
        }
        private void SettingsDeserializer()
        {
            var serializer = new XmlSerializer(typeof(MySettings));
            using (Stream stream = File.Open("settings.xml", FileMode.Open))
            {
                var settings = serializer.Deserialize(stream) as MySettings;
                GalleryWebsite = settings.GalleryWebsite;
                Password = settings.Password;
                Username = settings.Username;
                VideoWebsite = settings.VideoWebsite;
            }
        }
        private void WebBrowserFrameLoadEnded(object sender, FrameLoadEndEventArgs e)
        {
            Browser.GetSourceAsync().ContinueWith(async taskHtml =>
            {
                Dispatcher.Invoke(() => txtbox42.Text = taskHtml.Result);

            });
        }

        private void GetThreads_Click(object sender, RoutedEventArgs e)
        {
            GetThreads();
        }

        private void GetThreads()
        {
            string html = Dispatcher.Invoke(() => txtbox42.Text);
            var hDoc = new HtmlDocument();
            hDoc.LoadHtml(html);
            var threadtitles = txtbox41.Text.Contains("searchid") ? hDoc.DocumentNode.SelectNodes("//*[@class=\"searchtitle\"]")
                        : hDoc.DocumentNode.SelectNodes("//*[@class=\"threadtitle\"]");
            string output = "";
            for (int i = 0; i < threadtitles.Count; i++)
            {

                try
                {
                    var childs = threadtitles[i].ChildNodes;
                    string prefix = "";
                    foreach (var child in childs)
                    {
                        string gallerylink;
                        if (child.Name == "a" && child.GetAttributeValue("class", "SHIT") == "title\nthreadtitle_unread" || child.GetAttributeValue("class", "SHIT") == "title")
                        {
                             gallerylink = $"http://{GalleryWebsite}/{child.GetAttributeValue("href", "SHIT")}";
                            output += gallerylink+"\n";
                            break;
                        }
                    }
                    if (prefix == "IMPORTANT:")
                    {
                        continue;
                    }
                }
                catch
                {
                    
                    //MessageBox.Show($"page#{c}\n{node.InnerHtml}");
                    //ignore
                }
            }
            Dispatcher.Invoke(() => txtbox42.Text = output);
        }
        private BinaryFormatter Formatter { get; set; } = new BinaryFormatter();
        private readonly string file = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "cookies.dat");


        private ChromeDriver Driver { get; set; }
        private Bitmap Image_property { get; set; }

        private HttpClientHandler handler;
        private HttpClient client;
        private void InitializeHttpClient()
        {
            handler = new HttpClientHandler
            {
                UseCookies = true,
                AllowAutoRedirect = false,
                CookieContainer = CcToFile
            };
            client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:87.0) Gecko/20100101 Firefox/87.0");
        }
        private void GetVideoGroups()
        {
            VideoGroups = XmlDeserialize<List<VideoDirectGroup>>(DirectLinksPath);
            if (VideoGroups == null)
                VideoGroups = new List<VideoDirectGroup>();
        }
        private async Task<string> GetImageDimensions(string imageUrl)
        {
            var get1 = await client.GetStreamAsync(imageUrl);
            List<byte> byteList = new List<byte>();
            byte[] bytes = new byte[200];
            int offset = 0;
            while (true)
            {
                get1.Read(bytes, offset, bytes.Length);
                byteList.AddRange(bytes);
                using (MemoryStream ms = new MemoryStream(byteList.ToArray()))
                    try
                    {
                        Image_property = new Bitmap(ms);
                        break;
                    }
                    catch
                    {
                        if (byteList.Count > 35000)
                            return "0";
                    }
            }
            return Image_property.Width > Image_property.Height ? $"{Image_property.Width}" : $"{Image_property.Height}";
        }

        public static string Link_transform(string thumbLink)
        {
            if (Regex.IsMatch(thumbLink, @"turboim|imagevenue\.com|imagebam\.com"))
                return ComplexLinkTransform(thumbLink).Result;
            else
                return SimpleLinkTransform(thumbLink);
        }
        private static string SimpleLinkTransform(string thumbLink)
        {
            if (thumbLink.Contains("imx.to") && thumbLink.Contains("small"))
            {
                return Regex.Replace(thumbLink, @"\/small", "/big");
            }
            if (thumbLink.Contains("imx.to"))
            {
                return thumbLink.Replace("/t/", "/i/");
            }
            if (Regex.IsMatch(thumbLink, @"imx\.to\/u\/"))
            {
                return Regex.Replace(thumbLink, @"\/t", "/i");
            }
            if (Regex.IsMatch(thumbLink, @"pixroute"))
            {
                return thumbLink.Replace("_t.jpg", ".jpg");
            }
            if (Regex.IsMatch(thumbLink, @"acidimg\.cc"))
            {
                return thumbLink.Replace("small", "big");

            }
            if (Regex.IsMatch(thumbLink, @"imagetwist\.com"))
            {
                return thumbLink.Replace(".com/th/", ".com/i/");
            }
            if (Regex.IsMatch(thumbLink, @"pixhost\.to"))
            {
                return Regex.Replace(thumbLink, @"https:\/\/t", "https://img").Replace("thumbs", "images");

            }
            if (Regex.IsMatch(thumbLink, @"imgbox\.com"))
            {
                if (thumbLink.Contains("images"))
                    return Regex.Replace(thumbLink, @"_t\.", "_o.").Replace("thumbs", "images");
                else
                    return Regex.Replace(thumbLink, @":\/\/t", "://i");

            }
            if (Regex.IsMatch(thumbLink, @"imagezilla\.net"))
            {
                return Regex.Replace(thumbLink, @"_tn\.", ".").Replace("thumbs2", "images");

            }
            if (Regex.IsMatch(thumbLink, @"imgspice\.com"))
                return thumbLink.Replace("_t.", ".");
            return thumbLink;
        }
        private async static Task<string> ComplexLinkTransform(string thumbLink)
        {
            try
            {
                return await Task.Run(() =>
                {
                    if (Regex.IsMatch(thumbLink, @"turboim"))
                    {
                        var m = Regex.Match(thumbLink, @"\/(\d{6,10})_([\S]+\.jpe?g)");
                        string link = $"https://www.turboimagehost.com/p/{m.Groups[1]}/{m.Groups[2]}.html";
                        WebClient wc = new WebClient();
                        string html = wc.DownloadString(link);
                        var hDoc = new HtmlDocument();
                        hDoc.LoadHtml(html);
                        var node = hDoc.DocumentNode.SelectSingleNode("//*[@id=\"uImage\"]");
                        return node.GetAttributeValue("src", "SHIT");

                    }
                    if (Regex.IsMatch(thumbLink, @"imagevenue\.com"))
                    {
                        string link = Regex.Replace(thumbLink, @"loc\d{1,5}\/th_", "img.php?image=");
                        WebClient wc = new WebClient();
                        string html = wc.DownloadString(link);
                        var hDoc = new HtmlDocument();
                        hDoc.LoadHtml(html);
                        var node = hDoc.DocumentNode.SelectSingleNode("//*[@class=\"row\"]//a/img");
                        return node.GetAttributeValue("src", "SHIT");

                    }
                    if (Regex.IsMatch(thumbLink, @"imagebam\.com"))
                    {
                        string link = "http://imagebam.com/image" + thumbLink.Substring(thumbLink.LastIndexOf('/'),
                            thumbLink.LastIndexOf('.') - thumbLink.LastIndexOf('/')) + "/";
                        WebClient wc = new WebClient();
                        string html = wc.DownloadString(link);
                        var hDoc = new HtmlDocument();
                        hDoc.LoadHtml(html);
                        var node = hDoc.DocumentNode.SelectSingleNode("//*[@property=\"og:image\"]");
                        return node.GetAttributeValue("content", "SHIT");

                    }
                    return thumbLink;
                });
            }
            catch
            {
                return thumbLink;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            using (Stream s = File.Create(file))
                Formatter.Serialize(s, CcToFile);

        }

        private async void VideoWebsiteBtn_Click(object sender, RoutedEventArgs e)
        {
            string text = txtBox31.Text;
            var match = Regex.Match(text, $@"^(?:https?:\/\/(?:www.)?)?{VideoWebsite}/video/");
            if (match.Success)
            {
                await GetTRexVideoLinks();
            }
            else if (albums.IsChecked == true && (match = Regex.Match(text, $@"^(?:https?:\/\/(?:www.)?)?{VideoWebsite}\/models\/([\s\S]+?)\/")).Success)
            {
                string baseUrl = match.Value.Replace("/models", "/albums/models");
                string filename = match.Groups[1].Value;
                string filepath = Path.Combine(RootFolder, $"{filename}.csv");
                string filepathxlsx = filepath.Replace(".csv", ".xlsx");
                await GetAlbums(baseUrl, filepath);
                await XlsxConverter.ConvertToXLSX(filepath, filepathxlsx).ContinueWith((t) =>
                {
                    StartFile(filepathxlsx);
                    DeleteFile(filepath);
                });

            }
            else if ((match = Regex.Match(text, $@"^(?:https?:\/\/(?:www.)?)?{VideoWebsite}\/albums\/[\d]+\/[\S]+")).Success)
            {
                await AlbumPage(text);
            }
            else
            {
                if (useChromeDriverVideoWebsite.IsChecked == true)
                    VideoWebsiteWithChromedriver();
                else
                    await VideoWebsiteWithHttpclient();
            }

        }
        private async Task AlbumPage(string url)
        {
            string html = await client.GetStringAsync(url);
            MakeAlbum(html, url);

        }
        private void MakeAlbum(string html, string url)
        {
            string filename = Regex.Replace(url, $@"^(?:https?:\/\/(?:www.)?)?{VideoWebsite}\/(albums)\/([\d]+)\/([^\/\\]+)\/", @"$1 $2 $3");
            HtmlDocument hDoc = new HtmlDocument();
            hDoc.LoadHtml(html);
            List<string> thumbs = new List<string>();
            List<string> images = new List<string>();
            string query = hDoc.DocumentNode.SelectSingleNode("//span[@class=\"message\"]") != null ?
                "//div[@class=\"album-holder\"]//span[@class=\"item private\"]/img" :
                "//div[@class=\"album-holder\"]//a[@class=\"item\"]/div/img";
            var nodes = hDoc.DocumentNode.SelectNodes(query);
            foreach (var node in nodes)
            {
                thumbs.Add(node.GetAttributeValue("data-original", ""));
                images.Add(TransformAlbumLink(node.GetAttributeValue("data-original", "")));
            }
            MakeHtmlAlbum(thumbs, images, filename, url);
        }
        private void MakeHtmlAlbum(List<string> thumbs, List<string> images, string filename, string url)
        {
            try
            {
                string first_line = "<div class=\"demo-gallery\"><ul id = \"lightgallery\" class=\"list-unstyled row\">";
                string last_line = "</ul></div>";
                string _output = "";
                string imageDimensions = GetImageDimensions(images[0]).Result;
                int count = thumbs.Count;
                for (int i = 0; i < count; i++)
                {
                    string a = images[i];
                    string b = thumbs[i];
                    string base_html = $"<li class=\"col-xs-6 col-sm-4 col-md-2 col-lg-2\" data-responsive=\"{a}\"" +
                    $"data-src=\"{a}\"data-sub-html=\"\"><a href=\"\"><img class=\"img-responsive\" " +
                    $"src=\"{b}\"></a></li>";
                    _output += base_html;
                }
                string output = first_line + _output + last_line;
                string testHtml = File.ReadAllText(@"F:\web-d\modal images\template.html");
                string tempHtml = testHtml.Replace("<title></title>", $"<title>{imageDimensions}px {count} {filename}</title>");
                tempHtml = tempHtml.Replace("<h2></h2>", $"<h2><a href=\"{url}\">{filename} {imageDimensions}px {count}</a></h2>{output}");
                string filePath = $@"F:\web-d\modal images\{filename} {imageDimensions}px {count}.html";
                File.WriteAllText(filePath, tempHtml);
                Process.Start(filePath);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
        private string TransformAlbumLink(string input)
        {
            return Regex.Replace(input, @"(https?:\/\/(?:www\.)?(?:[\w]+\.)?(?:[\w]+\.)?(?:[\w]+)?\/)main\/\d+x\d+\/(\d+\/\d+\/\d+\.(?:jpe?g|png))", @"$1sources/$2");
        }
        private async Task<string> HtmlToAlbumsList(string html)
        {
            string output = "", link = "", title = "", views = "", totalphotos = "", dimensions = "";
            try
            {
                Regex pat = new Regex(@"[\d\s]+");
                HtmlDocument hDoc = new HtmlDocument();
                hDoc.LoadHtml(html);
                var items = hDoc.DocumentNode.SelectNodes("//div[@class=\"list-albums\"]/div/div");
                foreach (var item in items)
                {
                    var children = item.ChildNodes;
                    foreach (var child in children)
                    {
                        if (child.Name == "a")
                        {
                            link = child.GetAttributeValue("href", "");
                            title = child.GetAttributeValue("title", "");
                        }
                        else if (child.HasClass("viewsalbums"))
                        {
                            views = pat.Match(child.InnerText).Value.Replace(" ", "");
                        }
                        else if (child.HasClass("totalalbums"))
                        {
                            totalphotos = pat.Match(child.InnerText).Value.Replace(" ", "");
                        }
                    }
                    if (Dispatcher.Invoke(() => tryGetImageSize.IsChecked) == true)
                    {
                        dimensions = await GetImageDimensions(await ImageLink(link));
                    }
                    output += $"{title};{views};{totalphotos};{link};{dimensions}\n";
                }
                return output;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return output;
            }
        }
        private async Task<string> ImageLink(string url)
        {

            string html = await client.GetStringAsync(url);
            HtmlDocument hDoc = new HtmlDocument();
            hDoc.LoadHtml(html);
            string query = hDoc.DocumentNode.SelectSingleNode("//span[@class=\"message\"]") != null ?
            "//div[@class=\"album-holder\"]//span[@class=\"item private\"]/img" :
            "//div[@class=\"album-holder\"]//a[@class=\"item\"]/div/img";
            var nodes = hDoc.DocumentNode.SelectNodes(query);
            return TransformAlbumLink(nodes[0].GetAttributeValue("data-original", ""));


        }

        private void VideoWebsiteWithChromedriver()
        {
            var timenow = DateTime.Now;
            string output = $"{timenow}\nTitle;Addtime;Duration;Views;Rating;Link;Page;Privacy#\n";
            string base_url = txtBox31.Text;
            string[] arguments = { /*"--headless",*/ "--disable-gpu", "--blink-settings=imagesEnabled=false" };
            var chromeOptions = new ChromeOptions
            {
                PageLoadStrategy = PageLoadStrategy.Eager//"ускоряет" загрузку страниц
            };
            //Блокирует загрузку изображений!!! для быстрой загрузки
            chromeOptions.AddArguments(arguments);
            Driver = new ChromeDriver(chromeOptions);
            Driver.Navigate().GoToUrl(new Uri(base_url));
            int index = 0;
            WebDriverWait wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(20));
            System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> elems;
            bool looper = true;
            int i = 0;
            int elemsCount = 0;
            while (true)
            {
                try
                {
                    string html = Driver.PageSource;
                    //output += MakeXML(html, i);//!!!!!
                    if (i != 0 && index != 0 && index == elemsCount - 4)
                        break;
                    while (looper)
                    {
                        try
                        {
                            elems = Driver.FindElements(By.CssSelector("#list_videos_common_videos_list_norm .pagination-holder ul li"));
                            elemsCount = elems.Count;
                            index = elems.Cast<IWebElement>()
                                .Select(x => x.GetAttribute("class")).ToList().IndexOf("page-current");
                            Driver.FindElement(By.CssSelector($"#list_videos_common_videos_list_norm .pagination-holder ul li:nth-child({index + 2})"))
                                .Click();
                            looper = false;
                        }
                        catch
                        {
                            looper = true;
                        }
                    }
                    looper = true;
                    i++;
                }
                catch (Exception exc)
                {
                    MessageBox.Show($"{exc.Message} {index}");
                    break;

                }
            }
            File.WriteAllText($@"C:\users\dmytr\OneDrive\Desktop\{VideoWebsite}{timenow.Ticks}.csv", output);
            Driver.Close();


        }
        private async Task<int> CheckAddress(string url)
        {
            try
            {
                var head = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));
                if (head.StatusCode != HttpStatusCode.OK)
                    return 404;
                else
                {
                    return 200;
                }
            }
            catch
            {
                return 404;
            }
        }
        private async Task GetAlbums(string url, string filepath)
        {
            string output = "Название;Просмотры;Фотографий;Ссылка;Размеры\n";
            try
            {
                string html = await client.GetStringAsync(url);
                string result = await HtmlToAlbumsList(html);
                WriteFile(filepath, output + result);

            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }

        }
        private void WriteFile(string filepath, string content)
        {
            using (FileStream fs = new FileStream(filepath, FileMode.OpenOrCreate))
            {
                using (StreamWriter sw = new StreamWriter(fs, new UTF8Encoding(true)))
                {
                    sw.Write(content);
                }
            }
        }
        private async Task VideoWebsiteWithHttpclient(string link = null)
        {
            try
            {
                var timenow = DateTime.Now.ToLocalTime();
                string base_url = "";
                string type = "";
                bool query = false;
                string goToLink = link ?? txtBox31.Text;
                Match match = Regex.Match(goToLink, $@"https?:\/\/(?:www\.)?{VideoWebsite}\/(categories|search|models)\/(\S+?)\/");
                if (match.Success)
                {
                    base_url = match.Value;
                    type = match.Groups[1].Value;
                }
                else if ((match = Regex.Match(goToLink, $@"https?:\/\/(?:www\.)?{VideoWebsite}\/(members)\/(\d+)(?:\/videos\/)?")).Success)
                {
                    string baseLink = $"https://www.{VideoWebsite}/members/{match.Groups[2].Value}/";
                    await GetMembersVideos($@"https://www.{VideoWebsite}/members/{match.Groups[2].Value}/videos/?");
                    UpdateMemberInfo(baseLink);
                    return;
                }
                else if ((match = Regex.Match(goToLink, @"(\w+)( \w+){0,}")).Success)
                {
                    query = true;
                    base_url = $@"https://www.{VideoWebsite}/models/{goToLink.Trim().Replace(" ", "-")}/";
                    if (await CheckAddress(base_url) == 404)
                    {
                        base_url = $@"https://www.{VideoWebsite}/search/{goToLink.Trim().Replace(" ", "-")}/";
                        await GetVideos(base_url);
                        return;
                    }

                }
                else
                {
                    txtBox31.Text = "Неправильная ссылка";
                    return;
                }
                string filename = match.Success && !query ? match.Groups[1].Value + " " + match.Groups[2].Value : Regex.Replace(base_url, $@"https?:\/\/(?:www\.)?{VideoWebsite}\/(search|models)\/(\S+?)\/", @"$1 $2");
                string filepath = $@"C:\users\dmytr\OneDrive\Desktop\{filename}{timenow.Ticks}.csv";
                List<TrexVideo> trexVideos = XmlDeserialize<TrexVideos>(filepath)?.TrexVideosList ?? new List<TrexVideo>();
                int i = 0;
                //if (File.Exists(filepath))
                //    File.Delete(filepath);
                await Task.Run(() =>
                {
                    while (true)
                    {
                        try
                        {
                            i++;
                            string changing_part;
                            if (base_url.Contains("/members/"))
                            {
                                changing_part = $"?mode=async&function=get_block&block_id=list_videos_uploaded_videos&is_private=0,1&sort_by=&from_uploaded_videos={i}&_={(long)(timenow - new DateTime(1970, 1, 1)).TotalMilliseconds}";
                            }
                            else
                            {
                                changing_part =
                            type != "search" ?
                            $@"?mode=async&function=get_block&block_id=list_videos_common_videos_list_norm&sort_by=post_date&from4={i}&_={(long)(timenow - new DateTime(1970, 1, 1)).TotalMilliseconds}" :
                            $"{i}/";
                            }
                            if (type == "search" && i > 1)
                                break;
                            string url = base_url + changing_part;
                            string html = client.GetStringAsync(url).Result;
                            var list = MakeXML(html, trexVideos);
                            if (list == null)
                                break;
                            trexVideos.AddRange(list);

                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(e.Message);
                            break;
                        }
                    }
                    XmlSerialize<TrexVideos>(new TrexVideos(trexVideos), filepath);
                    Dispatcher.Invoke(() => TrexVideoDataGrid.ItemsSource = trexVideos);
                    AdjustColumnWidth(trexVideos[0]);
                    ToFilter = trexVideos.OrderByDescending(x => x.Id).ToList();
                    string filepathxlsx = filepath.Replace(".csv", ".xlsx");
                    XlsxConverter.ConvertToXLSX(filepath, filepathxlsx).ContinueWith(t =>
                    {
                        StartFile(filepathxlsx);
                        DeleteFile(filepath);
                    });

                });
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
        enum RequestSource
        {
            FromTextBox,
            FromListBox,
            Default
        }
        private string MakeFilePath(string baseLink)
        {
            var match = Regex.Match(baseLink, @"https?:\/\/[^\/]+\/([^\/]+)\/([^\/]+)\/");
            string fileName = $"{match.Groups[1]}_{match.Groups[2]}";
            string filepath = $"{fileName}.xml";
            return filepath;
        }
        private async Task GetVideos(string baseLink, RequestSource requestSource = RequestSource.Default)
        {
            Dispatcher.Invoke(() => ProcessInfo.Text = "Начинаем");
            var timeNow = DateTime.Now;
            int i = 0;
            string changingPart;
            Debug.WriteLine($"{i} _0");
            string filepath = MakeFilePath(baseLink);
            Debug.WriteLine($"{i} _1");
            List<TrexVideo> trexVideos = XmlDeserialize<TrexVideos>(Path.Combine(RootFolder, filepath))?.TrexVideosList ?? new List<TrexVideo>();
            while (true)
            {
                i++;
                Dispatcher.Invoke(() => ProcessInfo.Text = i.ToString());
                if (requestSource == RequestSource.FromListBox)
                    changingPart = $"?mode=async&function=get_block&block_id=list_videos_common_videos_list_norm&sort_by=post_date&from4={i}&_={(long)(timeNow - new DateTime(1970, 01, 01)).TotalMilliseconds - 10800000}";
                else if (requestSource == RequestSource.FromTextBox)
                    changingPart = $"?mode=async&function=get_block&block_id=list_videos_uploaded_videos&is_private=0%2C1&sort_by=&from_uploaded_videos={i}&_={(long)(timeNow - new DateTime(1970, 01, 01)).TotalMilliseconds - 10800000}";
                else
                {
                    if (i > 30)
                        break;
                    changingPart = $"{i}/";
                }
                if (ReturnStatusCode(baseLink + changingPart) == HttpStatusCode.NotFound)
                    break;
                var html = await client.GetStringAsync(baseLink + changingPart);
                var list = MakeXML(html, trexVideos);
                if (list == null)
                    break;
                trexVideos.AddRange(list);
                Debug.WriteLine($"{i} _2");
            }
            Debug.WriteLine($"{i} _3");
            XmlSerialize<TrexVideos>(new TrexVideos(trexVideos), Path.Combine(RootFolder, filepath));
            Dispatcher.Invoke(() => TrexVideoDataGrid.ItemsSource = trexVideos.OrderByDescending(x => x.Id));
            Debug.WriteLine($"{i} _4");
            AdjustColumnWidth(trexVideos[0]);
            Debug.WriteLine($"{i} _5");
            ToFilter = trexVideos.OrderByDescending(x => x.Id).ToList();
            Dispatcher.Invoke(() => ProcessInfo.Text = "Конец! Файл создан!");
        }
        private async Task GetMembersVideos(string baseLink)
        {
            await GetVideos(baseLink, RequestSource.FromTextBox);
        }
        private async Task GetVideosFromContextMenu()
        {
            if (PTDataGrid.SelectedItem == null)
                return;
            if (PTDataGrid.SelectedItem is Model)
            {
                string baseLink = (PTDataGrid.SelectedItem as Model).Link;
                await GetVideos(baseLink, RequestSource.FromListBox);
                UpdateModelInfo(baseLink);
            }
            else if (PTDataGrid.SelectedItem is Category)
            {
                string baseLink = (PTDataGrid.SelectedItem as Category).Link;
                await GetVideos(baseLink, RequestSource.FromListBox);
                UpdateCategoryInfo(baseLink);
            }
            else if (PTDataGrid.SelectedItem is Member)
            {
                string baseLink = (PTDataGrid.SelectedItem as Member).Link;
                await GetVideos(baseLink, RequestSource.FromListBox);
                UpdateMemberInfo(baseLink);
            }
        }
        private async Task UpdateModelInfo(string link)
        {
            var models = XmlDeserialize<Models>(Path.Combine(RootFolder, "Models.xml"));
            var toChange = models.Elements.Where(x => x.Link == link).FirstOrDefault();
            models.Elements.Remove(toChange);
            string filepath = MakeFilePath(link);
            toChange.NumberOfVideos = XmlDeserialize<TrexVideos>(Path.Combine(RootFolder, filepath)).TrexVideosList.Count;
            toChange.IsLocal = true;
            toChange.LocalPath = MakeFilePath(link);
            models.Elements.Add(toChange);
            ToFilterPTDataGrid = models.Elements.Cast<ITitle>().ToList();
            PTDataGrid.ItemsSource = models.Elements;
            XmlSerialize(models, Path.Combine(RootFolder, "Models.xml"));
        }
        private async Task UpdateCategoryInfo(string link)
        {
            var cats = XmlDeserialize<Categories>(Path.Combine(RootFolder, "Categories.xml"));
            var toChange = cats.Elements.Where(x => x.Link == link).FirstOrDefault();
            cats.Elements.Remove(toChange);
            string filepath = MakeFilePath(link);
            toChange.NumberOfVideos = XmlDeserialize<TrexVideos>(Path.Combine(RootFolder, filepath)).TrexVideosList.Count;
            toChange.IsLocal = true;
            toChange.LocalPath = MakeFilePath(link);
            cats.Elements.Add(toChange);
            ToFilterPTDataGrid = cats.Elements.Cast<ITitle>().ToList();
            PTDataGrid.ItemsSource = cats.Elements;
            XmlSerialize(cats, Path.Combine(RootFolder, "Categories.xml"));
        }
        private async Task UpdateMemberInfo(string link)
        {
            HtmlDocument hDoc = null;
            var members = XmlDeserialize<Members>(Path.Combine(RootFolder, "Members.xml"));
            if (members == null)
                members = new Members();
            var toChange = members.Elements.Where(x => x.Link == link).FirstOrDefault();
            if (toChange != null)
            {
                members.Elements.Remove(toChange);
            }
            else
            {
                toChange = new Member();
                toChange.Link = link;
                var html = await client.GetStringAsync(link);
                hDoc = new HtmlDocument();
                hDoc.LoadHtml(html);
                var node = hDoc.DocumentNode.SelectSingleNode("//div[@class=\"user-name\"]/div");
                toChange.Title = node.InnerText.Trim();
            }
            if (hDoc == null)
            {
                var html = await client.GetStringAsync(link);
                hDoc = new HtmlDocument();
                hDoc.LoadHtml(html);
            }
            var subsNode = hDoc.DocumentNode.SelectSingleNode("//div[@class=\"user-info-section user-info-img\"]/span/span");
            if (subsNode != null)
                if (int.TryParse(subsNode.InnerText.Replace(" ", ""), out int subs))
                    toChange.Subs = subs;
                else
                    toChange.Subs = 0;
            else
                toChange.Subs = 0;
            var vidNode = hDoc.DocumentNode.SelectSingleNode("//div[@id=\"list_videos_uploaded_videos\"]/div/h2");
            if (vidNode != null)
            {
                var m = Regex.Match(vidNode.InnerText, @"\((\d+)\)");
                if (m.Success)
                    if (int.TryParse(m.Groups[1].Value, out int vids))
                        toChange.NumberOfVideos = vids;
                    else
                        toChange.NumberOfVideos = 0;
                else
                    toChange.NumberOfVideos = 0;
            }
            toChange.IsLocal = true;
            toChange.LocalPath = MakeFilePath(link);
            members.Elements.Add(toChange);
            ToFilterPTDataGrid = members.Elements.Cast<ITitle>().ToList();
            PTDataGrid.ItemsSource = members.Elements;
            XmlSerialize(members, Path.Combine(RootFolder, "Members.xml"));
        }
        private void DeleteFile(string filepath)
        {
            while (true)
            {
                try
                {
                    File.Delete(filepath);
                    break;
                }
                catch
                {
                    MessageBox.Show("Файл занят");
                }
            }
        }
        private void StartFile(string filepath)
        {
            while (true)
            {
                try
                {
                    Process.Start(filepath);
                    break;
                }
                catch
                {
                    MessageBox.Show("Файл занят");
                }
            }
        }

        private List<TrexVideo> MakeXML(string html, List<TrexVideo> VideosIhave)
        {
            List<TrexVideo> trexVideos = VideosIhave ?? new List<TrexVideo>();
            List<TrexVideo> newTrexVideos = new List<TrexVideo>();
            List<string> links = trexVideos.Select(x => x.Link).ToList();
            int count = 0;
            int total = 0;
            var hDoc = new HtmlDocument();
            hDoc.LoadHtml(html);
            var privatenodes = hDoc.DocumentNode.SelectNodes(".//*[@class=\"video-preview-screen video-item thumb-item private \"]");
            var nonprivatenodes = hDoc.DocumentNode.SelectNodes(".//*[@class=\"video-preview-screen video-item thumb-item  \"]");
            if (privatenodes != null && privatenodes.Count != 0)
            {
                total += privatenodes.Count;
                var durationnodes = hDoc.DocumentNode.SelectNodes("//*[@class=\"video-preview-screen video-item thumb-item private \"]/*[@class=\"durations\"]");
                var viewsnodes = hDoc.DocumentNode.SelectNodes("//*[@class=\"video-preview-screen video-item thumb-item private \"]/*[@class=\"viewsthumb\"]");
                var infnodes = hDoc.DocumentNode.SelectNodes("//*[@class=\"video-preview-screen video-item thumb-item private \"]/*[@class=\"inf\"]/a");
                var addtimenodes = hDoc.DocumentNode.SelectNodes("//*[@class=\"video-preview-screen video-item thumb-item private \"]/*[@class=\"list-unstyled\"]/li[1]");
                var ratingnodes = hDoc.DocumentNode.SelectNodes("//*[@class=\"video-preview-screen video-item thumb-item private \"]/*[@class=\"list-unstyled\"]/*[@class=\"pull-right\"]");
                var qualitynodes = hDoc.DocumentNode.SelectNodes("//*[@class=\"video-preview-screen video-item thumb-item private \"]//*[@class=\"quality\"]");

                var duration = durationnodes.Cast<HtmlNode>().Select(x => x.InnerText.Trim().Replace(";", "")).Select(x => MinutesDouble(x)).ToList();
                var views = viewsnodes.Cast<HtmlNode>().Select(x => Regex.Replace(x.InnerText.Trim(), @"( views?|\s?)", "").Replace(";", "")).Select(x => Convert.ToInt32(x)).ToList();
                var title = infnodes.Cast<HtmlNode>().Select(x => x.InnerText.Trim().Replace(";", "")).ToList();
                var href = infnodes.Cast<HtmlNode>().Select(x => x.GetAttributeValue("href", "SHIT").Replace(";", "")).ToList();
                var addtime = addtimenodes.Cast<HtmlNode>().Select(x => x.InnerText.Trim().Replace(";", "")).ToList();
                var rating = ratingnodes.Cast<HtmlNode>().Select(x => Regex.Match(x.InnerText.Trim().Replace(";", ""), @"\d{1,3}")).Select(x => Convert.ToInt32(x.Value)).ToList();
                var quality = qualitynodes.Cast<HtmlNode>().Select(x => Regex.Match(x.InnerText.Trim().Replace(";", ""), @"\d{3,4}")).ToList();
                List<int> ids = href.Select(x => Convert.ToInt32(Regex.Match(x, @"video\/(\d+)").Groups[1].Value)).ToList();
                for (int i = 0; i < privatenodes.Count; i++)
                {
                    if (links.Contains(href[i]))
                    {
                        count++;
                        continue;
                    }
                    TrexVideo trexVideo = new TrexVideo
                    {
                        Duration = duration[i],
                        Views = views[i],
                        Title = title[i],
                        Link = href[i],
                        AddTime = addtime[i],
                        Rating = rating[i],
                        Quality = Convert.ToInt32(quality[i].Value),
                        Id = ids[i],
                        Type = "private"
                    };
                    newTrexVideos.Add(trexVideo);
                }
            }
            if (nonprivatenodes != null && nonprivatenodes.Count != 0)
            {
                total += nonprivatenodes.Count;
                var durationnodes = hDoc.DocumentNode.SelectNodes("//*[@class=\"video-preview-screen video-item thumb-item  \"]/*[@class=\"durations\"]");
                var viewsnodes = hDoc.DocumentNode.SelectNodes("//*[@class=\"video-preview-screen video-item thumb-item  \"]/*[@class=\"viewsthumb\"]");
                var infnodes = hDoc.DocumentNode.SelectNodes("//*[@class=\"video-preview-screen video-item thumb-item  \"]/*[@class=\"inf\"]/a");
                var addtimenodes = hDoc.DocumentNode.SelectNodes("//*[@class=\"video-preview-screen video-item thumb-item  \"]/*[@class=\"list-unstyled\"]/li[1]");
                var ratingnodes = hDoc.DocumentNode.SelectNodes("//*[@class=\"video-preview-screen video-item thumb-item  \"]/*[@class=\"list-unstyled\"]/*[@class=\"pull-right\"]");
                var qualitynodes = hDoc.DocumentNode.SelectNodes("//*[@class=\"video-preview-screen video-item thumb-item  \"]//*[@class=\"quality\"]");

                var duration = durationnodes.Cast<HtmlNode>().Select(x => x.InnerText.Trim().Replace(";", "")).Select(x => MinutesDouble(x)).ToList();
                var views = viewsnodes.Cast<HtmlNode>().Select(x => Regex.Replace(x.InnerText.Trim(), @"( views?|\s?)", "").Replace(";", "")).Select(x => Convert.ToInt32(x)).ToList();
                var title = infnodes.Cast<HtmlNode>().Select(x => x.InnerText.Trim().Replace(";", "")).ToList();
                var href = infnodes.Cast<HtmlNode>().Select(x => x.GetAttributeValue("href", "SHIT").Replace(";", "")).ToList();
                var addtime = addtimenodes.Cast<HtmlNode>().Select(x => x.InnerText.Trim().Replace(";", "")).ToList();
                var rating = ratingnodes.Cast<HtmlNode>().Select(x => Regex.Match(x.InnerText.Trim().Replace(";", ""), @"\d{1,3}")).Select(x => Convert.ToInt32(x.Value)).ToList();
                var quality = qualitynodes.Cast<HtmlNode>().Select(x => Regex.Match(x.InnerText.Trim().Replace(";", ""), @"\d{3,4}")).ToList();
                List<int> ids = href.Select(x => Convert.ToInt32(Regex.Match(x, @"video\/(\d+)").Groups[1].Value)).ToList();
                for (int i = 0; i < nonprivatenodes.Count; i++)
                {
                    if (links.Contains(href[i]))
                    {
                        count++;
                        continue;
                    }
                    TrexVideo trexVideo = new TrexVideo
                    {
                        Duration = duration[i],
                        Views = views[i],
                        Title = title[i],
                        Link = href[i],
                        AddTime = addtime[i],
                        Rating = rating[i],
                        Quality = Convert.ToInt32(quality[i].Value),
                        Id = ids[i],
                        Type = "public"
                    };
                    newTrexVideos.Add(trexVideo);
                }
            }
            if (count == total)
                return null;
            return newTrexVideos;
        }
        private double MinutesDouble(string duration)
        {
            string[] min_sec = duration.Split(':');
            return Convert.ToDouble(min_sec[0]) + (Convert.ToDouble(min_sec[1]) / 60);

        }
        private async Task GetTRexVideoLinks(string link = null)
        {
            try
            {
                string goToLink = link ?? txtBox31.Text;
                string html = await client.GetStringAsync(goToLink);
                Regex pattern = new Regex($@"https?:\/\/(www\.)?{VideoWebsite}\/get_file\/\d{{1,2}}\/[\da-f]{{42}}\/\d{{1,7}}\/\d{{1,7}}\/\d{{1,7}}(_\d{{3,4}}p)?\.mp4");
                var matches = pattern.Matches(html);
                string notToRepeat = "";
                List<long> fileSize = new List<long>();
                List<string> fileDefinition = new List<string>();
                List<PTrexVideoDirect> pTrexVideos = new List<PTrexVideoDirect>();
                foreach (var m in matches)
                {
                    string mstring = m.ToString();
                    if (!notToRepeat.Contains(mstring))
                    {
                        PTrexVideoDirect pTrexVideo = new PTrexVideoDirect();
                        notToRepeat += mstring;
                        switch (Regex.Match(m.ToString(), @"_?(\d{3,4}p)?\.mp4").Groups[0].Value)
                        {
                            case "_360p.mp4":
                                pTrexVideo.Quality = "360p";
                                goto default;
                            case "_720p.mp4":
                                pTrexVideo.Quality = "720p";
                                goto default;
                            case "_1080p.mp4":
                                pTrexVideo.Quality = "1080p";
                                goto default;
                            case "_1440p.mp4":
                                pTrexVideo.Quality = "1440p";
                                goto default;
                            case "_2160p.mp4":
                                pTrexVideo.Quality = "2160p";
                                goto default;
                            case ".mp4":
                                pTrexVideo.Quality = "480p";
                                goto default;
                            default:
                                pTrexVideo.Link = mstring;
                                pTrexVideo.Size = await NormalSizeFormat(mstring);
                                pTrexVideo.Title = Regex.Match(html, @"<p class=""title-video"">([\s\S]+?)<\/p>").Groups[1].Value;
                                var config = Configuration.Default;
                                var context = BrowsingContext.New(config);
                                var document = await context.OpenAsync(req => req.Content(html));
                                var item = document.DocumentElement.QuerySelector("div.username a");
                                pTrexVideo.UserLink = item.GetAttribute("href");
                                break;
                        }
                        pTrexVideos.Add(pTrexVideo);
                    }
                }
                TrexVideoLinks.ItemsSource = pTrexVideos;
                VideoDirectGroup group = new VideoDirectGroup();
                group.Videos = pTrexVideos;
                group.Link = goToLink;
                VideoGroups.Add(group);
                XmlSerialize(VideoGroups, DirectLinksPath);
                if (autoPlay.IsChecked == true)
                {
                    var item = pTrexVideos.FirstOrDefault(x => x.Quality == Dispatcher.Invoke(() => qList.Text)) ?? pTrexVideos[pTrexVideos.Count - 1];

                    Process.Start(Player, item.Link);
                    Clipboard.SetDataObject($"{item.Title};{item.Link}");
                }
                //_toFilterPTDataGrid = pTrexVideos.Select(x => x as ITitle).ToList();
                TrexVideoLinks.Columns[0].Width = DataGridLength.SizeToHeader;
                TrexVideoLinks.Columns[3].Width = 150;
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }

        }
        public List<VideoDirectGroup> VideoGroups { get; set; } = new List<VideoDirectGroup>();
        private List<ITitle> ToFilterPTDataGrid { get; set; }
        private async Task LoadCategories()
        {
            var config = Configuration.Default;
            var context = BrowsingContext.New(config);
            //Categories cats = new Categories();
            Categories cats = XmlDeserialize<Categories>(Path.Combine(RootFolder, "Categories.xml")) ?? new Categories();
            var html = await client.GetStringAsync($"https://www.{VideoWebsite}/categories/");
            var document = await context.OpenAsync(req => req.Content(html));
            var collection = document.DocumentElement.QuerySelectorAll("div.box a.item");
            List<string> entriesLinks = cats.Elements.Select(x => x.Link).ToList();
            foreach (var item in collection)
            {
                if (!entriesLinks.Contains(item.GetAttribute("href")))
                    cats.Elements.Add(new Category()
                    {
                        Title = item.GetAttribute("title"),
                        Link = item.GetAttribute("href"),
                        NumberOfVideos = Convert.ToInt32(Regex.Match(item.QuerySelector(".wrap").TextContent, @"\d+").Value),
                        Rating = Convert.ToInt32(Regex.Match(item.QuerySelector(".rating").TextContent, @"\d+").Value),
                    });

            }
            cats.LastUpdate = DateTime.Now.ToString();
            XmlSerialize(cats, Path.Combine(RootFolder, "Categories.xml"));
        }
        private async Task LoadModels()
        {
            var config = Configuration.Default;
            var context = BrowsingContext.New(config);
            //Models models = new Models();
            Models models = XmlDeserialize<Models>(Path.Combine(RootFolder, "Models.xml")) ?? new Models();
            var timeNow = DateTime.Now;
            int i = 0;
            while (true)
            {
                i++;
                string changingPart = $"?mode=async&function=get_block&block_id=list_models_models_list&section=&sort_by=avg_videos_rating&from={i}&_={(long)(timeNow - new DateTime(1970, 01, 01)).TotalMilliseconds}";
                if (ReturnStatusCode($"https://www.{VideoWebsite}/models/{changingPart}") == HttpStatusCode.NotFound)
                    break;
                var html = await client.GetStringAsync($"https://www.{VideoWebsite}/models/{changingPart}");
                var document = await context.OpenAsync(req => req.Content(html));
                var collection = document.DocumentElement.QuerySelectorAll("div.box a.item");
                List<string> entriesLinks = models.Elements.Select(x => x.Link).ToList();
                foreach (var item in collection)
                {
                    if (!entriesLinks.Contains(item.GetAttribute("href")))
                        models.Elements.Add(new Model()
                        {
                            Title = item.GetAttribute("title"),
                            Link = item.GetAttribute("href"),
                            NumberOfVideos = Convert.ToInt32(Regex.Match(item.QuerySelector(".wrap").TextContent, @"\d+").Value),
                            Rating = Convert.ToInt32(Regex.Match(item.QuerySelector(".rating").TextContent, @"\d+").Value),
                        });
                }
            }
            models.LastUpdate = DateTime.Now.ToString();
            XmlSerialize(models, Path.Combine(RootFolder, "Models.xml"));
        }
        private async void GetCatsAndModels_Click(object sender, RoutedEventArgs e)
        {
            await LoadCategories();
            await LoadModels();
        }
        private HttpStatusCode ReturnStatusCode(string url)
        {
            var req = client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url)).Result;
            return req.StatusCode;
        }
        private void Click_ToPlay(object sender, RoutedEventArgs e)
        {
            if (TrexVideoLinks.SelectedItem == null)
                return;
            var item = TrexVideoLinks.SelectedItem as PTrexVideoDirect;
            Process.Start(Player, item.Link);
            Clipboard.SetDataObject($"{item.Title};{item.Link}");
        }
        private void Click_ToCopy(object sender, RoutedEventArgs e)
        {
            if (TrexVideoLinks.SelectedItem == null)
                return;
            var item = TrexVideoLinks.SelectedItem as PTrexVideoDirect;
            Clipboard.SetDataObject($"{item.Title};{item.Link}");
        }
        private async void Click_ToSearch(object sender, RoutedEventArgs e)
        {
            if (TrexVideoDataGrid.SelectedItem == null)
                return;
            if (!(TrexVideoDataGrid.SelectedItem is TrexVideo))
                return;
            var item = TrexVideoDataGrid.SelectedItem as TrexVideo;
            await GetTRexVideoLinks(item.Link);
        }
        private void Click_ChangeData(object sender, RoutedEventArgs e)
        {
            if (PTDataGrid.SelectedItem == null)
                return;
            if (PTDataGrid.SelectedItem is Model)
            {
                if (!File.Exists(Path.Combine(RootFolder, "Categories.xml")))
                {
                    MessageBox.Show("Не существует");
                    //ToFilterPTDataGrid = null;
                    //PTDataGrid.ItemsSource = null;
                    //ChangeDataMenuItem.Header = "To Members";
                    return;
                }
                var deserialized = XmlDeserialize<Categories>(Path.Combine(RootFolder, "Categories.xml")).Elements;
                PTDataGrid.ItemsSource = deserialized;
                ToFilterPTDataGrid = deserialized.Select(x => x as ITitle).ToList();
                ChangeDataMenuItem.Header = "To Members";
            }
            else if (PTDataGrid.SelectedItem is Category)
            {
                if (!File.Exists(Path.Combine(RootFolder, "Members.xml")))
                {
                    MessageBox.Show("Не существует");
                    //ToFilterPTDataGrid = null;
                    //PTDataGrid.ItemsSource = null;
                    //ChangeDataMenuItem.Header = "To Members";
                    return;
                }
                var deserialized = XmlDeserialize<Members>(Path.Combine(RootFolder, "Members.xml"))?.Elements;
                if (deserialized == null)
                {
                    deserialized = new List<Member>();
                }
                PTDataGrid.ItemsSource = deserialized;
                ToFilterPTDataGrid = deserialized.Select(x => x as ITitle).ToList();
                ChangeDataMenuItem.Header = "To Models";
            }
            else
            {
                if (!File.Exists(Path.Combine(RootFolder, "Models.xml")))
                {
                    MessageBox.Show("Не существует");
                    //ToFilterPTDataGrid = null;
                    //PTDataGrid.ItemsSource = null;
                    //ChangeDataMenuItem.Header = "To Members";
                    return;
                }
                var deserialized = XmlDeserialize<Models>(Path.Combine(RootFolder, "Models.xml")).Elements;
                PTDataGrid.ItemsSource = deserialized;
                ToFilterPTDataGrid = deserialized.Select(x => x as ITitle).ToList();
                ChangeDataMenuItem.Header = "To Categories";
            }
        }
        private async void Click_GetVideosInfo(object sender, RoutedEventArgs e)
        {
            if (PTDataGrid.SelectedItem == null)
                return;
            if (PTDataGrid.SelectedItem is ITitle)
            {
                await GetVideosFromContextMenu();
            }
        }
        private async Task<long?> GetFileSize(string url)
        {
            var head = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));
            return head.Content.Headers.ContentLength;
        }
        private async Task<string> NormalSizeFormat(string url)
        {
            long? result = await GetFileSize(url);
            if (result == null)
                return "NaN";
            if (result >= 1073741824)
            {
                return $"{Convert.ToDouble(result / 1024 / 1024 / 1024):F1} GB";
            }
            if (result >= 1048576)
            {
                return $"{Convert.ToDouble(result / 1024 / 1024):F1} MB";
            }
            if (result >= 1024)
            {
                return $"{Convert.ToDouble(result / 1024):F1} KB";
            }
            else
            {
                return $"{result} B";
            }
        }
        private async Task PTLoginClickAsync()
        {
            await client.GetAsync($"http://{VideoWebsite}/");
            FormUrlEncodedContent content = new FormUrlEncodedContent(
                new Dictionary<string, string>() {
                    {"username",Username },
                    { "pass",Password},
                    {"action","login" },
                    {"email_link",$"https%3A%2F%2Fwww.{VideoWebsite}%2Femail%2F" },
                    {"remember_me","1" },
                    {"format","json" },
                    {"mode","async" }
                });
            HttpResponseMessage post = await client.PostAsync($"https://www.{VideoWebsite}/ajax-login/", content);
            if (post.Headers.TryGetValues("set-cookie", out IEnumerable<string> cookies))
            {
                foreach (var cookie in cookies)
                {
                    CcToFile.SetCookies(post.RequestMessage.RequestUri, cookie);
                }
            }
        }
        private async void VideoWebsiteLogin_Click(object sender, RoutedEventArgs e)
        {
            await PTLoginClickAsync();
        }

        private CookieContainer CcToFile { get; set; } = new CookieContainer();
        private async Task LoadPrefix()
        {
            var html = await client.GetStringAsync($"https://{GalleryWebsite}/search.php?search_type=1");
            HtmlDocument hDoc = new HtmlDocument();
            hDoc.LoadHtml(html);
            var nodes = hDoc.DocumentNode.SelectNodes("//*[@label=\"Adult Photo Sets\"]//*");
            var values = nodes.Select(x => x.GetAttributeValue("value", "SHIT")).ToList();
            values.AddRange(new[] { "sv_1080p", "sv_2160p", "sv_720p" });
            values.Insert(0, "");
            File.WriteAllText(@"PrefixId.csv", string.Join("\n", values));


        }
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            File.Delete(Path.Combine(RootFolder, "FileInfo.txt"));
            if (File.Exists(file))
            {
                try
                {
                    using (Stream f = File.OpenRead(file))
                    {
                        CcToFile = (CookieContainer)Formatter.Deserialize(f);
                    }
                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.Message);

                }
            }
            InitializeHttpClient();
            GetVideoGroups();
            ////
            if (!File.Exists("start.config"))
            {
                using (FileStream fs = new FileStream(@"start.config", FileMode.Create))
                {
                    ;
                }
            }
            string updateDate = File.ReadAllText("start.config");
            if (string.IsNullOrEmpty(updateDate))
            {
                await LoadPrefix().ContinueWith(t => File.WriteAllText("start.config", DateTime.Now.ToString()));
            }
            else
            {
                if ((DateTime.Now - DateTime.Parse(updateDate)) > new TimeSpan(3, 0, 0, 0))
                {
                    await LoadPrefix().ContinueWith(t => File.WriteAllText("start.config", DateTime.Now.ToString()));
                }
            }
            ///
            ForListBox = File.ReadAllText("PrefixId.csv").Replace("\r", "").Split('\n');
            listbox43.ItemsSource = ForListBox;
            LoadFavorites();
            List<string> sites = new List<string>() { "vk.com", "m.vk.com", "instagram.com" };
            History.ItemsSource = sites;
            listbox43.SelectAll();
            List<string> fromlistbox = listbox43.SelectedItems.Cast<string>().ToList();
            PrefixChoice = MakePrefix(fromlistbox);
            if (!File.Exists(Path.Combine(RootFolder, "Categories.xml")))
            {
                MessageBox.Show("Не существует");
                ToFilterPTDataGrid = null;
                ChangeDataMenuItem.Header = "To Members";
                PTDataGrid.ItemsSource = null;
                return;
            }
            var deserialized = XmlDeserialize<Categories>(Path.Combine(RootFolder, "Categories.xml")).Elements;
            ToFilterPTDataGrid = deserialized.Select(x => x as ITitle).ToList();
            PTDataGrid.ItemsSource = deserialized.OrderByDescending(x => x.IsLocal).ThenBy(x => x.Title).ToList();//TODO реализовать сортировку
        }
        string[] ForListBox { get; set; }
        private void LoadFavorites()
        {
            if (File.Exists("PrefixFavorites.csv"))
            {
                string[] forfavorites = File.ReadAllLines("PrefixFavorites.csv");
                listbox44.ItemsSource = forfavorites;
            }
        }


        private async void VipersInfo_Click(object sender, RoutedEventArgs e)
        {
            bool tocontinue = true;
            int c = 0;
            string output = "Prefix;Dimensions;Full Name;Celebrities;Date;Quantity;Gallery Name;Gallery Link\n";
            while (tocontinue)
            {
                c++;
                string pageinfo = "";
                Uri uri;
                if (txtbox41.Text.Contains($"{GalleryWebsite}/forums/"))
                    if (txtbox41.Text.Contains("?"))
                    {
                        uri = new Uri($"{txtbox41.Text}".Replace("?", $"/page{c}?"));
                    }
                    else
                    {
                        uri = new Uri($"{txtbox41.Text}/page{c}");
                    }
                else
                    uri = new Uri($"{txtbox41.Text}&pp=&page={c}");
                try
                {
                    string html = await client.GetStringAsync(uri);
                    var hDoc = new HtmlDocument();
                    hDoc.LoadHtml(html);
                    var paginationNode = txtbox41.Text.Contains("searchid") ? hDoc.DocumentNode.SelectSingleNode("//*[@id=\"pagination_top\"]//*[@class=\"popupctrl\"]")
                    : hDoc.DocumentNode.SelectSingleNode("//*[@class=\"threadpagenav\"]//*[@class=\"popupctrl\"]");
                    var threadtitles = txtbox41.Text.Contains("searchid") ? hDoc.DocumentNode.SelectNodes("//*[@class=\"searchtitle\"]")
                        : hDoc.DocumentNode.SelectNodes("//*[@class=\"threadtitle\"]");
                    if (threadtitles == null)
                        break;
                    if (paginationNode != null)
                    {
                        string str = paginationNode.InnerText;
                        MatchCollection matches = Regex.Matches(str, @"\d+");
                        if (matches[0].Value == matches[1].Value)
                            tocontinue = false;
                    }
                    else
                    {
                        tocontinue = false;
                    }
                    string galleryName = "", gallerylink = "", imageDimensions = "";
                    foreach (var node in threadtitles)
                    {

                        if (node.Name != "h3")
                            continue;
                        Stopwatch stw = new Stopwatch();
                        stw.Start();
                        try
                        {
                            var childs = node.ChildNodes;
                            string prefix = "";
                            foreach (var child in childs)
                            {
                                if (child.GetAttributeValue("class", "SHIT") == "prefix understate")
                                {
                                    prefix = child.InnerText.Trim();
                                }
                                if (child.Name == "a" && child.GetAttributeValue("class", "SHIT") == "title threadtitle_unread" || child.GetAttributeValue("class", "SHIT") == "title")
                                {
                                    gallerylink = $"http://{GalleryWebsite}/{child.GetAttributeValue("href", "SHIT")}";
                                    galleryName = child.InnerText.Trim().Replace(";", "");
                                    string attrVal = child.GetAttributeValue("onmouseover", "SHIT");
                                    imageDimensions = Regex.Match(attrVal, @"src=.+?(http[\S]+?jpg)").Groups[1].Value;
                                    imageDimensions = GetImageDimensions(Link_transform(imageDimensions)).Result;
                                    break;
                                }
                            }
                            if (prefix == "IMPORTANT:")
                            {
                                continue;
                            }
                            else if (string.IsNullOrEmpty(prefix))
                            {
                                prefix = "NoPrefix";
                            }
                            output += $"{prefix};{imageDimensions};{galleryName};{GalleryInfo(galleryName)};{gallerylink}\n";
                            stw.Stop();
                            pageinfo += $"Page #{c};ElapsedTime:{stw.ElapsedMilliseconds / 1000}";
                        }
                        catch
                        {
                            //MessageBox.Show($"page#{c}\n{node.InnerHtml}");
                            //ignore
                        }
                    }
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.Message);
                    // ignored
                }
                VipersInfo.IsEnabled = false;
                WriteToFile(Path.Combine(RootFolder, "FileInfo.txt"), pageinfo);
            }
            //Random rnd = new Random();
            //int r = rnd.Next(1, 10000000);
            string filepath;
            filepath = Path.Combine(RootFolder, $"Vipers_{Regex.Replace(txtbox41.Text.Substring(txtbox41.Text.LastIndexOf('/') + 1), @"\?", "")}.csv");
            File.WriteAllText(filepath, output);
            await XlsxConverter.ConvertToXLSX(filepath, filepath.Replace("csv", "xlsx"));
            Process.Start(filepath.Replace("csv", "xlsx"));
        }

        private string GalleryInfo(string galleryName)
        {
            string quantity = "", gallerynameSketch = "";
            string celebrities;
            if (Regex.IsMatch(galleryName, @"([\S\s]{2,}?) -"))
            {
                var celebritiesSketch = Regex.Match(galleryName, @"([\S\s]{2,}?) -");
                celebrities = celebritiesSketch.Groups[1].Value;
                gallerynameSketch = galleryName.Replace(celebritiesSketch.Groups[0].Value, "").Trim();
            }
            else
            {
                celebrities = "NoInfo";
            }

            string date;
            if (Regex.IsMatch(gallerynameSketch, @"\(([a-z]{3,4}\s*\d+,\s*\d{2,4})\)", RegexOptions.IgnoreCase))
            {
                var dateSketch = Regex.Match(gallerynameSketch, @"\(\s?([a-zа-я]{3,4}\s*\d+,\s*\d{2,4})\s?\)", RegexOptions.IgnoreCase);
                date = dateSketch.Groups[1].Value;
                gallerynameSketch = gallerynameSketch.Replace(dateSketch.Groups[0].Value, "").Trim();

            }
            else if (Regex.IsMatch(gallerynameSketch, @"\(([\d-\.\/]{5,10})\)"))
            {
                var dateSketch = Regex.Match(gallerynameSketch, @"\(([\d-\.\/]{5,10})\)");
                date = dateSketch.Groups[1].Value;
                gallerynameSketch = gallerynameSketch.Replace(dateSketch.Groups[0].Value, "").Trim();
            }
            else if (Regex.IsMatch(gallerynameSketch, @"([\d-\.\/]{5,10})"))
            {
                var dateSketch = Regex.Match(gallerynameSketch, @"([\d-\.\/]{5,10})");
                date = dateSketch.Groups[1].Value;
                gallerynameSketch = gallerynameSketch.Replace(dateSketch.Groups[0].Value, "").Trim();
            }
            else
            {
                date = "NoInfo";
            }
            if (Regex.IsMatch(gallerynameSketch, @"(\d{2,4}x|x\d{2,4})", RegexOptions.IgnoreCase))
            {
                var quantitySketch = Regex.Match(gallerynameSketch, @"(\d{2,4}x|x\d{2,4})", RegexOptions.IgnoreCase);
                quantity = quantitySketch.Value.Replace("x", "").Replace("X", "");
                gallerynameSketch = gallerynameSketch.Replace(quantitySketch.Value, "").Trim();

            }
            return $"{celebrities};{date};{quantity};{gallerynameSketch}";
        }



        private async void Search_Click(object sender, RoutedEventArgs e)
        {
            await SearchGalleries();
        }
        private async Task SearchGalleries()
        {
            if (!string.IsNullOrWhiteSpace(txtbox42.Text) && !txtbox42.Text.Contains("http"))
            {
                List<string> fromTextBox = txtbox42.Text.Trim().Split('\n').Select(x => x.Trim()).ToList();
                PrefixChoice = MakePrefix(fromTextBox);
            }
            Regex pat = new Regex($@"https?:\/\/{GalleryWebsite}.*");
            if (pat.IsMatch(txtbox41.Text))
            {
                await GetTopicsToTxtBox();
                return;
            }
            string sdata = $"searchthreadid=&" +
                            $"s=&" +
                            $"securitytoken=guest&" +
                            $"searchfromtype=vBForum%3APost&" +
                            $"do=process&" +
                            $"contenttypeid=1&" +
                            $"query={txtbox41.Text.Replace(" ", "+")}&" +
                            $"titleonly=0&" +//1 или 0
                            $"searchuser=&" +
                            $"starteronly=0&" +
                            $"tag=&" +
                            $"{Forumchoice()}" +
                            $"childforums=1&" +
                            $"{PrefixChoice}" +
                            $"replyless=0&" +
                            $"replylimit=&" +
                            $"searchdate=0&" +
                            $"beforeafter=after&" +
                            $"sortby=dateline&" +
                            $"order=descending&" +
                            $"showposts=0&" +
                            $"dosearch=Search+Now";
            var content = new ByteArrayContent(Encoding.GetEncoding("iso-8859-1").GetBytes(sdata));
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");
            var post = client.PostAsync($"https://{GalleryWebsite}/search.php?contenttype=1&do=process", content);
            await post.ContinueWith(t => Dispatcher.Invoke(() => txtbox41.Text = t.Result.Headers.Location.AbsoluteUri));
            Dispatcher.Invoke(() => Browser.Load(txtbox41.Text));
            await Browser.GetSourceAsync().ContinueWith(taskHtml => taskHtml.Result);
        }

        private async Task GetTopicsToTxtBox()
        {
            await GetTopicsFromHtml(await Browser.GetSourceAsync());
        }
        private async Task GetTopicsFromHtml(string html)
        {
            await Task.Run(() =>
            {
                try
                {
                    HtmlDocument hDoc = new HtmlDocument();
                    hDoc.LoadHtml(html);
                    var nodes = hDoc.DocumentNode.SelectNodes("//*[@class=\"searchtitle\"]");
                    List<string> output = new List<string>();
                    foreach (HtmlNode node in nodes)
                    {
                        if (node.HasChildNodes)
                        {
                            var children = node.ChildNodes;
                            foreach (HtmlNode child in children)
                            {
                                if (child.Name.ToUpper() == "A")
                                    if (child.HasClass("title"))
                                        output.Add($"http://{GalleryWebsite}/" + child.GetAttributeValue("href", ""));
                            }
                        }
                    }
                    Dispatcher.Invoke(() => { txtbox42.Text = string.Join("\n", output); });
                }
                catch
                {

                }
            });
        }
        private string PrefixChoice { get; set; }
        private void txtbox45_TextChanged(object sender, TextChangedEventArgs e)
        {
            Regex pat = new Regex($@"{txtbox45.Text}", RegexOptions.IgnoreCase);
            listbox43.ItemsSource = ForListBox.Where(x => pat.IsMatch(x));
            listbox43.SelectAll();
        }
        private void listbox43_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            List<string> fromlistbox = listbox43.SelectedItems.Cast<string>().ToList();
            PrefixChoice = MakePrefix(fromlistbox);

            if (!PreventUnselection)
            {
                listbox44.UnselectAll();
                websiteListBox.UnselectAll();
            }
            PreventUnselection = false;
        }
        private string Forumchoice()
        {
            string output = "";
            IEnumerable<string> fromchoicefile = File.ReadLines("ForumChoice.csv");
            foreach (string str in fromchoicefile)
            {
                output += $"forumchoice%5B%5D={str}&";
            }
            return output;
        }

        private void Favorites_Click(object sender, RoutedEventArgs e)
        {
            using (FileStream fs = new FileStream("PrefixFavorites.csv", FileMode.OpenOrCreate))
            {

                using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
                {
                    var fromfile = sr.ReadToEnd().Split().ToList();
                    using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
                    {
                        foreach (var str in listbox43.SelectedItems.Cast<string>().ToList())
                        {
                            if (!fromfile.Contains(str))
                            {
                                sw.WriteLine(str);
                            }
                        }
                    }
                }
            }
            LoadFavorites();
        }
        private void WriteToFile(string filePath, string str)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Append))
            {
                using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
                {

                    sw.WriteLine(str);
                }
            }
        }
        private void listbox44_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            List<string> fromlistbox = listbox44.SelectedItems.Cast<string>().ToList();
            PrefixChoice = MakePrefix(fromlistbox);
            if (!PreventUnselection)
            {
                listbox43.UnselectAll();
                websiteListBox.UnselectAll();
            }
            PreventUnselection = false;
        }
        private string MakePrefix(List<string> prefixList)
        {
            return $"prefixchoice%5B%5D=-2&";//TODO THIS
            string output = "";
            foreach (string str in prefixList)
            {
                output += $"prefixchoice%5B%5D={str}&";
            }
            return output;
        }
        private void Unselect_Click(object sender, RoutedEventArgs e)
        {
            listbox43.UnselectAll();
            listbox44.UnselectAll();
            websiteListBox.UnselectAll();

        }

        private void DeleteFavs_Click(object sender, RoutedEventArgs e)
        {
            using (FileStream fs = new FileStream("PrefixFavorites.csv", FileMode.OpenOrCreate))
            {

                using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
                {
                    var fromfile = sr.ReadToEnd().Split('\r').Select(x => x.Trim()).ToList();
                    using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
                    {
                        foreach (var str in listbox44.SelectedItems.Cast<string>().ToList())
                        {
                            fromfile.Remove(str);
                        }
                    }
                    File.WriteAllText("PrefixFavorites.csv", string.Join("\r\n", fromfile));
                }
            }
            LoadFavorites();
        }
        private bool PreventUnselection;
        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            if (listbox43.SelectedItems.Count > 0)
            {
                PreventUnselection = true;
                listbox43.SelectAll();
            }
            else if (listbox44.SelectedItems.Count > 0)
            {
                PreventUnselection = true;
                listbox44.SelectAll();
            }
            else if (websiteListBox.SelectedItems.Count > 0)
            {
                PreventUnselection = true;
                websiteListBox.SelectAll();
            }
        }
        private void GoTo_Click(object sender, RoutedEventArgs e)
        {
            //WBrowser.Source = new Uri(txtbox41.Text);
            if (!string.IsNullOrWhiteSpace(txtbox51.Text))
            {
                Browser.Address = txtbox51.Text;
            }
        }

        private void GoBack_Click(object sender, RoutedEventArgs e)
        {
            if (Browser.CanGoBack)
            {
                Browser.Back();
            }
        }

        private void GoForward_Click(object sender, RoutedEventArgs e)
        {
            if (Browser.CanGoForward)
            {
                Browser.Forward();
            }
        }

        private void history_change(object sender, SelectionChangedEventArgs e)
        {
            Browser.Load(History.SelectedItem.ToString());
        }

        private void txtbox41_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtbox41.Text.Contains($"{GalleryWebsite}/forums/") || txtbox41.Text.Contains($"{GalleryWebsite}/forumdisplay.php"))
            {
                VipersInfo.IsEnabled = true;
                Search.IsEnabled = false;
            }

            else
            {
                VipersInfo.IsEnabled = false;
                Search.IsEnabled = true;
            }
        }


        private void txtbox42_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtbox42.Text = Regex.Replace(txtbox42.Text.Trim(), @"\s+", "\n") + "\n";
        }

        private async void AliasesAtSites_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtbox41.Text))
                return;
            websiteListBox.ItemsSource = new List<string>();
            aliasListBox.ItemsSource = new List<string>();
            Profile profile = await PTG.GirlAliases(txtbox41.Text);
            var groups = profile.entry.GroupBy(x => x.Alias).OrderByDescending(g => g.Count());
            aliasGroups = groups.Select(g => (Alias: g.Key, WebSite: g, Count: g.Count()));
            List<string> aliases = new List<string>();
            foreach (var (Alias, WebSite, Count) in aliasGroups)
            {
                aliases.Add($"{Alias} {Count} times");
            }
            aliasListBox.ItemsSource = aliases;
        }

        IEnumerable<(string Alias, IGrouping<string, Entry> WebSite, int Count)> aliasGroups;

        private void aliasListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            object selected = aliasListBox.SelectedItem;
            if (selected == null)
                return;
            string selectedAlias = Regex.Replace(selected as string, @"\s\d{1,3}\stimes$", "");
            var webSites = aliasGroups.Where(g => g.Alias == selectedAlias).Select(x => x.WebSite);
            List<string> output = new List<string>();
            foreach (var entry in webSites)
            {
                foreach (var x in entry)
                {
                    output.Add(x.WebSite);
                }
            }
            websiteListBox.ItemsSource = output;
            websiteListBox.SelectAll();
            PrefixChoice = MakePrefix(websiteListBox.SelectedItems.Cast<string>().ToList());
            if (!PreventUnselection)
            {
                listbox43.UnselectAll();
                listbox44.UnselectAll();
            }
            PreventUnselection = false;
            txtbox41.Text = Regex.Replace(selectedAlias, @"\s\d{1,3}\stimes$", "");
        }



        public async Task<string> PageHtml(string url)
        {
            try
            {
                return await client.GetStringAsync(url);
            }
            catch
            {
                return "";
            }
        }

        private void txtbox62_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        static readonly string xmlPath = @"C:\Users\dmytr\OneDrive\Desktop\CDQualities.xml";
        private static void XmlSerialize(CBQualities qualities)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(CBQualities));
            //string timeNow = DateTime.Now.ToString(CultureInfo.GetCultureInfo("ja-jp")).Replace("/","").Replace(":","");
            using (FileStream fs = new FileStream(xmlPath, FileMode.OpenOrCreate))
            {
                serializer.Serialize(fs, qualities);
            }

        }
        private static void XmlSerialize<T>(T qualities, string path = null)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                //string timeNow = DateTime.Now.ToString(CultureInfo.GetCultureInfo("ja-jp")).Replace("/","").Replace(":","");
                using (FileStream fs = new FileStream(path ?? xmlPath, FileMode.OpenOrCreate))
                {
                    serializer.Serialize(fs, qualities);
                }
            }
            catch (Exception e)
            {

                MessageBox.Show(e.Message);
            }

        }
        private static T XmlDeserialize<T>(string filepath = null)
        {
            string _filepath = filepath ?? xmlPath;
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            if (!File.Exists(_filepath))
            {
                return default;
            }
            //string timeNow = DateTime.Now.ToString(CultureInfo.GetCultureInfo("ja-jp")).Replace("/","").Replace(":","");
            try
            {
                using (FileStream fs = new FileStream(_filepath, FileMode.Open))
                {
                    return (T)serializer.Deserialize(fs);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"{e.Message}\nОшибка десериализации!");
                return default;
            }

        }
        int count;

        private async Task<string> GetQuality(string url)
        {
            try
            {
                Driver.Navigate().GoToUrl(url);
                WebDriverWait wait = new WebDriverWait(Driver, new TimeSpan(0, 0, 5));
                //wait.Timeout(3000);
                wait.Until(ExpectedConditions.ElementExists(By.XPath("//*[@id=\"main\"]/div/div[6]/div[2]/div[1]/div[3]/div[6]/div[1]")));
                return await RetrieveQuality();
            }
            catch
            {
                if (Driver.PageSource.Contains("Room is currently offline"))
                    return "OFFLINE";
                if (Driver.PageSource.Contains("Please complete the security check to access"))
                    return "CLOUDFARE";
                if (Driver.PageSource.Contains("Access denied. This room is not available to your region or gender."))
                    return "DENIED";
                return null;
            }
        }
        private async Task<string> RetrieveQuality()
        {
            return await Task.Run(() =>
            {
                try
                {
                    string html = Driver.PageSource;

                    HtmlDocument hDoc = new HtmlDocument();
                    hDoc.LoadHtml(html);
                    HtmlNode node = hDoc.DocumentNode.SelectSingleNode("//*[@id=\"main\"]/div/div[6]/div[2]/div[1]/div[3]/div[6]/div[1]");
                    return node.InnerText.Trim();
                }
                catch
                {
                    return null;
                }
            });
        }
        private bool NeedToUpdate(string date)
        {
            if (DateTime.TryParse(date, out DateTime result))
            {
                if (DateTime.Now - result > new TimeSpan(3, 0, 0))
                    return true;
                return false;
            }
            return true;
        }

        private void VideoWebsiteLike_Click(object sender, RoutedEventArgs e)
        {
            var match = Regex.Match(txtBox31.Text, $@"^(?:https?:\/\/(?:www.)?)?{VideoWebsite}/video/(\d+)/");
            if (match.Success)
            {
                string url = $"{txtBox31.Text.TrimEnd('/')}?mode=async&format=json&action=add_to_favourites&video_id={match.Groups[1].Value}&album_id=&fav_type=0&playlist_id=0";
                var req = (HttpWebRequest)WebRequest.Create(new Uri(url));
                req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                req.Headers.Add("Accept-Language: uk-UA,uk;q=0.8,en-US;q=0.5,en;q=0.3");
                req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:74.0) Gecko/20100101 Firefox/74.0";
                req.Timeout = 30000;
                req.CookieContainer = CcToFile;
                req.AllowAutoRedirect = false;
                req.KeepAlive = true;
                using (var res = (HttpWebResponse)req.GetResponse())
                {
                    foreach (System.Net.Cookie cookie in res.Cookies)
                        CcToFile.Add(new System.Net.Cookie(cookie.Name, cookie.Value, "/", $".{req.Host}"));
                }
            }
        }

        private void albums_Checked(object sender, RoutedEventArgs e)
        {
            if (albums.IsChecked == true)
                tryGetImageSize.IsChecked = true;
        }
        private List<TrexVideo> ToFilter { get; set; }
        private void FilterVideos_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                string filter = Dispatcher.Invoke(() => FilterVideos.Text);
                TrexVideoDataGrid.ItemsSource = ToFilter.Where(x => Regex.IsMatch(x.Title, $@"{filter}", RegexOptions.IgnoreCase)).OrderByDescending(x => x.Id).ToList();
                AdjustColumnWidth(ToFilter[0]);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }
        private void AdjustColumnWidth(object type)
        {
            if (type is ITitle)
            {
                PTDataGrid.Columns[2].Width = new DataGridLength(150);
                PTDataGrid.Columns[1].Visibility = Visibility.Collapsed;
                //PTDataGrid.Columns[1].Width = new DataGridLength(350);
                return;
            }
            if (type is TrexVideo)
            {
                TrexVideoDataGrid.Columns[0].Width = new DataGridLength(250);
                TrexVideoDataGrid.Columns[1].Width = new DataGridLength(350);
                return;
            }

        }
        private void ChromiumWebBrowser_OnFrameLoadEnd(object sender, FrameLoadEndEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                txtbox51.Text = e.Url;
                GoBack.IsEnabled = Browser.CanGoBack;
                GoTo.IsEnabled = !string.IsNullOrWhiteSpace(txtbox51.Text);
                GoForward.IsEnabled = Browser.CanGoForward;
            }));
        }

        private void txtBox32_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (PTDataGrid == null || PTDataGrid.Items.Count == 0)
                    return;
                string filter = Dispatcher.Invoke(() => txtBox32.Text);
                var item = PTDataGrid.Items[0];
                if (ToFilterPTDataGrid.Any(x => x is Category))
                    PTDataGrid.ItemsSource = ToFilterPTDataGrid.Select(x => x as Category).Where(x => Regex.IsMatch(x.Title, $@"{filter}", RegexOptions.IgnoreCase)).OrderByDescending(x => x.IsLocal).ToList();
                if (ToFilterPTDataGrid.Any(x => x is Model))
                    PTDataGrid.ItemsSource = ToFilterPTDataGrid.Select(x => x as Model).Where(x => Regex.IsMatch(x.Title, $@"{filter}", RegexOptions.IgnoreCase)).OrderByDescending(x => x.IsLocal).ToList();
                if (ToFilterPTDataGrid.Any(x => x is Member))
                    PTDataGrid.ItemsSource = ToFilterPTDataGrid.Select(x => x as Member).Where(x => Regex.IsMatch(x.Title, $@"{filter}", RegexOptions.IgnoreCase)).OrderByDescending(x => x.IsLocal).ToList();

                AdjustColumnWidth(ToFilterPTDataGrid[0]);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void PTDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PTDataGrid == null || PTDataGrid.Items.Count == 0)
                return;
            if (PTDataGrid.SelectedItem is Model)
            {
                var model = PTDataGrid.SelectedItem as Model;
                if (!model.IsLocal)
                    return;
                var trexVideos = XmlDeserialize<TrexVideos>(Path.Combine(RootFolder, model.LocalPath));
                if (trexVideos == null)
                    return;
                ToFilter = trexVideos.TrexVideosList.OrderByDescending(x => x.Id).ToList();
                TrexVideoDataGrid.ItemsSource = ToFilter;
            }
            else if (PTDataGrid.SelectedItem is Category)
            {
                var cat = PTDataGrid.SelectedItem as Category;
                if (!cat.IsLocal)
                    return;
                var trexVideos = XmlDeserialize<TrexVideos>(Path.Combine(RootFolder, cat.LocalPath));
                if (trexVideos == null)
                    return;
                ToFilter = trexVideos.TrexVideosList.OrderByDescending(x => x.Id).ToList();
                TrexVideoDataGrid.ItemsSource = ToFilter;
            }
            else if (PTDataGrid.SelectedItem is Member)
            {
                var member = PTDataGrid.SelectedItem as Member;
                if (!member.IsLocal)
                    return;
                var trexVideos = XmlDeserialize<TrexVideos>(Path.Combine(RootFolder, member.LocalPath));
                if (trexVideos == null)
                    return;
                ToFilter = trexVideos.TrexVideosList.OrderByDescending(x => x.Id).ToList();
                TrexVideoDataGrid.ItemsSource = ToFilter;
            }
        }

        private void PTDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "LocalPath")
            {
                e.Column.Visibility = Visibility.Collapsed;

            }



        }
        private List<VideoWebsitePreviewImage> GetVideoPreviews(string VideoUrl)
        {
            List<VideoWebsitePreviewImage> images = new List<VideoWebsitePreviewImage>();
            string digits = Regex.Match(VideoUrl, $@"https?:\/\/(?:www\.){VideoWebsite}\/video\/(\d+)\/").Groups[1].Value;
            string digitsOfSeries = digits.Substring(0, digits.Length - 3) + "000";
            for (int i = 1; i <= 10; i++)
            {
                images.Add(new VideoWebsitePreviewImage($"https://www.{VideoWebsite}/contents/videos_screenshots/{digitsOfSeries}/{digits}/300x168/{i}.jpg", i));
            }
            return images;
        }
        private void PreviewPhotoDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PreviewPhotoDataGrid.SelectedItem == null)
                return;
            if (!tokenSource2.IsCancellationRequested)
                tokenSource2.Cancel();
            VideoWebsitePreviewImage videoWebsitePreviewImage = PreviewPhotoDataGrid.SelectedItem as VideoWebsitePreviewImage;
            ShowImage(videoWebsitePreviewImage.Url);
        }
        private void ShowImage(string url)
        {
            BitmapImage bi3 = new BitmapImage();
            bi3.CacheOption = BitmapCacheOption.OnDemand;
            //bi3.UriCachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
            bi3.BeginInit();
            bi3.UriSource = new Uri(url, UriKind.Absolute);
            bi3.EndInit();
            ImagePreview.Stretch = Stretch.Uniform;
            ImagePreview.Source = bi3;

        }
        CancellationTokenSource tokenSource2;
        private async void TrexVideoDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TrexVideoDataGrid.SelectedItem == null)
                return;
            tokenSource2 = new CancellationTokenSource();
            CancellationToken ct = tokenSource2.Token;
            var video = TrexVideoDataGrid.SelectedItem as TrexVideo;
            if (VideoGroups != null && VideoGroups.Count != 0)
            {
                var group = VideoGroups.FirstOrDefault(x => x.Link == video.Link);
                if (group != null)
                    TrexVideoLinks.ItemsSource = group.Videos;
            }
            ImagePreviews = GetVideoPreviews(video.Link);
            PreviewPhotoDataGrid.ItemsSource = ImagePreviews;
            PreviewPhotoDataGrid.Columns[0].Visibility = Visibility.Collapsed;
            await ShowImageNonStrop(ImagePreviews, ct);
        }
        private async Task ShowImageNonStrop(List<VideoWebsitePreviewImage> images, CancellationToken ct)
        {
            await Task.Run(() =>
            {
                TimeSpan ts = new TimeSpan(0, 0, 0, 0, 500);
                int i = 0;
                int last = images.Count - 1;
                TrexVideo item = Dispatcher.Invoke(() => TrexVideoDataGrid.SelectedItem as TrexVideo);
                while (true)
                {
                    DateTime start = DateTime.Now;
                    while (true)
                    {
                        if ((start + ts) < DateTime.Now)
                            break;
                    }
                    if (i > last)
                        return;
                    if (tokenSource2.IsCancellationRequested)
                    {
                        tokenSource2.Dispose();
                        return;
                    }
                    if (Dispatcher.Invoke(() => TrexVideoDataGrid.SelectedItem as TrexVideo) != item)
                        return;
                    Dispatcher.Invoke(() => ShowImage(images[i].Url));
                    i++;
                }
            }, ct);
        }
        private List<VideoWebsitePreviewImage> ImagePreviews { get; set; }

        private void TrexVideoDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "Link")
            {
                e.Column.MaxWidth = 350;

            }

            if (e.PropertyName == "Title")
            {
                e.Column.MaxWidth = 200;

            }
            if (e.PropertyType == typeof(double))
                (e.Column as DataGridTextColumn).Binding.StringFormat = "F2";

        }

        TrexVideo trexVideo;
        private void TrexVideoDataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            var t = e.Row.Item;
            if (t is TrexVideo)
            {
                trexVideo = t as TrexVideo;
                trexVideo.ChangeEvent += AsyncEvent;
            }
        }

        private async void AsyncEvent(TrexVideo video)
        {
            await Task.Run(() =>
            {
                object it = null;
                string path;
                Dispatcher.Invoke(() => it = PTDataGrid.SelectedItem);

                if (it is Model)
                {
                    Model item = Dispatcher.Invoke(() => PTDataGrid.SelectedItem as Model);
                    path = item.LocalPath;
                }
                else if (it is Category)
                {
                    Category item = Dispatcher.Invoke(() => PTDataGrid.SelectedItem as Category);
                    path = item.LocalPath;
                }
                else
                {
                    User item = Dispatcher.Invoke(() => PTDataGrid.SelectedItem as User);
                    path = item.LocalPath;
                }
                var items = TrexVideoDataGrid.Items.OfType<TrexVideo>().OrderByDescending(x => x.Id).ToList();
                XmlSerialize(new TrexVideos() { TrexVideosList = items }, Path.Combine(RootFolder, path));
            });
        }

        private void fileItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (fileItems.Text == "Categories")
            {
                if (!File.Exists(Path.Combine(RootFolder, "Categories.xml")))
                {
                    MessageBox.Show("Не существует");
                    //ToFilterPTDataGrid = null;
                    //PTDataGrid.ItemsSource = null;
                    //ChangeDataMenuItem.Header = "To Members";
                    return;
                }
                var deserialized = XmlDeserialize<Categories>(Path.Combine(RootFolder, "Categories.xml")).Elements;
                PTDataGrid.ItemsSource = deserialized;
                ToFilterPTDataGrid = deserialized.Select(x => x as ITitle).ToList();
            }
            else if (fileItems.Text == "Members")
            {
                if (!File.Exists(Path.Combine(RootFolder, "Members.xml")))
                {
                    MessageBox.Show("Не существует");
                    //ToFilterPTDataGrid = null;
                    //PTDataGrid.ItemsSource = null;
                    //ChangeDataMenuItem.Header = "To Members";
                    return;
                }
                var deserialized = XmlDeserialize<Members>(Path.Combine(RootFolder, "Members.xml"))?.Elements;
                if (deserialized == null)
                {
                    deserialized = new List<Member>();
                }
                PTDataGrid.ItemsSource = deserialized;
                ToFilterPTDataGrid = deserialized.Select(x => x as ITitle).ToList();
            }
            else if (fileItems.Text == "Models")
            {
                if (!File.Exists(Path.Combine(RootFolder, "Models.xml")))
                {
                    MessageBox.Show("Не существует");
                    //ToFilterPTDataGrid = null;
                    //PTDataGrid.ItemsSource = null;
                    //ChangeDataMenuItem.Header = "To Members";
                    return;
                }
                var deserialized = XmlDeserialize<Models>(Path.Combine(RootFolder, "Models.xml")).Elements;
                PTDataGrid.ItemsSource = deserialized;
                ToFilterPTDataGrid = deserialized.Select(x => x as ITitle).ToList();
            }
            else
            {
                if (!File.Exists(Path.Combine(RootFolder, "DirectLinks.xml")))
                {
                    MessageBox.Show("Не существует");
                    //ToFilterPTDataGrid = null;
                    //PTDataGrid.ItemsSource = null;
                    //ChangeDataMenuItem.Header = "To Members";
                    return;
                }
                return;//TODO SHIT
                var deserialized = XmlDeserialize<Models>(Path.Combine(RootFolder, "DirectLinks.xml")).Elements;
                PTDataGrid.ItemsSource = deserialized;
                ToFilterPTDataGrid = deserialized.Select(x => x as ITitle).ToList();
            }
        }

    }
    public class VideoWebsitePreviewImage
    {
        public VideoWebsitePreviewImage(string url, int number)
        {
            Url = url;
            Number = number;

        }
        public string Url { get; set; }
        public int Number { get; set; }
    }
    public class CBQualities
    {
        public List<CBQuality> CBQualityList { get; set; } = new List<CBQuality>();
    }
    public class CBQuality
    {
        public string Name { get; set; }
        public string Quality { get; set; }
        public string Link { get; set; }
        public string Gender { get; set; }
        public string Age { get; set; }
        public string Date { get; set; }
    }
    class VideoInfo
    {
        private string duration;
        public string Duration
        {
            get
            {
                return duration;
            }

            set
            {
                duration = ReturnDuration(value);
            }
        }
        public string Title { get; set; }
        public string Link { get; set; }
        private static string ReturnDuration(string Time)
        {
            if (Regex.IsMatch(Time.Trim(), @"^\d{1,2}:\d{2}$"))
            {
                Match match = Regex.Match(Time, @"^(\d{1,2}):(\d{2})$");
                return (Convert.ToDouble(match.Groups[1].Value) / 1440 + Convert.ToDouble(match.Groups[2].Value) / 86400).ToString(CultureInfo.GetCultureInfo("en-us"));
            }
            else if (Regex.IsMatch(Time, @"^\d{1,2}:\d{2}:\d{2}$"))
            {
                Match match = Regex.Match(Time, @"^(\d{1,2}):(\d{2}):(\d{2})$");
                return (Convert.ToDouble(match.Groups[1].Value) / 24 + Convert.ToDouble(match.Groups[2].Value) / 1440 + Convert.ToDouble(match.Groups[3].Value) / 86400).ToString(CultureInfo.GetCultureInfo("en-us"));
            }
            return Time;

        }
    }

    public class SearchContextMenuHandler : IContextMenuHandler
    {
        private const int CopyLink = 26503;
        public void OnBeforeContextMenu(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model)
        {
            model.Clear();
            model.AddItem((CefMenuCommand)CopyLink, "Copy Link Address");
            model.SetEnabledAt(0, true);

        }

        public bool OnContextMenuCommand(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IContextMenuParams parameters, CefMenuCommand commandId, CefEventFlags eventFlags)
        {
            if ((int)commandId == CopyLink)
            {
                Clipboard.SetDataObject(parameters.LinkUrl);
                return true;
            }
            return false;
        }

        public void OnContextMenuDismissed(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame)
        {

        }

        public bool RunContextMenu(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model, IRunContextMenuCallback callback)
        {
            return false;
        }

    }
}
