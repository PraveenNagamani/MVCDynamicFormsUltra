using System.Numerics;
using Microsoft.AspNetCore.Mvc;
using MVCDynamicFormsUltra.Models;
using StackExchange.Redis;

namespace MVCDynamicFormsUltra.Controllers
{
    public class RedisConnectController : Controller
    {
        RedisManager Redis;
        IDatabase db;
        private readonly ILogger _logger;
        public RedisConnectController(RedisManager _redis, ILogger<RedisConnectController> logger)
        {
            Redis = _redis;
            db = RedisManager.GetDatabase();
            _logger = logger;
        }
        public async Task<IActionResult> SetPostLikeCount(string Author, string messageId, BigInteger Currlikecount)
        {
            string? userId = HttpContext.Session.GetString("UserName");
            if (userId == null)
            {
                return RedirectToAction("Error", new { ErrMsg = "OOPS !! Session Expired" });

            }

            //var db = RedisManager.GetDatabase();



            if (Currlikecount > 10000)
            {
                SetViralLikePost(userId, messageId, Currlikecount);
            }
            else
            {
                AddPostLike(messageId, userId, Currlikecount);
            }
            await Task.Delay(0);
            // wont add get like count in real time
            return PartialView();

        }

        internal async Task AddPostLike(string messageId, string userId, BigInteger Currlikecount)
        {
            //var db = RedisManager.GetDatabase();

            string StreamKey = $"message:{messageId}:likecount";
            string HyperlogLikeKey = $"message:{messageId}:approxlikecount";
            string setkey = $"message:{messageId}:like";

            if (!db.SetContains(setkey, userId))
            {

                if (Currlikecount > 10000)
                {
                    await db.StreamAddAsync(StreamKey, new NameValueEntry[]
                    {
                            new NameValueEntry("userId",userId),
                            new NameValueEntry("timestamp",DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()),
                    });

                }
                else
                {
                    await db.HyperLogLogAddAsync(HyperlogLikeKey, userId);
                    await db.SetAddAsync(setkey, userId);

                }
            }

        }
        internal async Task SetViralLikePost(string userId, string messageId, BigInteger Currlikecount)
        {


            string consumergroup = "ViralPostLike";
            string StreamKey = $"{messageId}:likecount";
            //var db = RedisManager.GetDatabase();

            try
            {
                await db.StreamCreateConsumerGroupAsync(StreamKey, consumergroup, "0-0");
            }
            catch (RedisServerException e) when (e.Message.Contains("BUSY"))
            {
                _logger.LogCritical("Consumer group already exists. Skipping creation.");
                Console.WriteLine("Consumer group already exists. Skipping creation.");
            }
            int Count = 0;
            while (Count < 5)
            {

                var entries = await db.StreamReadGroupAsync(StreamKey, consumergroup, userId, count: 100);
                Count++;
                if (entries.Length == 0)
                {
                    // No new entries, so we wait before the next attempt to reduce CPU usage

                    if (Count == 1) await AddPostLike(messageId, userId, Currlikecount);
                    await Task.Delay(1000);

                    continue;
                }

                foreach (var entry in entries)
                {
                    if (entry["userId"] == userId)
                    {
                        try
                        {
                            await AddPostLike(messageId, userId, 0);
                            await db.StreamAcknowledgeAsync(StreamKey, "ViralPostLike", entry.Id);

                        }
                        catch (RedisException e)
                        {
                            Console.WriteLine("Failed to store stream data : " + e.Message);
                        }
                        break;
                    }
                }

            }

        }

        public async Task CreateUser(List<Users> users)
        {


            var Tasks = new List<Task>();
            foreach (var user in users)
            {
                Tasks.Add(CheckandAddUser(user.userId, user.UserName, user.Email));
            }

            await Task.WhenAll(Tasks);

        }

        public async Task CheckandAddUser(string userId, string username, string email)
        {

            bool isexists = await db.HashExistsAsync($"user:{userId}", username);
            if (isexists)
            {
                await db.HashSetAsync($"user:{userId}", new HashEntry[]
                {
                new HashEntry("username", username),
                new HashEntry("email", email)
                });
            }

        }

        public async Task SaveTrends(List<Tweet> TrendingPosts)
        {
            //var transaction = db.CreateTransaction();

            var Tasks = new List<Task>();
            foreach (var post in TrendingPosts)
            {
                Tasks.Add(AddPost(post.author, post.Title, post.Content, post.LikeCount));
                Tasks.Add(AddPostToTrend(post, "Trending:Topics"));
            }

            await Task.WhenAll(Tasks);

        }

        private async Task AddPostToTrend(Tweet post, string TrendKey)
        {
            long? msgrank = await db.SortedSetRankAsync(TrendKey, post.Title);
            if (msgrank == null)
            {

                await db.SortedSetAddAsync(TrendKey, post.Title, (double)post.LikeCount);
            }
        }
        public async Task AddPost(string userId, string title, string Content, BigInteger likecount)
        {
            Guid guid = Guid.NewGuid();
            string MessageId = $"MessageId{guid.ToString()}{userId}";
            string userMessagesKey = $"user:{userId}:messages_sorted";

            long? msgrank; double score;

            string datetime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            DateTimeOffset dateTimeOffset = DateTimeOffset.Parse(datetime);
            score = dateTimeOffset.ToUnixTimeSeconds();

            msgrank = await db.SortedSetRankAsync(userMessagesKey, MessageId);

            if (msgrank == null)
            {
                await db.HashSetAsync($"message:{MessageId}", new HashEntry[]
                {
                        new HashEntry("user", userId),
                        new HashEntry("Content",Content),
                        new HashEntry("title", title.Replace("#",string.Empty)),
                        new HashEntry("likecount",likecount.ToString())
                });
                db.SetAddAsync($"Topic:{title}", MessageId);
                db.SortedSetAddAsync(userMessagesKey, MessageId, score);
                SetPostLikeCount(userId, MessageId, likecount);
            }

        }


    }

    public class Users
    {
        public required string userId { get; set; }
        public required string UserName { get; set; }
        public string? Email { get; set; }


    }
}
