namespace MVCDynamicFormsUltra.Models
{
    public class Tweet
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public string author { get; set; }

        public IFormFile? attachments { get; set; }

        public int LikeCount { get; set; }
        public int retweetcount { get; set; }
    }

   
    
}
