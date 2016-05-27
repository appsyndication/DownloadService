using System;
using System.Threading.Tasks;
using AppSyndication.BackendModel.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace AppSyndication.DownloadService.WebSvc
{
    public class TagContext : ITagContext
    {
        private readonly IMemoryCache _cache;

        private readonly ILogger _logger;

        private readonly IRedirectTable _redirectTable;

        private readonly IDownloadTable _downloadTable;

        public TagContext(ILoggerFactory loggerFactory, IMemoryCache cache, IRedirectTable redirectTable, IDownloadTable downloadTable)
        {
            _logger = loggerFactory.CreateLogger(typeof(TagContext).FullName);

            _cache = cache;

            _redirectTable = redirectTable;

            _downloadTable = downloadTable;
        }

        public async Task<string> GetTagDownloadRedirectUriAsync(string key)
        {
            string redirectUri;

            if (!_cache.TryGetValue(key, out redirectUri))
            {
                _logger.LogWarning("Cache miss for download key: {0}", key);

                var redirect = await _redirectTable.GetRedirectAsync(key);

                redirectUri = redirect?.Uri;
            }

            return redirectUri;
        }

        public async Task IncrementDownloadRedirectCountAsync(string key, string ip)
        {
            await _downloadTable.IncrementDownloadRedirectCountAsync(key, ip);
        }
    }
}
