using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CyberdropDownloader
{
    public class MyWebClient : WebClient
    {
        protected override WebRequest GetWebRequest(Uri address)
        {
            var req = base.GetWebRequest(address);
            req.Timeout = 10000000;//ms
            return req;
        }
    }
    class Program
    {
        private static void Title()
        {
            while (true)
            {
                Console.Title = "Downloading: "+AlbumName+" | Pic: "+Downloaded+"/"+ AlbumPics.Count;
            }
        }

        private static string Substring(string self, string left, string right, int startIndex = 0,
            StringComparison comparison = StringComparison.Ordinal, string fallback = null)
        {
            if (string.IsNullOrEmpty(self) || string.IsNullOrEmpty(left) || (startIndex < 0) ||
                startIndex >= self.Length)
                return fallback;
            int num1 = self.IndexOf(left, startIndex, comparison);
            if (num1 == -1)
                return fallback;
            int startIndex1 = num1 + left.Length;
            if (string.IsNullOrEmpty(right))
            {
                return self.Substring(startIndex1);
            }
            else
            {
                int num2 = self.IndexOf(right, startIndex1, comparison);
                return num2 == -1 ? fallback : self.Substring(startIndex1, num2 - startIndex1);
            }
        }

        private static string AlbumName = "";
        private static int Downloaded = 0;
        private static List<string> AlbumPics = new List<string>();

        private static async Task Main()
        {
            string[] lines = { };
            try
            {
                lines = File.ReadAllLines(@"links.txt");
            }
            catch
            {
                Console.WriteLine("[Error] links.txt not found, press enter to exit");
                Console.ReadLine();
                Environment.Exit(0);
            }
            Thread thread = new Thread(new ThreadStart(Title));
            thread.Start();
            if (lines.Length == 0 || lines.Length == null)
            {
                Console.WriteLine("[Error] no album links found, press enter to exit");
                Console.ReadLine();
                Environment.Exit(0);
            }
            foreach (var AlbumLink in lines)
            {
                string htmlCode = "";
                using (WebClient client = new WebClient())
                {
                    try
                    {
                        htmlCode = client.DownloadString(AlbumLink);
                    }
                    catch(Exception e)
                    {
                        if (e.ToString().Contains("(404) Not Found"))
                        {
                            continue;
                        }
                        else
                        {
                            Console.WriteLine("Unknown Error");
                            break;
                        }
                    }
                    
                }
                string pattern = @"<a class=""image"" href=""(.*?)"" target=""_blank"" title=""";
                RegexOptions regexOptions = RegexOptions.None;
                Regex regex = new Regex(pattern, regexOptions);
                string inputData = htmlCode;
                AlbumName = Substring(htmlCode, "<h1 id=\"title\" class=\"title has-text-centered\" title=\"", "\"");
                if (Directory.Exists("Downloads\\" + AlbumName)) continue;
                Directory.CreateDirectory("Downloads\\" + AlbumName);
                AlbumPics.Clear();
                Downloaded = 0;
                foreach (Match match in regex.Matches(inputData))
                {
                    if (match.Success)
                    {
                        string MatchParse = Substring(match.ToString(), "<a class=\"image\" href=\"", "\"");
                        AlbumPics.Add(MatchParse);
                    }
                } //get all links
                ThreadPool.SetMaxThreads(5, 5);
                ThreadPool.SetMinThreads(5, 5);
                Parallel.ForEach(AlbumPics, (string PicLink) =>
                {
                    string FileName = "";
                    string FileExtention = PicLink.Substring(PicLink.Length - 3);
                    if (PicLink.Contains("cyberdrop.nl"))
                    {
                        FileName = Substring(PicLink, "https://f.cyberdrop.nl/", FileExtention);
                    }
                    else
                    {
                        FileName = Substring(PicLink, "https://f.cyberdrop.cc/", FileExtention);
                    }
                    while (true)
                    {
                        try
                        {
                            using (WebClient webClient = new MyWebClient())
                            {
                                webClient.DownloadFile(PicLink, "Downloads\\"+AlbumName + "\\" + FileName + FileExtention);
                                Downloaded++;
                                Console.WriteLine("Downloaded: "+AlbumName+" | "+ FileName + FileExtention);
                            }
                            break;
                        }
                        catch
                        {
                        }
                    }

                });
            }
            Console.WriteLine("Done");
            Console.ReadLine();
        }
    }
}
