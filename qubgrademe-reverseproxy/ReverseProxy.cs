using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http;
using System.Collections.Generic;
using System.Net.Http.Json;
using qubgrademe_totalmarks.Data;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace qubgrademe_reverseproxy
{
    public class ReverseProxy
    {
        private readonly MemoryCache _endpointCache;
        private readonly HttpClient _client;

        public ReverseProxy(MemoryCache endpointCache, HttpClient client)
        {
            _endpointCache = endpointCache;
            _client = client;
        }

        [FunctionName("reverse-proxy")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "put" ,"post", Route = "v1/proxy-service/{service}/{id?}")] HttpRequest req, string service,int? id,
            ILogger log)
        {
            log.LogInformation($"C# HTTP trigger function processed for service {service}" + (id.HasValue ? $" with ID {id}" : ""));
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = new List<object>();
            if (!string.IsNullOrEmpty(requestBody))
            {
                string errorResponse = string.Empty;

                data.AddRange(JsonConvert.DeserializeObject<List<object>>(requestBody, new JsonSerializerSettings
                {
                    Error = delegate (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
                    {
                        errorResponse = args.ErrorContext.Error.Message;
                        args.ErrorContext.Handled = true;
                    }
                }));
                if (!string.IsNullOrEmpty(errorResponse)) return new BadRequestObjectResult(errorResponse);
            }
            try
            {
                if (_endpointCache.GetKeys().Count == 0)
                {
                    var cacheFunction = new UpdateCache(_endpointCache);
                    await cacheFunction.RunV2(req, log);
                }
                var cachedEndpoint = _endpointCache.Get(service);

                HttpResponseMessage response = new HttpResponseMessage();
                List<Module> getResult = new List<Module>();
                string endpointWithParams = (string)cachedEndpoint + (id.HasValue ? $"{id.Value}/" : "") + (req.QueryString.HasValue ? req.QueryString.Value : "");
                var method = req.Method.ToUpper();

                if (method == "GET")
                {
                    response = await _client.GetAsync(endpointWithParams);
                }
                else if (method == "PUT")
                {
                    response = await _client.PutAsJsonAsync(endpointWithParams, data);
                }
                else if (method == "POST")
                {
                    response = await _client.PostAsJsonAsync(endpointWithParams, data);
                }

                response.Dispose();
                if (response.IsSuccessStatusCode) return new OkObjectResult(await response.Content.ReadAsStringAsync());
                else return new BadRequestObjectResult($"Endpoint {service} is not active");
            }
            catch
            {
                return new StatusCodeResult(500);
            }
        }
    }

    public class StudentModuleDTO
    {
        public string ModuleName { get; set; }
        public string Mark { get; set; }
    }
}
