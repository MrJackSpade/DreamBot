using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DreamBot.Shared.Models
{
    public class Prompt
    {
        public Prompt(string text)
        {
            Text = text;
            List<string> parts = [];
            Parts = parts;

            foreach (string part in text.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)))
            {
                parts.Add(part);
            }
        }

        public IReadOnlyList<string> Parts { get; private set; }

        public string Text { get; private set; }
    }
}