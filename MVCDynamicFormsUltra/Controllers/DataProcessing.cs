using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using StackExchange.Redis;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace MVCDynamicFormsUltra.Controllers
{
    public class DataProcessing : Controller
    {
        private readonly IConnectionMultiplexer _Redis;
        private readonly ILogger _logger;
        public DataProcessing(IConnectionMultiplexer Redis, ILogger<DataProcessing> logger)
        {
            _Redis = Redis;
            _logger = logger;
        }


        public async Task Preparation()
        {
            List<string> tlist = new List<string>();
            tlist.Add("Modi"); tlist.Add("Dhoni"); tlist.Add("US Election"); tlist.Add("Work Life Balance"); tlist.Add("HYDRAA"); tlist.Add("Himanchal Floods");
            tlist.Add("SRK");

            IDatabase db = _Redis.GetDatabase();

            // 2. Loop to insert 100,000 users
            for (int a = 1; a <= 100000; a++)
            {
                // Simulate user data
                string userId = $"userid{a}";
                string username = $"TestUser{a}";
                string email = $"user{a}@example.com";

                // 3. Create a hash entry for each user in Redis
                await db.HashSetAsync($"user:{userId}", new HashEntry[]
                {
                new HashEntry("username", username),
                new HashEntry("email", email)
                });

                // Print progress for every 10,000 users
                if (a % 10000 == 0)
                {
                    Console.WriteLine($"Inserted {a} users into Redis.");
                }

                for (int i = 1; i <= 2; i++)
                {
                    // Simulate user data
                    Guid guid = Guid.NewGuid();
                    string MessageId = $"MessageId{guid.ToString()}";
                    Random random = new Random();
                    int randomIndex = random.Next(tlist.Count);

                    string tagid = $"#{tlist[randomIndex]}";
                    string Content = $"hello {tagid.Replace(" ",string.Empty)} from {userId} with ref no {MessageId}";


                    // 3. Create a hash entry for each user in Redis
                    await db.HashSetAsync($"message:{MessageId}", new HashEntry[]
                     {
                    new HashEntry("user", userId),
                    new HashEntry("Content",Content),
                    new HashEntry("title", tagid.Replace("#",string.Empty)),
                    new HashEntry("likecount",0)
                     });

                    string userMessagesKey = $"user:{userId}:messages_sorted";
                    string datetime;
                    if (i % 2 == 0)
                    {
                        datetime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                    }
                    else
                    {
                        
                        datetime = "28-09-2024 15:37:22";
                    }
                    DateTimeOffset dateTimeOffset = DateTimeOffset.Parse(datetime);
                    double score = dateTimeOffset.ToUnixTimeSeconds();

                    bool isAdded = await db.SortedSetAddAsync(userMessagesKey, MessageId,score);
                    if (!isAdded) { _logger.LogCritical(userMessagesKey + " not mapped with " + MessageId, null); }
                }

            }

            Console.WriteLine("Successfully inserted users and messages into Redis.");
            
        }



        public async Task setuser()
        {
            IDatabase db = _Redis.GetDatabase();
            var server =  _Redis.GetServer("127.0.0.1", 6379);

            IEnumerable<RedisKey> keys = server.Keys( pattern: "message:*");
            List<string> keylist = (keys.Select(key => (string)key)).ToList();

            for (int i = 1; i <= keylist.Count(); i++)
            {
                // Simulate user data
                string datetime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                string backdatetime = "28/09/2024 15:37:22";
                var titlekey = await db.HashGetAsync((RedisKey)keylist[i], "title");
                var contents = await db.HashGetAsync((RedisKey)keylist[i], "Content");
                string replacedtitle = titlekey.ToString().Replace(" ",string.Empty);
                if(i%2 == 0)
                {
                    await db.HashSetAsync(keylist[i], new HashEntry[] {
                        new HashEntry ("Time",datetime),
                        new HashEntry ("Content",contents.ToString().Replace(titlekey.ToString(),replacedtitle)),
                        new HashEntry("title",titlekey.ToString().Replace("#",string.Empty))
                    });
                }
                else
                {
                    await db.HashSetAsync(keylist[i], new HashEntry[] {
                        new HashEntry ("Time",backdatetime),
                         new HashEntry ("Content",contents.ToString().Replace(titlekey.ToString(),replacedtitle)),
                        new HashEntry("title",titlekey.ToString().Replace("#",string.Empty))
                    });
                }


                // Print progress for every 10,000 users
                if (i % 10000 == 0)
                {
                    Console.WriteLine($"Inserted {i} message into Redis.");
                }
            }

            Console.WriteLine("Successfully updated 200000 message into Redis.");
        }

        public async Task converttosortedsets()
        {
            var db = _Redis.GetDatabase();
            string setKey = "user:{userId}:messages";          // Original set key
            string sortedSetKey = "user:{userId}:messages_sorted";  // New sorted set key

            // Retrieve all members from the set
            var messageIds = db.SetMembers(setKey);

            // Add each member to the sorted set with a score (e.g., based on timestamp)
            foreach (var messageId in messageIds)
            {
                // For simplicity, assign the current Unix timestamp as the score
                double score = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                // Add the message to the sorted set with the score
                db.SortedSetAdd(sortedSetKey, messageId, score);
            }
        }


    }
}
