using System.Text.RegularExpressions;

namespace MangaDownloader.MangaRetrievers
{
    public static class MangaRetrieverHelper
    {
        public static string CleanUpText(string txt)
        {
            var result = txt.Replace("â€”", "—");
            var entityPattern = "&#[0-9]+;|#x[0-9a-fA-F]+;|&[a-zA-Z]+;";
            var entityExpression = new Regex(entityPattern);
            result = entityExpression.Replace(result, (Match match) => {
                if (match.Value.StartsWith("&#x"))
                {
                    return char.ConvertFromUtf32(int.Parse(match.Value.Substring(3, match.Length - 4), System.Globalization.NumberStyles.HexNumber));
                }
                else if (match.Value.StartsWith("&#"))
                {
                    return char.ConvertFromUtf32(int.Parse(match.Value.Substring(2, match.Length - 3)));
                }
                else
                {
                    return match.Value;
                }
            });
            return result;
        }
    }
}
