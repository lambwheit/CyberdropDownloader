using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
                Console.Title = "Downloading: " + AlbumName + " | Pic: " + Downloaded + "/" + AlbumPics.Count;
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

        private static void Main()
        {
            int threads = 5;

            Console.Write("Threads(Default 5): ");
            try
            {
                threads = int.Parse(Console.ReadLine());
                if (threads <= 0)
                {
                    threads = 5;
                }
            }
            catch
            {

            }
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
            if (lines?.Length == 0)
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
                    catch (Exception e)
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
                string inputData = htmlCode;
                AlbumName = Substring(htmlCode, "<h1 id=\"title\" class=\"title has-text-centered\" title=\"", "\"");
                AlbumName = string.Concat(AlbumName.Split(Path.GetInvalidFileNameChars()));
                if (Directory.Exists("Downloads\\" + AlbumName)) continue;
                Directory.CreateDirectory("Downloads\\" + AlbumName);
                AlbumPics?.Clear();
                Downloaded = 0;
                //LINK FIX
                string json = Substring(htmlCode, "dynamicEl: ", "})");
                JArray o = JArray.Parse(json);
                foreach (var VARIABLE in o)
                {
                    AlbumPics.Add(VARIABLE["downloadUrl"].ToString());

                }

                ThreadPool.SetMaxThreads(threads, threads);
                ThreadPool.SetMinThreads(threads, threads);

                Parallel.ForEach(AlbumPics, (string PicLink) =>
                {

                    var url = new Uri(PicLink);
                    var filename = url.LocalPath;
                    //FAST LINK CURRENTLY BROKEN
                    //var fastPicLink = "https://"+Substring(PicLink, "https://", "/")+"/s"+filename;
                    while (true)
                    {
                        try
                        {
                            using (WebClient webClient = new MyWebClient())
                            {
                                webClient.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/118.0");
                                webClient.Headers.Add("Host", "fs-02.cyberdrop.me");
                                webClient.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
                                webClient.DownloadFile(PicLink, "Downloads\\" + AlbumName + "\\" + filename);
                                Downloaded++;
                                Console.WriteLine("Downloaded: " + AlbumName + " | " + filename);
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
