using System.Threading.Tasks;

namespace AppSyndication.DownloadService.WebSvc
{
    public interface ITagContext
    {
        Task<string> GetTagDownloadRedirectUriAsync(string key);

        Task IncrementDownloadRedirectCountAsync(string key, string ip);
    }
}
