using System;
using System.Linq;

namespace Uploader.Managers.Common
{
    public static class Tools
    {
        public static DateTime Max(params DateTime[] dates)
        {
            return dates.Max();
        }
    }
}