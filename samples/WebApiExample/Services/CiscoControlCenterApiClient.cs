//using System.Diagnostics;
//using System.Net;
//using System.Text.Json;
//using System.Text.Json.Serialization;
//using System.Runtime.CompilerServices;
//using System.ComponentModel.DataAnnotations;
//using System.ComponentModel;

//namespace WebApiExample.Services
//{
//    public record DeviceApiPaginatedModel
//    {
//        [JsonPropertyName("pageNumber")]
//        public int PageNumber { get; set; }
//        [Required]
//        [JsonPropertyName("devices")]
//        public IEnumerable<DeviceApiModel> Devices { get; set; } = null!;
//        [JsonPropertyName("lastPage")]
//        public bool LastPage { get; set; }
//    }

//    public record DeviceApiModel
//    {
//        /// <summary>
//        /// Integrated Circuit Card ID for the SIM chip.
//        /// 19-20 unique ID printed on the phsical SIM device.
//        /// <see href="https://en.wikipedia.org/wiki/SIM_card#ICCID">ICCID</see> is
//        /// made up of 2 MII, 2-3 country, 1-4 II/MNC, <=10 IIN/MSIN, 1-2 Luhn digits.
//        /// </summary>
//        [Required]
//        public string Iccid { get; set; } = null!;
//        public string Status { get; set; } = null!;
//        public string RatePlan { get; set; } = null!;
//        public string CommunicationPlan { get; set; } = null!;

//        public override string ToString() => $"ICCID={Iccid}, Status={Status}";
//    }

//    public record SimDeviceDetailModel
//    {
//        /// <summary>
//        /// Integrated Circuit Card ID for the SIM chip.
//        /// SIM card serial number printed on the phsical device.
//        /// Normally 19 or 20 digit unique ID with standard prefix '89'
//        /// followed by the country code then a further sequence of digits.
//        /// This generally takes one of 3 forms:
//        ///  Type A: d1 … d18, d19 = luhn_seq(d1 … d18) i.e. 19 digits total
//        ///  Type B: d1 … d19, d20 = luhn_seq(d1 … d19) i.e. 20 digits total
//        ///  Type C: d1 … d18, d19 = luhn_seq(d1 … d18), d20 i.e. 20 digits total
//        /// <see href="https://en.wikipedia.org/wiki/SIM_card#ICCID">ICCID</see> is
//        /// made up of 2 MII, 2-3 country, 1-4 II/MNC, <=10 IIN/MSIN, 1-2 Luhn digits.
//        /// </summary>
//        [Key]
//        [Required]
//        public string Iccid { get; set; } = null!;

//        /// <summary>
//        /// Mobile Station Integrated Services Digital Network number.
//        /// Used for routing calls; the cellphone number users dial.
//        /// Watch out for leading zeros and/or the '+' prefix.
//        /// </summary>
//        public string? Msisdn { get; set; }

//        /// <summary>
//        /// International Mobile Subscriber Identity.
//        /// Used to identify individual users of a cellular network
//        /// internationally, stored on the SIM card itself.
//        /// ~15 digits composed of MCC, MNC, MSIN:
//        ///  * MCC = Mobile Country Code
//        ///  * MNC = Mobile Network Code
//        ///  * MSIN = sequential serial number
//        /// </summary>
//        public string? Imsi { get; set; }

//        /// <summary>
//        /// International Mobile Equipment Identity.
//        /// 3GPP hardware address (like IEEE 802 MAC).
//        /// </summary>
//        public string? Imei { get; set; }

//        /// <summary>
//        /// Date and time the SIM was added
//        /// </summary>
//        [DisplayName("Date Added")]
//        public DateTimeOffset? DateAdded { get; set; }

//        /// <summary>
//        /// Date and time the SIM was last updated
//        /// </summary>
//        [DisplayName("Date Updated")]
//        public DateTimeOffset? DateUpdated { get; set; }

//        /// <summary>
//        /// Cellular customer group ID
//        /// </summary>
//        public string? GroupId { get; set; }

//        public override string ToString() =>
//            $"ICCID={Iccid}, MSISDN={Msisdn}, IMSI={Imsi}, IMEI={Imei}, GroupId={GroupId}, added {DateAdded}, updated {DateUpdated}";
//    }

//    public record SimDevicePaginatedModel
//    {
//        public int PageNumber { get; set; } = 1;
//        public bool IsLastPage { get; set; } = true;
//        public IEnumerable<string> Iccids { get; set; } = new List<string>();
//        public int IccidCount { get; set; } = 0;

//        public override string ToString() =>
//            $"Page #{PageNumber,2} {(IsLastPage ? "is" : "is not")} the last page and contains {IccidCount} ICCID(s)";
//    }

