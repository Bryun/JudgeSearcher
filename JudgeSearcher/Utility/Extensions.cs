using OpenQA.Selenium;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace JudgeSearcher.Utility
{
    public static class Extensions
    {
        public static string[] FullName(this string value, string expression = ", III|, II|, Jr\\.|, JR\\.")
        {

            if (value.Contains(" "))
            {
                var indexes = value.ToCharArray().Select((x, y) => x.Equals(' ') ? y : -1).Where(i => i != -1).ToArray();

                return new string[] {
                Regex.IsMatch(value, expression) ? value.Substring(0, indexes[indexes.Length - 2]).Trim() : value.Substring(0, indexes[indexes.Length - 1]).Trim(),
                Regex.IsMatch(value, expression) ? value.Substring(indexes[indexes.Length - 2]).Trim() : value.Substring(indexes[indexes.Length - 1]).Trim()
            };
            }

            return new string[] { value, value };

        }

        public static string Type(this string value)
        {
            var expression = "Type: |\\r\\n";

            return string.Join("|", Regex.Split(value, expression)).Split("|", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault()!;
        }

        public static string County(this string value)
        {
            var expression = "County: |\\r\\n";

            return string.Join("|", Regex.Split(value, expression)).Split("|", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault()!;
        }

        public static string Phone(this string value)
        {
            var expression = "Phone: |\\r\\n|P:";

            return string.Join("|", Regex.Split(value, expression)).Split("|", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault()!;
        }

        public static string Assistant(this string value)
        {
            var expression = "Judicial Assistant: |\\r\\n|,|;| \\(";

            return string.Join("|", Regex.Split(value, expression)).Split("|", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault()!;
        }

        public static string Division(this string value)
        {
            var expression = "Division: |Assignment: |\\r\\n";

            return string.Join("|", Regex.Split(value, expression)).Split("|", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault()!;
        }

        public static string[] Address(this string value)
        {
            var expression = @"Address: |\r\n|, Florida |, FL |, Fl | FL |,";

            if (Regex.IsMatch(value, ", ST."))
                value = value.Replace(", ST.", " ST.");

            return string.Join("|", Regex.Split(value, expression)).Split("|", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToArray();
        }

        public static string XPath(this IWebElement element, string identify)
        {
            var href = element.GetAttribute("href");

            if (!href.Contains(identify)) 
                return string.Empty;

            href = href.Substring(href.IndexOf(identify));

            return string.Format("//a[@href='{0}']", href);
        }

        public static string HREF(this IWebElement element, string identify)
        {
            var href = element.GetAttribute("href");
            return string.Format("//a[@href='{0}']", href.Replace(identify, string.Empty));
        }
    }
}
