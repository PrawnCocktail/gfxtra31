using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace gfxtra31
{
    class Program
    {
        static void Main(string[] args)
        {
            Tuple<int, int> pages = argumentHandle(args);
            
            mainLoop(pages.Item1, pages.Item2);

            Console.WriteLine("Finished scraping pages " + pages.Item1 + " to " + pages.Item2);
            Console.ReadLine();
        }

        static Tuple<int, int> argumentHandle(string[] args)
        {
            int first = 1;
            int last = 0;

            if (args.Length == 0)
            {
                Console.WriteLine("Enter the first page number you want to scrape. \nIf none is entered it will start from page 1");
                string fline = Console.ReadLine();

                if (!int.TryParse(fline, out first))
                {
                    first = 1;
                }
                Console.WriteLine("Starting on page: " + first);
                Console.WriteLine();

                Console.WriteLine("Enter the last page number you want to scrape. \nIf none is entered it will run until the end.");
                string lline = Console.ReadLine();

                if (!int.TryParse(lline, out last))
                {
                    last = 0;
                }
                Console.WriteLine("Starting on page: " + first);
            }
            else
            {
                //deal with arguments here. 
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
            }

            //checks and sets last page number
            int totalPages;
            if (last == 0)
            {
                totalPages = getTotalPages("https://www.gfxtra31.com/sitemap/page/1/");
            }
            else
            {
                totalPages = last;
            }

            //sets the first page number
            int pageNum = first;

            return Tuple.Create(pageNum, totalPages);
        }

        static void mainLoop(int pageNum, int totalPages)
        {
            List<OutputClasses> itemList = new List<OutputClasses>();

            //loop though until you get to the final page, be it the total number of page or the number the user entered. 
            while (pageNum <= totalPages)
            {
                //color the new page text green so its easier to see
                Console.ForegroundColor = ConsoleColor.Green;
                Console.BackgroundColor = ConsoleColor.Black;
                Console.Write("Processing page: " + pageNum);
                Console.ResetColor();
                Console.WriteLine();

                var url = "https://www.gfxtra31.com/sitemap/page/" + pageNum;
                var web = new HtmlWeb();
                var doc = web.Load(url);

                //gets all the results from the page, 50 per page.
                var items = doc.DocumentNode.SelectNodes("//div[@id='mcontent_inner_box']//div[@class='quote']//a");

                //loops through the results 
                foreach (var item in items)
                {
                    //seperator cause why not.
                    Console.WriteLine();
                    Console.WriteLine("###########################################################################");
                    Console.WriteLine();

                    OutputClasses itemObj = new OutputClasses();

                    //gets the page title from the website and sets in the object 
                    string pageTitle = item.InnerText;
                    Console.WriteLine(pageTitle);
                    itemObj.Name = pageTitle.Replace(",", "");

                    //gets the page url from the website and sets in the object 
                    string pageUrl = item.Attributes["href"].Value;
                    Console.WriteLine(pageUrl);
                    itemObj.PageUrl = pageUrl;

                    //parses the page and gets all the filenext urls from the website and sets in the object 
                    var parseResult = parseItemPage(pageUrl);
                    string fileUrl = parseResult.Item1;
                    Console.WriteLine(fileUrl);
                    itemObj.FileUrl = fileUrl;

                    //adds it to the list
                    itemList.Add(itemObj);

                    //dump the list to csv every 50 records. thee is 50 on a page, so the most you can lose, should the program crash, is 1 page of records. 
                    //this can be changed to whatever tho.
                    if (itemList.Count == 50)
                    {
                        WriteCSV(itemList, @"links.csv");
                        itemList.Clear();
                    }
                }

                //increase the page number every loop.
                pageNum++;
                Console.WriteLine();
            }
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

            //used to check if the server is overloaded. if it is, it will wait to retry until it isnt overloadded
            if (doc.Text.Contains("please refresh in several seconds"))
            {
                bool alive = false;
                while (alive == false)
                {
                    Console.WriteLine("Waiting to prevent overloading the site...");
                    //this sleeps for 3 minutes, anything less and it seems to restart the server wait timer again.
                    Thread.Sleep(180000);

                    //after 3 mins reload the page and recheck the message.
                    doc = web.Load(url);
                    if (doc.Text.Contains("please refresh in several seconds"))
                    {
                        //if it contains the same message, then rerun this and wait another 3 minutes.
                        alive = false;
                    }
                    else
                    {
                        //if not, continue on as usual.
                        alive = true;
                    }
                }
            }

            //website is kinda unpredictable with its layout and html tags, so we just hope that all the file urls use the go.php redirect page. 
            var items = doc.DocumentNode.SelectNodes("//a[starts-with(@href, 'https://www.gfxtra31.com/engine/go.php?url=')]");
            List<string> pagefileurls = new List<string>();

            //prevents a crash is no urls are foung. 
            if (items != null)
            {
                //a single page can have multiple urls, so loop though them, and add them to a list.
                foreach (var item in items)
                {

                    //this url redirects back to the item page instead of the download, if you decode the base64 string it outputs
                    //the actual url that will take you to the download page. 
                    string goBase = Uri.UnescapeDataString(item.Attributes["href"].Value)
                        .Replace("https://www.gfxtra31.com/engine/go.php?url=", "");

                    //some urls on the page, even though they are the "go.php" urls, constain something other than a base64 string, so check that its a base64 string
                    if (IsBase64String(goBase))
                    {
                        //if is it base64 then decode the string
                        byte[] data = Convert.FromBase64String(goBase);
                        string decodedBaseUrl = Encoding.UTF8.GetString(data);

                        string fileNextUrl = "";
                        //sometimes the url decoded is a redirect url or just the filenext url, so just check for that and handle accordingly. 
                        if (decodedBaseUrl.Contains("filenext.com"))
                        {
                            fileNextUrl = decodedBaseUrl;
                            pagefileurls.Add(fileNextUrl);
                        }
                        else if (decodedBaseUrl.Contains("gftxra.net"))
                        {
                            //if it is a redirect url then follow and check where it redirects and use that as the filenext url. 
                            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(decodedBaseUrl);
                            request.AllowAutoRedirect = false;
                            var response = request.GetResponse();

                            fileNextUrl = response.Headers["Location"];
                            pagefileurls.Add(fileNextUrl);
                        }
                    }

                }
            }
            //not all urls on the page use the "go.php" redirection method, some are just plane filenext urls
            else if (doc.DocumentNode.SelectNodes("//a[starts-with(@href, 'https://www.filenext.com/')]") != null)
            {
                var result = doc.DocumentNode.SelectNodes("//a[starts-with(@href, 'https://www.filenext.com/')]");
                foreach (var item in result)
                {
                    string fileNextUrl = item.Attributes["href"].Value;
                    pagefileurls.Add(fileNextUrl);
                }

            }

            //convert the list of urls found on the page to a string seperated by a semicolon so its easier to put into the csv file.
            string fileUrls = String.Join(";", pagefileurls);
            return Tuple.Create(fileUrls, "");

            //was gonna try and fetch the passwords on the page but its a hell of a task. the formatting is hit and miss, some pages dont have passwords and some do.
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
            //loads the first page of results and checks to see how many pages.
            //this is only used if the user doesnt specify an end page 
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
            var props = itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            bool writeHeader = false;

            if (!File.Exists(path))
            {
                writeHeader = true;
            }

            using (var writer = new StreamWriter(path, true))
            {
                //if csv already exists then dont write header again
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

        static bool IsBase64String(string s)
        {
            s = s.Trim();
            return (s.Length % 4 == 0) && Regex.IsMatch(s, @"^[a-zA-Z0-9\+/]*={0,3}$", RegexOptions.None);

        }
    }
}
