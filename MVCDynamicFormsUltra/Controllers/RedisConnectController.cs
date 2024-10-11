using System.Numerics;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace MVCDynamicFormsUltra.Controllers
{
    public class RedisConnectController : Controller
    {
        IConnectionMultiplexer Redis;
        public RedisConnectController(IConnectionMultiplexer _redis)
        {
            Redis = _redis;
        }
        public async Task SetPostLikeCount(string userId, string messageId, BigInteger Currlikecount)
        {
            
            var db = Redis.GetDatabase();

            string likesKey = $"{messageId}:likecount";     // Set key for users who liked the message

            if (Currlikecount > 10000)
            {
                await db.StreamAddAsync(likesKey,new NameValueEntry[] {
                    new NameValueEntry("userId",userId),
                    new NameValueEntry("timestamp",DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()),
                } );

                SetViralLikePost(userId, messageId, Currlikecount);
            }
            else
            {
                db.HyperLogLogAddAsync(likesKey, userId);

            }

            // use pub sub , see reference fro chat gpt 03/10/2024

        }
        public async Task SetViralLikePost(string userId, string messageId, BigInteger Currlikecount)
        {
            
            string streamkey = messageId + "_Likes";
            string consumergroup = "ViralPostLike";
            string likesKey = $"{messageId}:likecount";
            var db = Redis.GetDatabase();

            try
            {
                await db.StreamCreateConsumerGroupAsync(streamkey, consumergroup, "0-0");
            }
            catch (RedisServerException e) when (e.Message.Contains("BUSY"))
            {
                Console.WriteLine("Consumer group already exists. Skipping creation.");
            }

            while (true){

                var entries = await db.StreamReadGroupAsync(streamkey,consumergroup,userId,count: 100);

                if(entries.Length ==0){
                    // No new entries, so we wait before the next attempt to reduce CPU usage
                    await Task.Delay(1000);
                    continue;
                }

                foreach(var entry in entries){
                    string streamuserid = entry["userId"];

                    await db.HyperLogLogAddAsync(likesKey, userId);

                    db.StreamAcknowledgeAsync(streamkey,consumergroup,entry.Id);
                }

            }

        }
    }
}
