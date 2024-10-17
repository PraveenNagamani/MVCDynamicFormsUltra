using System.Net.Http.Headers;

public class TrendingTopics
{
    private readonly RedisManager _Redis;
    public TrendingTopics(RedisManager Redis)
    {
        _Redis = Redis;
    }

    public async Task SetTrendingTopics()
    {
        string clientId = "7c0g6BPlKYPQy_NFOLAYgA";
        string clientSecret = "PVkwmFewOV24pMySn0RWdlXGJ-08pg";
        string username = "praveen_nagamani";
        string password = "pra";
        string userAgent = "RedisTrends/1.0";
        // http://localhost:8080

        using (HttpClient client = new HttpClient())
        {
            // Step 1: Authenticate and get the access token
            var requestData = new MultipartFormDataContent
                {
                    { new StringContent("password"), "grant_type" },
                    { new StringContent(username), "username" },
                    { new StringContent(password), "password" }
                };

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{clientId}:{clientSecret}"))
            );

            HttpResponseMessage tokenResponse = await client.PostAsync("https://www.reddit.com/api/v1/access_token", requestData);
            string tokenResponseBody = await tokenResponse.Content.ReadAsStringAsync();

            // Extract the access token from the response (token parsing logic omitted for brevity)
            string accessToken = "your_access_token"; // Replace with token extraction logic

            // Step 2: Use the access token to fetch trending topics
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            client.DefaultRequestHeaders.Add("User-Agent", userAgent);

            HttpResponseMessage response = await client.GetAsync("https://oauth.reddit.com/r/popular/top?limit=10");
            string responseBody = await response.Content.ReadAsStringAsync();

            Console.WriteLine("Trending Topics: " + responseBody);
        }
    }

    public async Task IncrementTopicScoreAsync(string topic)
    {
        var db = RedisManager.GetDatabase();
        await db.SortedSetIncrementAsync("trending:topics", topic, 1);
    }

    // Method to apply time-decay to all trending topics
    public async Task ApplyDecayFactorAsync(double decayFactor)
    {
        var db = RedisManager.GetDatabase();
        var trendingTopics = await db.SortedSetRangeByRankWithScoresAsync("trending:topics", 0, -1);

        foreach (var entry in trendingTopics)
        {
            string topic = entry.Element;
            double newScore = entry.Score * decayFactor;
            await db.SortedSetAddAsync("trending:topics", topic, newScore);
        }

        Console.WriteLine("Decay factor applied successfully to trending topics.");
    }
}
