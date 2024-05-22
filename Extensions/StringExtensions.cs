namespace DreamBot.Extensions
{
    public static class StringExtensions
    {
        public static byte[] FromBase64(this string base64String)
        {
            if (string.IsNullOrWhiteSpace(base64String))
            {
                return null;
            }

            try
            {
                return Convert.FromBase64String(base64String);
            }
            catch (FormatException)
            {
                // Handle invalid base64 string format
                return null;
            }
        }

        public static string ApplyTemplate(this string baseString, string template)
        {
            if (string.IsNullOrWhiteSpace(template))
            {
                return baseString;
            }

            if (template.Contains("{0}"))
            {
                return string.Format(template, baseString);
            }
            else
            {
                return $"{baseString},{template}";
            }
        }

        public static List<string> ApplyTemplate(this List<string> baseString, string template)
        {
            if (string.IsNullOrWhiteSpace(template))
            {
                return baseString;
            }

            string merged = string.Join(",", baseString);

            if (template.Contains("{0}"))
            {
                merged = string.Format(template, merged);
            }
            else
            {
                merged = $"{merged},{template}";
            }

            return merged.Split(',').Select(x => x.Trim()).ToList();
        }
    }
}