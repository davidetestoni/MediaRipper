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
    public class Ripper
    {
        public string RootFolder { get; set; }
        public Uri Domain { get; set; }
        public int Threads { get; set; }

        private HttpClientHandler httpClientHandler;
        private HttpClient httpClient;
        private CookieContainer cookieContainer;
        private Dictionary<string, string> customHeaders;

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

        public Ripper(string domain, string rootFolder) : this(new Uri(domain), rootFolder) { }

        /// <summary>
        /// Fetches all the data according to a regex pattern.
        /// </summary>
        public async Task<IEnumerable<FetchedItem>> FetchAsync(string relativeUri, string baseFolder, IEnumerable<FetchPattern> patterns)
        {
            var uri = relativeUri.StartsWith("/") ? new Uri(Domain, relativeUri) : new Uri(relativeUri);
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

        public async Task<IEnumerable<FetchedItem>> FetchAsync(string relativeUri, string baseFolder, FetchPattern pattern)
        {
            return await FetchAsync(relativeUri, baseFolder, new FetchPattern[] { pattern });
        }

        public async Task<IEnumerable<FetchedItem>> FetchAllAsync(IEnumerable<FetchedItem> items, IEnumerable<FetchPattern> patterns, string baseFolder = "", IProgress<ProgressReport> progress = null)
        {
            var total = items.Count();
            var current = 0;

            SemaphoreSlim ss = new SemaphoreSlim(Threads);
            var tasks = items.Select(async item =>
            {
                await ss.WaitAsync();

                var message = "";

                List<FetchedItem> fetched = new List<FetchedItem>();
                try
                {
                    fetched = (await FetchAsync(item.uri, baseFolder == "" ? item.path : baseFolder, patterns)).ToList();
                    message = $"Fetched {fetched.Count} items from {item.uri}";
                }
                catch (Exception ex)
                {
                    message = $"Failed to fetch {item.uri}. Reason: {ex.Message}";
                }
                finally
                {
                    if (progress != null) { progress.Report(new ProgressReport(++current * 100.0f / total, message)); }
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

        public async Task<IEnumerable<FetchedItem>> FetchAllAsync(IEnumerable<FetchedItem> items, FetchPattern pattern, string baseFolder = "", IProgress<ProgressReport> progress = null)
        {
            return await FetchAllAsync(items, new FetchPattern[] { pattern }, baseFolder, progress);
        }

        /// <summary>
        /// Downloads the data to disk.
        /// </summary>
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

                string message = "";

                try
                {
                    await RipAsync(item);
                    message = $"Downloaded {item.uri} to {item.path}";
                }
                catch (Exception ex)
                {
                    message = $"Failed to download {item.uri}. Reason: {ex.Message}";
                }
                finally
                {
                    if (progress != null) { progress.Report(new ProgressReport(++current * 100.0f / total, message)); }
                    ss.Release();
                }
            });

            await Task.WhenAll(tasks);
        }

        public async Task RipAsync(FetchedItem item)
        {
            // Create the directory tree
            var dir = Path.GetDirectoryName(item.path);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            // Try to get the stream from the server
            var uri = item.uri.StartsWith("/") ? new Uri(Domain, item.uri) : new Uri(item.uri);
            var response = await httpClient.GetStreamAsync(uri);

            // Copy it to a file
            using (var fs = new FileStream(item.path, FileMode.Create))
            {
                await response.CopyToAsync(fs);
            }
        }

        public Ripper WithSessionCookie(Cookie cookie)
        {
            cookieContainer.Add(cookie);
            return this;
        }

        public Ripper WithSessionCookie(string name, string value)
        {
            return WithSessionCookie(new Cookie(name, value, "/", Domain.Host));
        }

        public Ripper WithHeader(string name, string value)
        {
            customHeaders.Add(name, value);
            return this;
        }

        public Ripper WithTimeout(int seconds)
        {
            httpClient.Timeout = TimeSpan.FromSeconds(seconds);
            return this;
        }

        public Ripper WithThreads(int threads)
        {
            Threads = threads;
            return this;
        }
    }
}
