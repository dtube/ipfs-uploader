using Microsoft.Extensions.Configuration;

using Uploader.Core.Managers.Front;
using Uploader.Core.Managers.Ipfs;
using Uploader.Core.Managers.Video;

namespace Uploader.Core.Managers.Common
{
    public static class Startup
    {
        public static void InitSettings(IConfiguration Configuration)
        {
            Configuration.GetSection("General").Bind(GeneralSettings.Instance);
            Configuration.GetSection("Ipfs").Bind(IpfsSettings.Instance);
            Configuration.GetSection("Video").Bind(VideoSettings.Instance);
        }
    }
}