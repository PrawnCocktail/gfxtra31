using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gfxtra31
{
    class Program
    {
        static void Main(string[] args)
        {
            List<OutputClasses> itemList = new List<OutputClasses>();

            //string url = "";
            int first = 1;
            int last = 0;

            foreach (var argument in args)
            {
                if (argument.StartsWith("first="))
                {
                    first = Convert.ToInt32(argument.Replace("first=", ""));
                }
                else if (argument.StartsWith("last="))
                {
                    last = Convert.ToInt32(argument.Replace("last=", ""));
                }
                else
                {
                    printHelp();
                }
            }

            int totalPages;
            if (last == 0)
            {
                 totalPages = getTotalPages("https://www.gfxtra31.com/sitemap/page/1/");
            }
            else
            {
                totalPages = last;
            }

            int pageNum;
            if (first == 1)
            {
                pageNum = 1;
            }
            else
            {
                pageNum = first;
            }

            while (pageNum <= totalPages)
            {
                Console.WriteLine("Processing page: " + pageNum);
                
                var url = "https://www.gfxtra31.com/sitemap/page/" + pageNum;
                var web = new HtmlWeb();
                var doc = web.Load(url);
                var items = doc.DocumentNode.SelectNodes("//div[@id='mcontent_inner_box']//div[@class='quote']//a");

                string pageTitle;
                string pageUrl;
                string fileUrl;

                foreach (var item in items)
                {
                    OutputClasses itemObj = new OutputClasses();

                    pageTitle = item.InnerText;
                    Console.WriteLine(pageTitle);
                    itemObj.Name = pageTitle.Replace(",", "");

                    pageUrl = item.Attributes["href"].Value;
                    Console.WriteLine(pageUrl);
                    itemObj.PageUrl = pageUrl;

                    //wait  between each item on a page. should help prevent flooding the server
                    //Thread.Sleep(1000);

                    var parseResult = parseItemPage(pageUrl);
                    fileUrl = parseResult.Item1;
                    Console.WriteLine(fileUrl);
                    itemObj.FileUrl = fileUrl;

                    itemList.Add(itemObj);

                    if (itemList.Count == 50)
                    {
                        WriteCSV(itemList, @"items.csv");
                        itemList.Clear();
                    }
                }

                pageNum++;
                Console.WriteLine();
                //wait 5 second between new page requests. should help prevent flooding the server
                //Thread.Sleep(5000);
            }

            Console.WriteLine("Finished scraping pages " + first + " to " + totalPages);
            Console.ReadLine();
        }

        static void printHelp()
        {
            Console.WriteLine("Invalid argument found.");
            Console.WriteLine("Valid arguments are...");
            Console.WriteLine("first - This is the number of the first page of search results. [Optional]");
            Console.WriteLine("last - This is the number of the last page of search results. [Optional]");
            Console.WriteLine("For example:");
            Console.WriteLine("gfxtra31.exe first=120 last=400");
            Console.WriteLine("This will process all pages starting with page 120 and ending on page 400.");
            Console.WriteLine("Press return to exit.");
            Console.ReadLine();
            Environment.Exit(0);
        }

        static Tuple<string, string> parseItemPage(string url)
        {
            var web = new HtmlWeb();
            var doc = web.Load(url);

            if (doc.Text.Contains("please refresh in several seconds"))
            {
                bool alive = false;
                while (alive == false)
                {
                    Console.WriteLine("Waiting to prevent overloading the site...");
                    //sleep x seconds
                    Thread.Sleep(180000);
                    doc = web.Load(url);
                    if (doc.Text.Contains("please refresh in several seconds"))
                    {
                        alive = false;
                    }
                    else
                    {
                        alive = true;
                    }
                }
            }

            //website is kinda unpredictable with its layout and html tags, so we just hope that all the file urls use the go.php redirect page. 
            var items = doc.DocumentNode.SelectNodes("//a[starts-with(@href, 'https://www.gfxtra31.com/engine/go.php?url=')]");
            if (items != null)
            {
                foreach (var item in items)
                {

                    //this url redirects back to the item page instead of the download, if you decode the base64 string it outputs
                    //the actual url that will take you to the download page. 
                    string goBase = Uri.UnescapeDataString(item.Attributes["href"].Value)
                        .Replace("https://www.gfxtra31.com/engine/go.php?url=", "");

                    //decode base64 string
                    byte[] data = Convert.FromBase64String(goBase);
                    string decodedBaseUrl = Encoding.UTF8.GetString(data);

                    string fileNextUrl = "";
                    //sometimes the url decoded is a redirect url or just the filenext url, so just check for that and handle accordingly. 
                    if (decodedBaseUrl.Contains("filenext.com"))
                    {
                        fileNextUrl = decodedBaseUrl;
                        return Tuple.Create(fileNextUrl, "");

                    }
                    else if (decodedBaseUrl.Contains("gftxra.net"))
                    {
                        //if it is a redirect url then follow and check where it redirects and use that as the filenext url. 
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(decodedBaseUrl);
                        request.AllowAutoRedirect = false;
                        var response = request.GetResponse();

                        fileNextUrl = response.Headers["Location"];
                        return Tuple.Create(fileNextUrl, "");

                    }
                }
            }
            else if (doc.DocumentNode.SelectNodes("//a[starts-with(@href, 'https://www.filenext.com/')]") != null)
            {

                var result = doc.DocumentNode.SelectNodes("//a[starts-with(@href, 'https://www.filenext.com/')]");
                foreach (var item in result)
                {
                    string fileNextUrl = item.Attributes["href"].Value;
                    return Tuple.Create(fileNextUrl, "");
                }

            }

            return Tuple.Create("", "");

            //var passSearch = doc.DocumentNode.SelectNodes("//div[starts-with(@id, 'news-id-')]");
            //foreach (var tag in passSearch)
            //{
            //    if (tag.InnerText.Contains("P A S S W O R D"))
            //    {

            //    }
            //}
        }

        static int getTotalPages(string url)
        {
            url = "https://www.gfxtra31.com/sitemap/page/1";
            var web = new HtmlWeb();
            var doc = web.Load(url);
            var navNode = doc.DocumentNode.SelectNodes("//div[@class='navigation']//a");

            int totalPages = 0;
            foreach (var item in navNode)
            {
                int num;
                bool result = Int32.TryParse(item.InnerText, out num);
                if (result)
                {
                    if (num > totalPages)
                    {
                        totalPages = num;
                    }
                }
            }
            Console.WriteLine("Total Pages: " + totalPages);
            Console.WriteLine();
            return totalPages;
        }

        static void WriteCSV<T>(IEnumerable<T> items, string path)
        {
            Type itemType = typeof(T);
            var props = itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                .OrderBy(p => p.Name);

            bool writeHeader = false;
            if (!File.Exists(path))
            {
                writeHeader = true;
            }

            using (var writer = new StreamWriter(path, true))
            {
                if (writeHeader == true)
                {
                    writer.WriteLine(string.Join(", ", props.Select(p => p.Name)));
                }

                foreach (var item in items)
                {
                    writer.WriteLine(string.Join(", ", props.Select(p => p.GetValue(item, null))));
                }
            }
        }
    }
}
