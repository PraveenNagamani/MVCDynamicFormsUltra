using System.Numerics;

namespace MVCDynamicFormsUltra.Models
{
    public class Tweet
    {
        public string TweetID { get; set;}
        public string Title { get; set; }
        public string Content { get; set; }
        public string author { get; set; }

        public IFormFile? attachments { get; set; }

        public BigInteger LikeCount { get; set; }
        public BigInteger retweetcount { get; set; }
    }

   
    
}
