using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MediaRipper
{
    /// <summary>
    /// A ripper for remote media on a website.
    /// </summary>
    public class Ripper
    {
        /// <summary>The base domain of the website.</summary>
        /// <example>https://example.com</example>
        public Uri Domain { get; set; }

        /// <summary>The root folder on disk where the structured data will be downloaded.</summary>
        public string RootFolder { get; set; }

        /// <summary>The amount of threads to use for parallelized tasks.</summary>
        public int Threads { get; set; }

        private HttpClientHandler httpClientHandler;
        private HttpClient httpClient;
        private CookieContainer cookieContainer;
        private Dictionary<string, string> customHeaders;

        /// <summary>
        /// Initializes a ripper from a Uri.
        /// </summary>
        /// <param name="domain">The base domain of the website</param>
        /// <param name="rootFolder">The root folder on disk where the structured data will be downloaded</param>
        public Ripper(Uri domain, string rootFolder)
        {
            Domain = domain;
            RootFolder = rootFolder;
            Threads = 10;

            // Initialize the cookie container and headers dictionary
            cookieContainer = new CookieContainer();
            customHeaders = new Dictionary<string, string>();

            // Initialize the http client handler
            httpClientHandler = new HttpClientHandler();
            httpClientHandler.UseCookies = true;
            httpClientHandler.CookieContainer = cookieContainer;

            // Initialize the http client
            httpClient = new HttpClient(httpClientHandler);
        }

        /// <summary>
        /// Initializes a ripper from a domain string.
        /// </summary>
        /// <param name="domain">The base domain of the website</param>
        /// <param name="rootFolder">The root folder on disk where the structured data will be downloaded</param>
        public Ripper(string domain, string rootFolder) : this(new Uri(domain), rootFolder) { }

        /// <summary>
        /// Fetches all items from a remote page that match any of the given patterns.
        /// </summary>
        /// <param name="pageUri">The relative or absolute uri of the resource to query</param>
        /// <param name="baseFolder">The base folder where the structured data will be downloaded during ripping</param>
        /// <param name="patterns">The regex patterns which can match the required resource</param>
        /// <returns>All the found items</returns>
        public async Task<IEnumerable<FetchedItem>> FetchAsync(string pageUri, string baseFolder, IEnumerable<FetchPattern> patterns)
        {
            var uri = pageUri.StartsWith("/") ? new Uri(Domain, pageUri) : new Uri(pageUri);
            SetHeaders();
            var response = await httpClient.GetAsync(uri);
            var content = await response.Content.ReadAsStringAsync();

            List<FetchedItem> fetched = new List<FetchedItem>();
            foreach (var pattern in patterns)
            {
                foreach (Match m in Regex.Matches(content, pattern.pattern))
                {
                    var parsedName = new StringBuilder(pattern.nameFormat);
                    var parsedUri = new StringBuilder(pattern.uriFormat);
                    
                    for (var i = 0; i < m.Groups.Count; i++)
                    {
                        parsedName.Replace($"[{i}]", m.Groups[i].Value);
                        parsedUri.Replace($"[{i}]", m.Groups[i].Value);
                    }

                    fetched.Add(new FetchedItem(parsedUri.ToString(), parsedName.ToString(), Path.Combine(RootFolder, baseFolder)));
                }
            }

            return fetched;
        }

        /// <summary>
        /// Fetches all items from a remote page that match a given pattern.
        /// </summary>
        /// <param name="pageUri">The relative or absolute uri of the resource to query</param>
        /// <param name="baseFolder">The base folder where the structured data will be downloaded during ripping</param>
        /// <param name="pattern">The regex pattern which can match the required resource</param>
        /// <returns>All the found items</returns>
        public async Task<IEnumerable<FetchedItem>> FetchAsync(string pageUri, string baseFolder, FetchPattern pattern)
        {
            return await FetchAsync(pageUri, baseFolder, new FetchPattern[] { pattern });
        }

        /// <summary>
        /// Fetches all resources from a given set of pages in a parallelized way.
        /// </summary>
        /// <param name="items">The resources to query</param>
        /// <param name="patterns">The regex patterns which can match the required resource</param>
        /// <param name="baseFolder">The name of the subfolder in which the resources will be downloaded</param>
        /// <param name="progress">The progress reporting delegate</param>
        /// <returns>The fetched resources</returns>
        public async Task<IEnumerable<FetchedItem>> FetchAllAsync(IEnumerable<FetchedItem> items, IEnumerable<FetchPattern> patterns, string baseFolder = "", IProgress<ProgressReport> progress = null)
        {
            var total = items.Count();
            var current = 0;

            SemaphoreSlim ss = new SemaphoreSlim(Threads);
            var tasks = items.Select(async item =>
            {
                await ss.WaitAsync();

                bool success = true;
                string error = "";

                List<FetchedItem> fetched = new List<FetchedItem>();
                try
                {
                    fetched = (await FetchAsync(item.uri, baseFolder == "" ? item.path : baseFolder, patterns)).ToList();
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }
                finally
                {
                    if (progress != null) 
                    { 
                        progress.Report(new ProgressReport(++current * 100.0f / total, item, success, error, DateTime.Now, fetched.Count)); 
                    }
                    ss.Release();
                }

                return fetched;
            });

            var results = await Task.WhenAll(tasks);
            List<FetchedItem> joined = new List<FetchedItem>();
            foreach (var result in results)
            {
                joined.AddRange(result);
            }

            return joined;
        }

        /// <summary>
        /// Fetches all resources from a given set of pages in a parallelized way.
        /// </summary>
        /// <param name="items">The resources to query</param>
        /// <param name="pattern">The regex pattern which can match the required resource</param>
        /// <param name="baseFolder">The name of the subfolder in which the resources will be downloaded</param>
        /// <param name="progress">The progress reporting delegate</param>
        /// <returns>The fetched resources</returns>
        public async Task<IEnumerable<FetchedItem>> FetchAllAsync(IEnumerable<FetchedItem> items, FetchPattern pattern, string baseFolder = "", IProgress<ProgressReport> progress = null)
        {
            return await FetchAllAsync(items, new FetchPattern[] { pattern }, baseFolder, progress);
        }

        /// <summary>
        /// Downloads a collection of fetched items to the disk.
        /// </summary>
        /// <param name="items">The items to download</param>
        /// <param name="skipExisting">Whether to skip the items that already exist on disk</param>
        /// <param name="progress">The progress reporting delegate</param>
        public async Task RipAllAsync(IEnumerable<FetchedItem> items, bool skipExisting = true, IProgress<ProgressReport> progress = null)
        {
            var total = items.Count();
            var current = 0;

            SemaphoreSlim ss = new SemaphoreSlim(Threads);
            var tasks = items
                .Where(item => !skipExisting || (skipExisting && !File.Exists(item.path)))
                .Select(async item =>
            {
                // Wait for the semaphore
                await ss.WaitAsync();

                bool success = true;
                string error = "";

                try
                {
                    await RipAsync(item);
                }
                catch (Exception ex)
                {
                    success = false;
                    error = ex.Message;
                }
                finally
                {
                    if (progress != null) 
                    {
                        progress.Report(new ProgressReport(++current * 100.0f / total, item, success, error, DateTime.Now, 1)); 
                    }
                    ss.Release();
                }
            });

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Downloads a fetched item to disk.
        /// </summary>
        /// <param name="item">The item to download</param>
        public async Task RipAsync(FetchedItem item)
        {
            // Create the directory tree
            var dir = Path.GetDirectoryName(item.path);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            // Try to get the stream from the server
            var uri = item.uri.StartsWith("/") ? new Uri(Domain, item.uri) : new Uri(item.uri);
            SetHeaders();
            var response = await httpClient.GetStreamAsync(uri);

            // Copy it to a file
            using (var fs = new FileStream(item.path, FileMode.Create))
            {
                await response.CopyToAsync(fs);
            }
        }

        /// <summary>
        /// Adds a session cookie to the ripper's HttpClient.
        /// </summary>
        /// <param name="cookie">The cookie to add</param>
        public Ripper WithSessionCookie(Cookie cookie)
        {
            cookieContainer.Add(cookie);
            return this;
        }

        /// <summary>
        /// Adds a session cookie to the ripper's HttpClient, using the previously defined domain and a root path.
        /// </summary>
        /// <param name="name">The cookie name</param>
        /// <param name="value">The cookie value</param>
        public Ripper WithSessionCookie(string name, string value)
        {
            return WithSessionCookie(new Cookie(name, value, "/", Domain.Host));
        }

        /// <summary>
        /// Adds a session header to the ripper's HttpClient.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Ripper WithHeader(string name, string value)
        {
            customHeaders.Add(name, value);
            return this;
        }

        private void SetHeaders()
        {
            foreach (var header in customHeaders)
            {
                httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        /// <summary>
        /// Sets the timeout for HTTP requests.
        /// </summary>
        /// <param name="seconds">The maximum amount of seconds to wait before failing</param>
        public Ripper WithTimeout(int seconds)
        {
            httpClient.Timeout = TimeSpan.FromSeconds(seconds);
            return this;
        }

        /// <summary>
        /// Sets the amount of parallel threads for fetching and ripping.
        /// </summary>
        /// <param name="threads">The amount of parallel threads that can be active at the same time</param>
        public Ripper WithThreads(int threads)
        {
            Threads = threads;
            return this;
        }
    }
}