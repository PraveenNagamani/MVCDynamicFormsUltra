using System.Net.Http.Headers;

namespace MVCDynamicFormsUltra
{
    public class LaunchAPI
    {

        string url ;
        HttpClient client;
        public LaunchAPI(IConfiguration configuration,HttpClient httpClient)
        {
            client = httpClient;
            url = configuration["API:CoreUrl"] ?? throw new ArgumentNullException("No API URL Configured") ;
        }

        public async Task<string> CallAPI(string endpoint,bool IsGet = true, Dictionary<string,string> queryparams = null, byte[] files = null)
        {
            var uribuilder = new UriBuilder($"{url}/{endpoint}");

            var content = new MultipartFormDataContent();

            try
            {
                if (files != null)
                {

                    var fileContent = new ByteArrayContent(files);
                    fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                    content.Add(fileContent, "file", "file1");

                }

                if (queryparams != null)
                {
                    if (queryparams.Count > 0)
                    {
                        uribuilder.Query = new FormUrlEncodedContent(queryparams).ReadAsStringAsync().Result;
                    }
                }


                HttpResponseMessage response = await client.PostAsync(uribuilder.Uri, content);
                if (!IsGet)
                {
                    response = await client.PostAsync(uribuilder.Uri, content);
                }
                else
                {
                    response = await client.GetAsync(uribuilder.Uri);
                }

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    throw new HttpRequestException("Exception While Calling API " + endpoint, new Exception(), response.StatusCode);
                }
            }
            catch (Exception e)
            {
                return e.Message;
            }



        }
    }
}
