using System;

namespace Uploader.Core.Managers.Video
{
    internal static class SizeHelper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="finalWidth"></param>
        /// <param name="finalHeight"></param>
        /// <returns>largeur, hauteur</returns>
        public static Tuple<int, int> GetSize(double width, double height, double finalWidth, double finalHeight)
        {
            //video verticale, garder hauteur finale, réduire largeur finale
            if(width / height < finalWidth / finalHeight)
                return new Tuple<int, int>(GetWidth(width, height, finalHeight), (int)finalHeight);
            
            // sinon garder largeur finale, réduire hauteur finale
            return new Tuple<int, int>((int)finalWidth, GetHeight(width, height, finalWidth));
        }
        
        public static int GetWidth(double width, double height, double finalHeight)
        {
            return GetPair((int)(finalHeight * width / height));
        }

        public static int GetHeight(double width, double height, double finalWidth)
        {
            return GetPair((int)(finalWidth * height / width));
        }

        public static int GetPair(int number)
        {
            return number % 2 == 0 ? number : number + 1;
        }
    }
}