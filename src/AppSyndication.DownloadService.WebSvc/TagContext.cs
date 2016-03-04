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

        private readonly Connection _connection;

        public TagContext(ILoggerFactory loggerFactory, IMemoryCache cache, Connection connection)
        {
            _logger = loggerFactory.CreateLogger(typeof(TagContext).FullName);

            _cache = cache;

            _connection = connection;
        }

        public async Task<string> GetTagDownloadRedirectUriAsync(string key)
        {
            string redirectUri;

            if (!_cache.TryGetValue(key, out redirectUri))
            {
                _logger.LogWarning("Cache miss for download key: {0}", key);

                var redirectTable = _connection.RedirectTable();

                var redirect = await redirectTable.GetRedirectAsync(key);

                redirectUri = redirect?.Uri;
            }

            return redirectUri;
        }

        public async Task IncrementDownloadRedirectCountAsync(string key, string ip)
        {
            var downloadsTable = _connection.DownloadTable();

            await downloadsTable.IncrementDownloadRedirectCountAsync(key, ip);
        }
    }
}
