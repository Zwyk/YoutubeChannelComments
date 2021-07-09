using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

using Newtonsoft.Json;

namespace YoutubeChannelComments
{
    class Program
    {
        static string API_KEY;
        static string ChannelId;

        class Output
        {
            public string channelId;

            public int total;
            public int max;
            public double average;
            public double stdev;

            public List<VideoData> results;

            public Output(string channelId, List<VideoData> results)
            {
                this.channelId = channelId;
                this.results = results;
            }
        }

        class VideoData
        {
            public string id;
            public string name;
            public string published;
            public int comments;

            public VideoData(string id, string name, DateTime published, int comments)
            {
                this.id = id;
                this.name = name;
                this.published = published.ToString("yyyy-MM-dd hh:mm:ss");
                this.comments = comments;
            }
        }

        private static async void Work()
        {
            try
            {
                var youtubeService = new YouTubeService(new BaseClientService.Initializer()
                {
                    ApiKey = API_KEY,
                });

                List<VideoData> results = new List<VideoData>();

                var reqChannel = youtubeService.Channels.List("contentDetails");
                reqChannel.Id = ChannelId;
                var resChannel = await reqChannel.ExecuteAsync();
                var uploadsId = resChannel.Items[0].ContentDetails.RelatedPlaylists.Uploads;

                List<string> videoIds = new List<string>();

                print("Requesting videos for channel " + ChannelId);

                string nextPageToken = "";
                while (nextPageToken != null)
                {
                    var reqUploads = youtubeService.PlaylistItems.List("snippet");
                    reqUploads.PlaylistId = uploadsId;
                    reqUploads.MaxResults = 10000;
                    reqUploads.PageToken = nextPageToken;
                    var resUploads = await reqUploads.ExecuteAsync();

                    foreach (var vi in resUploads.Items) videoIds.Add(vi.Snippet.ResourceId.VideoId);

                    nextPageToken = resUploads.NextPageToken;
                }

                print("Videos to request : " + videoIds.Count);

                foreach (string videoId in videoIds)
                {
                    print("Requesting data for " + videoId);

                    var reqVideo = youtubeService.Videos.List("snippet,statistics");
                    reqVideo.Id = videoId;
                    var resVideo = await reqVideo.ExecuteAsync();

                    Video v = resVideo.Items[0];
                    results.Add(new VideoData(videoId, v.Snippet.Title, v.Snippet.PublishedAt.Value, (int)v.Statistics.CommentCount.Value));
                }

                Output output = new Output(ChannelId, results);
                output.total = results.Sum(r => r.comments);
                output.max = results.Max(r => r.comments);
                output.average = results.Average(r => r.comments);
                output.stdev = Math.Sqrt(results.Sum(r => Math.Pow(r.comments - output.average, 2)) / results.Count);

                Directory.CreateDirectory("Results");

                string file = @"Results\Result" + DateTime.Now.ToString(" - yyyyMMdd_HHmmss_fff") + ".json";
                File.WriteAllText(file, JsonConvert.SerializeObject(output, Formatting.Indented));

                print("Finished, results written in : " + file);
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occured : " + e);
            }
        }

        static void Main(string[] args)
        {
            Console.Write("API Key : ");
            API_KEY = Console.ReadLine();

            Console.Write("Channel ID : ");
            ChannelId = Console.ReadLine();

            Work();

            Console.ReadKey();
        }

        static void print(object o, bool nl = true)
        {
            if (nl) Console.WriteLine(o == null ? "null" : o.ToString());
            else Console.Write(o == null ? "null" : o.ToString());
        }
    }
}
