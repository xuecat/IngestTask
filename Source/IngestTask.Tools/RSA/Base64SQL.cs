using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IngestTask.Tool
{
    public static class Base64SQL
    {
        private static string ind0 = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=";
        private static string ind = "0S1o2b3e4ya7cd8fghijklmn6pqrstuvwx9zABCDEFGHIJKLMNOPQR5TUVWXYZ+/=";

        public static string ToBase64String(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            return Convert.ToBase64String(bytes);
        }
        //解码
        public static string Base64_Decode(string pSrc)
        {
            string dest = "";
            try
            {
                string src2 = "";
                char[] map = ind.ToArray();
                if (pSrc!= null)
                {
                    foreach (char a in pSrc)
                    {
                        src2 += map[ind0.IndexOf(a)];
                    }
                }

                byte[] bpath = Convert.FromBase64String(src2);
                dest = System.Text.ASCIIEncoding.Default.GetString(bpath);
            }
            catch (Exception)
            {

            }
            return dest;
        }
    }
}
