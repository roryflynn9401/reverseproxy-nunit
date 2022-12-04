using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace qubgrademe_reverseproxy
{
    public class UpdateCache
    {
        private readonly MemoryCache _endpointCache;

        public UpdateCache(MemoryCache endpointCache)
        {
            _endpointCache = endpointCache;
        }

        [FunctionName("UpdateCache")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "v1/EndpointCache")] HttpRequest req, ILogger log)
        {
            try
            {
                string fileStream = string.Empty;
                using (var sr = new StreamReader("./endpoints.json", new FileStreamOptions { Mode = FileMode.OpenOrCreate}))
                {
                    fileStream = await sr.ReadToEndAsync();
                    sr.Dispose();
                    sr.Close();
                }

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                string[] errorResponse = new string[2];

                var newEndpoints = JsonConvert.DeserializeObject<List<Endpoint>>(requestBody, new JsonSerializerSettings
                {
                    Error = delegate (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
                    {
                        errorResponse[0] = args.ErrorContext.Error.Message;
                        args.ErrorContext.Handled = true;
                    }
                });

                var fileEndpoints = JsonConvert.DeserializeObject<List<Endpoint>>(fileStream, new JsonSerializerSettings
                {
                    Error = delegate (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
                    {
                        errorResponse[1] = args.ErrorContext.Error.Message;
                        args.ErrorContext.Handled = true;
                    }
                });

                if(fileEndpoints == null) fileEndpoints = new List<Endpoint>();
                if (newEndpoints != null)
                {
                    var newAdds = newEndpoints.Where(x => !fileEndpoints.Select(y => y.Name).Contains(x.Name)).ToList();

                    foreach (var endpoint in newEndpoints.Except(newAdds))
                    {
                        var needsUpdated = fileEndpoints.Where(x => x.Name == endpoint.Name && x.Url != endpoint.Url);
                        if (needsUpdated.Any())
                        {
                            needsUpdated.FirstOrDefault().Url = endpoint.Url;
                        }
                    }

                    fileEndpoints.AddRange(newAdds);
                }

                foreach (var entry in fileEndpoints)
                {
                    try
                    {
                        var endpoint = _endpointCache.Get(entry.Name);
                        if ((string)endpoint == entry.Url) continue;
                        else _endpointCache.Set(entry.Name, entry.Url);
                    }
                    catch
                    {
                        return new BadRequestObjectResult("Error getting endpoint");
                    }
                }

                var json = JsonConvert.SerializeObject(fileEndpoints);

                //if(File.Exists("endpoints.json")) File.Delete("endpoints.json");
                File.WriteAllText("endpoints.json", json);

                if (!string.IsNullOrEmpty(errorResponse[0]) || !string.IsNullOrEmpty(errorResponse[1])) return new BadRequestObjectResult(errorResponse);
            }
            catch
            {
                return new BadRequestObjectResult("Error reading endpoints.json file");
            }

            return new OkObjectResult(GetCacheList());
            
        }

        [FunctionName("GetCache")]
        public async Task<IActionResult> RunV2([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/EndpointCache")] HttpRequest req, ILogger log)
        {
            if(_endpointCache.GetKeys().Count == 0) await Run(req, log);
            return new OkObjectResult(GetCacheList());
        }

        [FunctionName("DeleteFromCache")]
        public async Task<IActionResult> RunV3(
           [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "v1/EndpointCache")] HttpRequest req, ILogger log)
        {
            var endpoints = new List<Endpoint>();
            string fileStream = string.Empty;
            string errorResponse = string.Empty;
            try
            {
                using(var sr = new StreamReader("endpoints.json"))
                {
                    fileStream = await sr.ReadToEndAsync();
                    sr.Dispose();
                    sr.Close();
                }
                
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                endpoints = JsonConvert.DeserializeObject<List<Endpoint>>(requestBody, new JsonSerializerSettings
                {
                    Error = delegate (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
                    {
                        errorResponse = args.ErrorContext.Error.Message;
                        args.ErrorContext.Handled = true;
                    }
                });
            }
            catch
            {
                return new BadRequestObjectResult("Error reading endpoints.json file" + (string.IsNullOrEmpty(errorResponse) ? "" : errorResponse));
            }

            foreach (var entry in endpoints)
            {
                try
                {
                    _endpointCache.Remove(entry.Name);
                    var fileEndpoints = JsonConvert.DeserializeObject<List<Endpoint>>(fileStream, new JsonSerializerSettings
                    {
                        Error = delegate (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
                        {
                            errorResponse = args.ErrorContext.Error.Message;
                            args.ErrorContext.Handled = true;
                        }
                    });
                    fileEndpoints.RemoveAll(x => x.Name == entry.Name);
                    var json = JsonConvert.SerializeObject(fileEndpoints);
                    File.WriteAllText("endpoint.json", json);
                }
                catch
                {
                    return new BadRequestObjectResult(entry.Name);
                }
            }

            if (!string.IsNullOrEmpty(errorResponse)) return new BadRequestResult();

            return new OkObjectResult(GetCacheList());
        }

        public List<object> GetCacheList()
        {
            var vals = _endpointCache.GetKeys().Cast<object>().ToList();
            var returnVals = new List<object>();
            foreach (var val in vals)
            {
                returnVals.Add(new { Name = val, Url = _endpointCache.Get(val) });
            }
            return returnVals;
        }
    }
    public class Endpoint
    {
        public string Name { get; set; }
        public string Url { get; set; }
    }
}