//    /// <summary>
//    /// <see href="https://pubhub.devnetcloud.com/media/control-center-sandbox/docs/Content/api/rest/get_started_rest.htm#api_sim_status"/>
//    /// </summary>
//    public enum CiscoApiDeviceStatus
//    {
//        [Description("INVENTORY")]
//        INVENTORY,
//        [Description("TEST_READY")]
//        TEST_READY,
//        [Description("ACTIVATION_READY")]
//        ACTIVATION_READY,
//        [Description("ACTIVATED")]
//        ACTIVATED,
//        [Description("DEACTIVATED")]
//        DEACTIVATED,
//        [Description("REPLACED")]
//        REPLACED,
//        [Description("RETIRED")]
//        RETIRED,
//        [Description("PURGED")]
//        PURGED
//    }

//    public interface ICiscoControlCenterApiClient
//    {
//        Task<SimDeviceDetailModel> GetSimDetailAsync(string iccid, string? fieldNames = null, CancellationToken cancellationToken = default);
//        IAsyncEnumerable<SimDeviceDetailModel> GetSimDetails(IEnumerable<string> iccids, string? fieldNames = null, CancellationToken cancellationToken = default);
//        Task<SimDevicePaginatedModel> GetSimsPageAsync(DateTimeOffset modifiedSince, int pageNumber = 1, int? pageSize = null, CiscoApiDeviceStatus status = CiscoApiDeviceStatus.TEST_READY, CancellationToken cancellationToken = default);
//        IAsyncEnumerable<IEnumerable<string>> GetSimIccids(DateTimeOffset modifiedSince, CiscoApiDeviceStatus status = CiscoApiDeviceStatus.TEST_READY, int? maxResults = null, CancellationToken cancellationToken = default);
//    }

//    public sealed class CiscoControlCenterApiClient : ICiscoControlCenterApiClient, IDisposable
//    {
//        private const int MaxPageSize = 50;
//        private const string AllFields = "*";
//        private static readonly string _dateTimeWriteFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ssK";
//        private static readonly string _dateTimeReadFormat = "yyy'-'MM'-'dd' 'HH':'mm':'ss'.'fffK";

//        private static readonly JsonSerializerOptions JsonOptions = new()
//        {
//            PropertyNameCaseInsensitive = true,
//            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
//        };

//        private readonly HttpClient _httpClient;

//        public CiscoControlCenterApiClient(HttpClient httpClient)
//        {
//            _httpClient = httpClient;
//        }

//        /// <summary>Returns detailed information about a specified device.
//        /// <see href="https://developer.cisco.com/docs/control-center/#!getting-started/api-best-practices">
//        /// Best Practice</see> is to get just the information you need.
//        /// <example>Curl request example (make sure to use your own user credentials):<code>
//        /// curl -X GET --header "Accept: application/json" --header "Authorization: Basic {AccessToken}" "https://rws-jpotest.jasper.com/rws/api/v1/devices/{iccid}"
//        /// </code></example></summary>
//        /// <param name="iccid">ICCID to search</param>
//        /// <param name="cancellationToken">Cancellation token</param>
//        /// <returns>SIM info from the API</returns>
//        /// <exception cref="OperationCanceledException"><see cref="HttpClient">HTTP client</see> operation cancelled</exception>
//        /// <exception cref="HttpRequestException">HTTP request failure, web host may be offline</exception>
//        /// <exception cref="NotSupportedException">Content type not supported</exception>
//        /// <exception cref="JsonException">Invalid JSON</exception>
//        /// <exception cref="NullReferenceException">ICCID not found</exception>
//        public async Task<SimDeviceDetailModel> GetSimDetailAsync(string iccid, string? fieldNames = AllFields, CancellationToken cancellationToken = default)
//        {
//            fieldNames ??= AllFields;
//            string resourcePath = $"devices/{iccid}?fields={fieldNames}";
//            var result = await _httpClient.GetFromJsonAsync<DeviceApiDetailModel>(resourcePath, JsonOptions, cancellationToken).ConfigureAwait(false) ??
//                throw new NullReferenceException($"No valid SIM information was found for ICCID {iccid}");
//            var mappedResult = result.ToDto();
//            return mappedResult;
//        }

