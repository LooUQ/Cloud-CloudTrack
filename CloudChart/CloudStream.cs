using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Loouq.CloudChart
{
    /// <summary>
    /// Processes LooUQ Cloud stream tasks (open stream, get event doc , close stream)
    /// </summary>
    public class CloudStream
    {
        private readonly HttpClient httpClient = new HttpClient();
        private readonly string CloudStreamsBaseUrl;
        private string streamId;
        private string userName;

        public CloudStream(string streamsBaseUrl)
        {
            CloudStreamsBaseUrl = streamsBaseUrl;
            httpClient = new HttpClient
            {
                MaxResponseContentBufferSize = 64000
            };
        }

        public async Task<bool> Open(string streamOptions, string userName, string userPassword)
        {
            Debug.WriteLine("CloudStream Open()");
            this.userName = userName;
            var streamUri = new Uri(CloudStreamsBaseUrl + "/open");
            var options = new StringContent(streamOptions, Encoding.UTF8, "application/json");
            var credentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", userName, userPassword)));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            var response = await httpClient.PostAsync(streamUri, options);
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var openResponseContent = await response.Content.ReadAsStringAsync();
                    var responseJObj = JObject.Parse(openResponseContent);
                    if (responseJObj["streamId"] != null)
                    {
                        streamId = responseJObj["streamId"].ToString();
                        return true;
                    }
                }
                catch (Exception) { }
            }
            streamId = null;
            return false;
        }

        /// <summary>
        /// Gets the next sequence from LooUQ Cloud event stream.  Return sequence/item based on stream open options.
        /// </summary>
        /// <returns>JArray</returns>
        public async Task<JArray> Get()
        {
            Debug.WriteLine("CloudStream Get()");
            if (streamId != null)
            {
                var streamUri = new Uri(CloudStreamsBaseUrl + "/get");
                var bodyJson = JsonConvert.SerializeObject(new
                {
                    streamId,
                    actorName = userName
                });
                var postContent = new StringContent(bodyJson, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(streamUri, postContent);
                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        return JArray.Parse(responseContent);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Exception on stream Get(): {0}", ex.Message);
                    }
                }
            }
            return null;
        }


        public async Task<bool> Close()
        {
            Debug.WriteLine("CloudStream Close()");

            var streamUri = new Uri(CloudStreamsBaseUrl + "/close");
            var postContent = new StringContent(streamId, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(streamUri, postContent);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            return false;
        }
    }
}
