using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace MVCDynamicFormsUltra.Controllers
{
    public class RedisConnectController : Controller
    {
        IConnectionMultiplexer Redis;
        public RedisConnectController(IConnectionMultiplexer _redis) {
            Redis = _redis;
        }
        public async Task SetPostLikeCount(string userId, string messageId)
        {
            await Task.Delay(1000);
            var db = Redis.GetDatabase();

            string likesKey = $"{messageId}:likes";     // Set key for users who liked the message
            string likeCountKey = "messages:likecount"; // Hash to store like counts for each message
            
            // use pub sub , see reference fro chat gpt 03/10/2024

        }
    }
}
