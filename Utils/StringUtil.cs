using System.Collections.Generic;

namespace Bangumi.Utils
{
    public static class StringUtil
    { 
        public static string FormatWithDictionary(string template, Dictionary<string, string> dictionary)
        {
            string result = template;
            foreach (var kvp in dictionary)
            {
                result = result.Replace($"%{kvp.Key}%", kvp.Value);
            }
            return result;
        }
    }
}
