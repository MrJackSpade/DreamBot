using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DreamBot.Extensions
{
    public static class StringExtensions
    {
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
    }
}