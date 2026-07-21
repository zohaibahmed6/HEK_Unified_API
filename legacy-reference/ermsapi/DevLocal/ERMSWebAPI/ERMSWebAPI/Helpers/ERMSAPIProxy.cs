using Logger;
using System;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ERMSWebAPI.Helpers
{
    public static class ERMSAPIProxy
    {
        public static async Task<HttpResponseMessage> ForwardtoERMSAzureAPIPOST(HttpRequestMessage originalRequest,string targetEndpoint)
        {
            string baseUrl = ConfigurationManager.AppSettings["AzureEMRSAPI"] ?? string.Empty;
            targetEndpoint = targetEndpoint ?? string.Empty;
            string finalUrl = $"{baseUrl.TrimEnd('/')}/{targetEndpoint.TrimStart('/')}";
           
            Logging.Instance.WriteEventLog($"ForwardtoERMSAzureAPIPOST Final URL request: {finalUrl}", TypeEnums.LogType.Default);

            try
            {
                string forwardtoERMSAPIPostLog =
                    $"Method: {originalRequest.Method}\n" +
                    $"URL: {originalRequest.RequestUri}\n" +
                    $"Authorization: {originalRequest.Headers.Authorization}\n" +
                    $"Body:\n{(originalRequest.Content == null ? "" : await originalRequest.Content.ReadAsStringAsync())}";

                Logging.Instance.WriteEventLog($"ForwardtoERMSAzureAPIPOST request: {forwardtoERMSAPIPostLog}", TypeEnums.LogType.Default);

                using (var client = new HttpClient())
                using (var request = new HttpRequestMessage(originalRequest.Method, finalUrl))
                {
                    string originalContent = await originalRequest.Content.ReadAsStringAsync();

                    request.Content = new StringContent(originalContent, Encoding.UTF8, "application/xml");
                    
                    if (!targetEndpoint.ToLower().Contains("authenticate"))
                    {
                        if (originalRequest.Headers.Authorization != null)
                        {
                            request.Headers.Authorization = originalRequest.Headers.Authorization;
                        }
                    }
                   

                    HttpResponseMessage response = await client.SendAsync(request);
                    response.EnsureSuccessStatusCode();
                    if (response.IsSuccessStatusCode)
                        return response;

                    string errorBody = await response.Content.ReadAsStringAsync();
                    string errorMsg = $"Error response: {(int)response.StatusCode} {response.ReasonPhrase}. {errorBody}";
                    Logging.Instance.WriteEventLog($"ForwardtoERMSAzureAPIPOST:  {errorMsg}" , TypeEnums.LogType.Default);

                    return SetToXmlERMSAPIProxy(string.Empty, errorMsg);

                }
            }
           
            catch (Exception ex)
            {
                Logging.Instance.WriteExceptionLog($"Unexpected error in ForwardToERMSAzureApiPost: {ex.Message}", ex);
                return SetToXmlERMSAPIProxy(string.Empty, ex.Message);
            }
        }
        public static async Task<HttpResponseMessage> ForwardtoERMSAzureAPIGET(HttpRequestMessage originalRequest, string targetEndpoint)
        {
            if (originalRequest.Method != HttpMethod.Get)
            {
                return SetToXmlERMSAPIProxy(string.Empty, "Only GET requests are supported.");
            }

            string baseUrl = ConfigurationManager.AppSettings["AzureEMRSAPI"];
            string finalUrl = $"{baseUrl.TrimEnd('/')}/{targetEndpoint.TrimStart('/')}";

            var originalQuery = originalRequest.RequestUri.Query;
            if (!string.IsNullOrWhiteSpace(originalQuery))
            {
                finalUrl += originalQuery.StartsWith("?") ? originalQuery : "?" + originalQuery;
            }

            Logging.Instance.WriteEventLog($"ForwardtoERMSAzureAPIGET: Final URL request: {finalUrl}", TypeEnums.LogType.Default);

            try
            {

                string forwardtoERMSAPIGetLog =
                    $"Method: {originalRequest.Method}\n" +
                    $"URL: {originalRequest.RequestUri}\n" +
                    $"Authorization: {originalRequest.Headers.Authorization}\n";


                Logging.Instance.WriteEventLog($"ForwardtoERMSAzureAPIGET: Incoming Request Logs: {forwardtoERMSAPIGetLog}", TypeEnums.LogType.Default);

                using (var client = new HttpClient())
                using (var request = new HttpRequestMessage(originalRequest.Method, finalUrl))
                {
                    if (originalRequest.Headers.Authorization != null)
                    {
                        request.Headers.Authorization = originalRequest.Headers.Authorization;
                    }

                    var response = await client.SendAsync(request);
                    response.EnsureSuccessStatusCode();
                    if (response.IsSuccessStatusCode)
                        return response;

                    string errorBody = await response.Content.ReadAsStringAsync();
                    string errorMsg = $"Error message: {(int)response.StatusCode} {response.ReasonPhrase}. {errorBody}";
                    Logging.Instance.WriteEventLog($"ForwardtoERMSAzureAPIGET:  {errorMsg}", TypeEnums.LogType.Default);

                    return SetToXmlERMSAPIProxy(string.Empty, errorMsg);
                }

            }
            catch (Exception ex)
            {
                Logging.Instance.WriteExceptionLog($"Unexpected error in ForwardtoERMSAzureAPIGET: {ex.Message}", ex);
                return SetToXmlERMSAPIProxy(string.Empty, ex.Message);
            }
        }
        private static HttpResponseMessage SetToXmlERMSAPIProxy(string xmlString, string error)
        {
            if (!string.IsNullOrWhiteSpace(error))
                xmlString = "<?xml version=\"1.0\" encoding=\"utf-16\"?>" +
                            "<Error><Message>" + error + "</Message></Error>";

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(xmlString, Encoding.UTF8, "application/xml");

            return response;
        }

    }
}
