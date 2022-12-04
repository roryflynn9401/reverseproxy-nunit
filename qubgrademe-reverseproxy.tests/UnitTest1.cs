using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using qubgrademe_reverseproxy;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace qubgrademe_reverseproxy_tests;

[TestFixture]
public class Tests
{
    private readonly HttpClient client = new HttpClient();

    public static HttpStatusCode GetHttpStatusCode(IActionResult functionResult)
    {
        try
        {
            return (HttpStatusCode)functionResult
                    .GetType()
                    .GetProperty("StatusCode")
                    .GetValue(functionResult, null);
        }
        catch
        {
            return HttpStatusCode.InternalServerError;
        }
    }

    public static DefaultHttpContext httpContext = new DefaultHttpContext();
    public HttpRequest request = httpContext.Request;
    public const string Url = "qubgrademe-totalmarks.azurewebsites.net";

    [SetUp]
    public void Setup()
    {
        request.Method = "GET";
        request.Scheme = "http";
        request.ContentType = "application/json";
    }

    //Valid Data
    [Test]
    public async Task ProxyTest()
    {
        var uriBuilder = new UriBuilder()
        {
            Scheme = "http",
            Host = Url.ToString(),
            Path = "/api/v1/proxy-service",
        };

        request.Host = new HostString(Url);
        request.Path = uriBuilder.Path;

        var cacheFunction = new ReverseProxy(new MemoryCache(new MemoryCacheOptions()), new HttpClient());
        var result = await cacheFunction.Run(request, "EndpointCache", null ,new LoggerFactory().CreateLogger("tests"));
        var status = GetHttpStatusCode(result);

        if (status == HttpStatusCode.InternalServerError)
        {
            Assert.Fail();
        }
        Assert.Pass();
    }

    //Get Cache
    [Test]
    public async Task GetCacheTest()
    {
        var uriBuilder = new UriBuilder()
        {
            Scheme = "http",
            Host = Url,
            Path = "/api/v1/EndpointCache",
        };

        request.Host = new HostString(Url);
        request.Path = uriBuilder.Path;

        var cacheFunction = new UpdateCache(new MemoryCache(new MemoryCacheOptions()));
        var result = await cacheFunction.Run(request, new LoggerFactory().CreateLogger("tests"));
        var status = GetHttpStatusCode(result);

        if (status == HttpStatusCode.OK)
        {
            Assert.Pass();
        }
        Assert.Fail(); 
    }

    [Test]
    public async Task PutCacheTest()
    {
        var uriBuilder = new UriBuilder()
        {
            Scheme = "http",
            Host = Url,
            Path = "/api/v1/EndpointCache",
        };

        request.Method = "PUT";
        request.Host = new HostString(Url);
        request.Path = uriBuilder.Path;

        var cacheFunction = new UpdateCache(new MemoryCache(new MemoryCacheOptions()));
        var initalCount = cacheFunction.GetCacheList().Count();
        var newEndpoint = JsonConvert.SerializeObject(new Endpoint { Name = "Test", Url = "www.test.com" });

        var sb = new StringBuilder();
        sb.Append("[").Append(newEndpoint).Append("]");
        newEndpoint = sb.ToString();

        ASCIIEncoding encoding = new ASCIIEncoding();
        byte[] body = encoding.GetBytes(newEndpoint);

        Stream bodyStream = new MemoryStream();
        bodyStream.Write(body, 0, body.Length);
        bodyStream.Position = 0;
        request.Body = bodyStream;

        var result = await cacheFunction.Run(request, new LoggerFactory().CreateLogger("tests"));
        var status = GetHttpStatusCode(result);

        bodyStream.Flush();
        bodyStream.Close();

        if (status == HttpStatusCode.OK && 
            result is OkObjectResult okResult)
        {
            var returnValues = JsonConvert.DeserializeObject<List<Endpoint>>(JsonConvert.SerializeObject(okResult.Value));
            if(returnValues.Count == initalCount + 1)
            {
                Assert.Pass();
            }
        }
        Assert.Fail();
    }
}