using System.Numerics;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace MVCDynamicFormsUltra.Controllers
{
    public class RedisConnectController : Controller
    {
        RedisManager Redis;
        public RedisConnectController(RedisManager _redis)
        {
            Redis = _redis;
        }
        public async Task<IActionResult> SetPostLikeCount(string Author, string messageId, BigInteger Currlikecount)
        {
            string? userId = HttpContext.Session.GetString("UserName");
            if (userId == null)
            {
                return RedirectToAction("Error", new { ErrMsg = "OOPS !! Session Expired" });

            }

            var db = RedisManager.GetDatabase();



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
            var db = RedisManager.GetDatabase();

            string StreamKey = $"message:{messageId}:likecount";
            string setkey = $"message:{messageId}:like";

            if (!db.SetContains(setkey, userId))
            {

                if (Currlikecount > 10000)
                {
                    await db.HyperLogLogAddAsync(StreamKey, userId);
                    await db.SetAddAsync(setkey, userId);

                }
                else
                {
                    await db.StreamAddAsync(StreamKey, new NameValueEntry[] {
                            new NameValueEntry("userId",userId),
                            new NameValueEntry("timestamp",DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()),
                    });
                }
            }

        }
        internal async Task SetViralLikePost(string userId, string messageId, BigInteger Currlikecount)
        {


            string consumergroup = "ViralPostLike";
            string StreamKey = $"{messageId}:likecount";
            var db = RedisManager.GetDatabase();

            try
            {
                await db.StreamCreateConsumerGroupAsync(StreamKey, consumergroup, "0-0");
            }
            catch (RedisServerException e) when (e.Message.Contains("BUSY"))
            {
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
    }
}