//        /// <summary>
//        /// Returns detailed information about a specified ICCID
//        /// </summary>
//        /// <param name="iccids">ICCIDs to search</param>
//        /// <param name="fieldNames">CSV field filter</param>
//        /// <param name="cancellationToken">Cancellation token</param>
//        /// <returns>The specific field(s) from the SIM info object</returns>
//        /// <exception cref="ArgumentNullException">ICCID is null or empty</exception>
//        public async IAsyncEnumerable<SimDeviceDetailModel> GetSimDetails(IEnumerable<string> iccids, string? fieldNames = AllFields, [EnumeratorCancellation] CancellationToken cancellationToken = default)
//        {
//            if (iccids != null)
//            {
//                int id = 0;
//                fieldNames ??= AllFields;
//                var enumerator = iccids.GetEnumerator();
//                while (!cancellationToken.IsCancellationRequested && enumerator.MoveNext())
//                {
//                    var iccid = enumerator.Current;
//                    var simInfo = await GetSimDetailAsync(iccid, fieldNames, cancellationToken);
//                    Debug.WriteLine($"Get SIM detail request #{++id} returned ICCID {simInfo.Iccid}, MSISDN {simInfo.Msisdn}");
//                    yield return simInfo;
//                }
//            }
//        }

//        public async Task<IEnumerable<SimDeviceDetailModel>> GetSimDetailsAsync(IEnumerable<string> iccids, string? fieldNames = AllFields, CancellationToken cancellationToken = default)
//        {
//            List<SimDeviceDetailModel> results = new();
//            if (iccids != null)
//            {
//                fieldNames ??= AllFields;
//                await foreach (var simInfo in GetSimDetails(iccids, fieldNames, cancellationToken).ConfigureAwait(false))
//                {
//                    results.Add(simInfo);
//                }
//            }
//            return results;
//        }

//        public async Task<SimDevicePaginatedModel> GetSimsPageAsync(DateTimeOffset modifiedSince, int pageNumber = 1, int? pageSize = MaxPageSize, CiscoApiDeviceStatus status = CiscoApiDeviceStatus.TEST_READY, CancellationToken cancellationToken = default)
//        {
//            if (pageSize <= 0)
//            {
//                return new();
//            }
//            if (pageSize > MaxPageSize)
//            {
//                pageSize = MaxPageSize;
//            }
//            string modifiedDate = modifiedSince.ToString(_dateTimeWriteFormat);
//            string encodedDate = WebUtility.UrlEncode(modifiedDate);
//            string resourcePath = $"devices?status={status}&modifiedSince={encodedDate}&pageSize={pageSize}&pageNumber={pageNumber}";
//            var result = await _httpClient.GetFromJsonAsync<DeviceApiPaginatedModel>(resourcePath, JsonOptions, cancellationToken).ConfigureAwait(false);
//            var mappedResult = result.ToDto();
//            return mappedResult;
//        }

//        // TODO: set a Polly timeout if IsLastPage goes on for too long
//        // TODO: add XML comments
//        /// <summary>
//        /// Note: IEnumerable ICCIDs used by JasperApiClient.GetMissingIccidsAsync().
//        /// </summary>
//        /// <param name="modifiedSince"></param>
//        /// <param name="status"></param>
//        /// <param name="maxResults"></param>
//        /// <param name="cancellationToken"></param>
//        /// <returns>IEnumerable ICCIDs</returns>
//        public async IAsyncEnumerable<IEnumerable<string>> GetSimIccids(DateTimeOffset modifiedSince, CiscoApiDeviceStatus status = CiscoApiDeviceStatus.TEST_READY, int? maxResults = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
//        {
//            int totalCount = 0;
//            int pageNumber = 0;
//            bool hasReachedLimit = false;
//            bool hasMaxLimit = maxResults > 0;
//            int pageSize = hasMaxLimit && maxResults < MaxPageSize ? maxResults.Value : MaxPageSize;
//            Trace.TraceInformation("Requesting all SIM API devices since {0}", modifiedSince);
//            Stopwatch stopwatch = new();
//            while (!hasReachedLimit && !cancellationToken.IsCancellationRequested)
//            {
//                stopwatch.Restart();
//                var page = await GetSimsPageAsync(modifiedSince, ++pageNumber, pageSize, status, cancellationToken).ConfigureAwait(false);
//                Trace.TraceInformation("GET devices page {0} returned {1} after {2}ms",
//                    pageNumber, page.IccidCount, stopwatch.ElapsedMilliseconds);
//                totalCount += page.IccidCount;
//                hasReachedLimit = page.IsLastPage || (hasMaxLimit && totalCount >= maxResults);
//                yield return page.Iccids;
//            }
//            stopwatch.Stop();
//            Trace.TraceInformation("{0} total SIM API devices received in {1:#.##} seconds",
//                totalCount, stopwatch.Elapsed.TotalSeconds);
//        }

//        public void Dispose()
//        {
//            _httpClient?.Dispose();
//        }
//    }
//}
