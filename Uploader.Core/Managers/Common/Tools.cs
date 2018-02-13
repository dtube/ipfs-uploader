using System;
using System.Linq;

namespace Uploader.Core.Managers.Common
{
    internal static class Tools
    {
        public static DateTime Max(params DateTime[] dates)
        {
            return dates.Max();
        }
    }
}