

namespace IngestTask.Tool
{
    using System.Net.Http.Headers;

    public static class HeaderHelper
    {
        public static void SetHeaderValue(this HttpContentHeaders Headers)
        {
            if (Headers != null)
            {
                Headers.Add("sobeyhive-http-system", "INGESTSERVER");
                Headers.Add("sobeyhive-http-site", "S1");
                Headers.Add("sobeyhive-http-tool", "INGESTSERVER");
                Headers.Add("sobeyhive-http-secret", RSAHelper.RSAstr());
                Headers.Add("current-user-code", "admin");
            }
          
        }
        public static void SetHeaderValue(this HttpRequestHeaders Headers)
        {
            if (Headers != null)
            {
                Headers.Add("sobeyhive-http-system", "INGESTSERVER");
                Headers.Add("sobeyhive-http-site", "S1");
                Headers.Add("sobeyhive-http-tool", "INGESTSERVER");
                Headers.Add("sobeyhive-http-secret", RSAHelper.RSAstr());
                Headers.Add("current-user-code", "admin");
            }
            
        }
        public static void SetHeaderValue(this HttpRequestHeaders Headers, string code)
        {
            if (Headers != null)
            {
                Headers.Add("sobeyhive-http-system", "INGESTSERVER");
                Headers.Add("sobeyhive-http-site", "S1");
                Headers.Add("sobeyhive-http-tool", "INGESTSERVER");
                Headers.Add("sobeyhive-http-secret", RSAHelper.RSAstr());
                Headers.Add("current-user-code", code);
            }
            
        }
    }
}
