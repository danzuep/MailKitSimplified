//using System.Text;
//using System.Net.Mime;
//using System.Net.Http.Headers;
//using Microsoft.Extensions.Options;
//using Microsoft.Extensions.Caching.Memory;

//namespace WebApiExample.Services
//{
//    public interface ICiscoControlCenterApiClientFactory
//    {
//        ICiscoControlCenterApiClient GetClient(string name);
//    }

//    public class CiscoControlCenterApiClientFactory : ICiscoControlCenterApiClientFactory
//    {
//        private readonly IHttpClientFactory _httpClientFactory;
//        private readonly IMemoryCache _memoryCache;
//        private readonly IOptionsSnapshot<SimPorterOptions> _cacheOptionsSnapshot;
//        private readonly IOptionsSnapshot<CiscoApiOptions> _ciscoOptionsSnapshot;

//        public CiscoControlCenterApiClientFactory(IHttpClientFactory httpClientFactory, IMemoryCache memoryCache, IOptionsSnapshot<SimPorterOptions> cacheOptionsSnapshot, IOptionsSnapshot<CiscoApiOptions> ciscoOptionsSnapshot)
//        {
//            _httpClientFactory = httpClientFactory;
//            _memoryCache = memoryCache;
//            _cacheOptionsSnapshot = cacheOptionsSnapshot;
//            _ciscoOptionsSnapshot = ciscoOptionsSnapshot;
//        }

//        public ICiscoControlCenterApiClient GetClient(string cellularGroupId)
//        {
//            var cachedValue = _memoryCache.GetOrCreate(cellularGroupId,
//                cacheEntry =>
//                {
//                    if (_cacheOptionsSnapshot != null)
//                    {
//                        cacheEntry.SlidingExpiration = _cacheOptionsSnapshot.Value.SlidingCacheTime;
//                        cacheEntry.AbsoluteExpirationRelativeToNow = _cacheOptionsSnapshot.Value.MaximumCacheTime;
//                    }
//                    var httpClient = CreateHttpClientNamedFromOptions(cellularGroupId);
//                    return new CiscoControlCenterApiClient(httpClient);
//                });
//            return cachedValue;
//        }

//        private HttpClient CreateHttpClientNamedFromOptions(string cellularGroupId)
//        {
//            if (string.IsNullOrWhiteSpace(cellularGroupId))
//            {
//                throw new ArgumentNullException(nameof(cellularGroupId));
//            }
//            if (!int.TryParse(cellularGroupId, out int groupId))
//            {
//                throw new ArgumentOutOfRangeException(nameof(cellularGroupId), $"'{cellularGroupId}' is not a valid cellular group ID");
//            }

//            var clientOptions = _ciscoOptionsSnapshot?.Value.CiscoApiClients;
//            var client = clientOptions?.FirstOrDefault(c => c.CellularGroupId == groupId);
//            if (client == null)
//            {
//                throw new NullReferenceException($"No configuration of type {CiscoApiOptions.SectionName}:{cellularGroupId} was supplied");
//            }

//            var httpClient = CreateHttpClient(client.UserName, client.AccessToken, client.BaseUrl);

//            return httpClient;
//        }

//        private HttpClient CreateHttpClient(string userName, string accessToken, Uri baseUrl)
//        {
//            if (string.IsNullOrWhiteSpace(userName))
//            {
//                throw new ArgumentNullException(nameof(userName));
//            }
//            if (string.IsNullOrWhiteSpace(accessToken))
//            {
//                throw new ArgumentNullException(nameof(accessToken));
//            }
//            if (baseUrl == null || baseUrl == default)
//            {
//                throw new ArgumentNullException(nameof(baseUrl));
//            }

//            var httpClient = _httpClientFactory.CreateClient(nameof(ICiscoControlCenterApiClientFactory));

//            httpClient.BaseAddress = baseUrl;

//            httpClient.DefaultRequestHeaders.Accept.Add(
//                new MediaTypeWithQualityHeaderValue(
//                    MediaTypeNames.Application.Json));

//            httpClient.DefaultRequestHeaders.Authorization =
//                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(
//                    Encoding.ASCII.GetBytes($"{userName}:{accessToken}")));

//            if (_cacheOptionsSnapshot != null)
//            {
//                httpClient.Timeout = _cacheOptionsSnapshot.Value.HttpClientTimeout;
//            }

//            return httpClient;
//        }
//    }
//}
