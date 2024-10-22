using System.Net.Http.Headers;
using Microsoft.AspNetCore.Identity;
using MVCDynamicFormsUltra.Controllers;
using MVCDynamicFormsUltra.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;

public class TrendingTopics : BackgroundService
{
    private readonly RedisManager _Redis;
    private readonly IDatabase db;
    private readonly RedisConnectController _RControl;
    private readonly ILogger _logger;
    private readonly PasswordHasher<string> passwordHasher = new PasswordHasher<string>();
    public TrendingTopics(RedisManager Redis, RedisConnectController r, ILogger<TrendingTopics> logger)
    {
        db = RedisManager.GetDatabase();
        _Redis = Redis;
        _RControl = r;
        _logger = logger;
    }

    public async Task SetTrendingTopics()
    {
        
        string clientId = "7c0g6BPlKYPQy_NFOLAYgA";
        string clientSecret = "";
        string username = "praveen_nagamani";
        string enteredpassword = "";
        try
        {
            string? encryptedpassword = db.HashGet($"Redditencryptedpassword{username}", "encryptedpassword");
            HashEntry[] entries = db.HashGetAll($"RedditpasswordkeyIV{username}");
            enteredpassword = AesEncryptionHelper.DecryptStringFromBytes_Aes(Convert.FromBase64String(encryptedpassword), Convert.FromBase64String(entries[0].Value), Convert.FromBase64String(entries[1].Value));

        }
        catch (Exception ex)
        {
            Console.WriteLine("Error while decrypting" + ex.Message); return;
        }


        string userAgent = "RedisTrends/1.0 (http://localhost:8080 praveenkumr97@gmail.com)";
        // http://localhost:8080

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);


            // Step 1: Authenticate and get the access token
            var requestData = new MultipartFormDataContent
                {
                    { new StringContent("password"), "grant_type" },
                    { new StringContent(username), "username" },
                    { new StringContent(enteredpassword), "password" }
                };

            clientSecret = await db.StringGetAsync($"RedditClient:{clientId}");

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{clientId}:{clientSecret}"))
            );

            HttpResponseMessage tokenResponse = await client.PostAsync("https://www.reddit.com/api/v1/access_token", requestData);
            string tokenResponseBody = await tokenResponse.Content.ReadAsStringAsync();
            var tokenData = JObject.Parse(tokenResponseBody);

            // Extract the access token from the response (token parsing logic omitted for brevity)
            string? accessToken = tokenData["access_token"]?.ToString();

            // Step 2: Use the access token to fetch trending topics
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            client.DefaultRequestHeaders.Add("User-Agent", userAgent);

            HttpResponseMessage response = await client.GetAsync("https://oauth.reddit.com/r/popular/top?limit=10");
            string responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Trending Topics: " + responseBody);
            Console.WriteLine("---------------------------------------------------------------");
            if (response.IsSuccessStatusCode)
            {
                var trendingdata = JsonConvert.DeserializeObject<RedditResponse>(responseBody);

                List<Users> users = new List<Users>();
                List<Tweet> tweets = new List<Tweet>();
                foreach (var post in trendingdata.Data.Children)
                {
                    //Console.WriteLine($"Title: {post.Data.title}, Author: {post.Data.author}, Score: {post.Data.score} , Group: {post.Data.subreddit}");
                    users.Add( new Users{ userId = post.Data.author_fullname, UserName = post.Data.author, Email = $"{post.Data.author_fullname}@example.com" });
                    tweets.Add( new Tweet() { author = post.Data.author_fullname, Content = post.Data.title , Title = post.Data.subreddit , LikeCount = post.Data.score  });
                }
                _RControl.CreateUser(users);
                _RControl.SaveTrends(tweets);

                Console.WriteLine("---------------------------------------------------------------");
                
            }

        }
    }

    public async Task IncrementTopicScoreAsync(string topic)
        {
            
            await db.SortedSetIncrementAsync("Trending:Topics", topic, 1);
        }

        // Method to apply time-decay to all trending topics
        public async Task ApplyDecayFactorAsync(double decayFactor)
        {
            
            var trendingTopics = await db.SortedSetRangeByRankWithScoresAsync("Trending:Topics", 0, -1);

            foreach (var entry in trendingTopics)
            {
                string topic = entry.Element;
                double newScore = entry.Score * decayFactor;
                await db.SortedSetAddAsync("Trending:Topics", topic, newScore);
            }
            _logger.LogInformation("Decay factor applied successfully to trending topics.");
            Console.WriteLine("Decay factor applied successfully to trending topics.");
        }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested){
            long CurrentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long OneHourinSec = 3600;

            var lastupdatedvalue = await db.StringGetAsync("Trending:LastUpdated");

            if(!lastupdatedvalue.IsNullOrEmpty){
                long lastupdatedtime = (long) lastupdatedvalue;

                if(CurrentTime - lastupdatedtime < OneHourinSec)
                {
                    long remainingTime = OneHourinSec -(CurrentTime - lastupdatedtime);
                    await Task.Delay(TimeSpan.FromSeconds(remainingTime) );
                }
            }
            
            await ApplyDecayFactorAsync(0.9);

            await SetTrendingTopics();
            await db.StringSetAsync("Trending:LastUpdated",CurrentTime);
            
            await Task.Delay(TimeSpan.FromHours(1),stoppingToken);
        }
    }
}



public class RedditPostData
{
    public required string title { get; set; }
    public string author { get; set; }

    public required string author_fullname { get; set; }
    public int score { get; set; }
    public required string subreddit { get; set; }
    public string permalink { get; set; }
    public int numcomments { get; set; }
    public string url { get; set; }
}

public class RedditPost
{
    public string Kind { get; set; }
    public RedditPostData Data { get; set; }
}

public class RedditResponseData
{
    public List<RedditPost> Children { get; set; }
}

public class RedditResponse
{
    public string Kind { get; set; }
    public RedditResponseData Data { get; set; }
}
