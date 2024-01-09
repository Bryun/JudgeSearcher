using HtmlAgilityPack;
using JudgeSearcher.Models;
using JudgeSearcher.Utility;
using OpenQA.Selenium;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JudgeSearcher.Circuits
{
    public class Eighteenth : Base
    {

        public Eighteenth() : base()
        {
            Status = 1;
        }

        public override string Alias => "Eighteenth";

        public override string Description => "Brevard and Seminole";

        public override string URL => "https://flcourts18.org/directory-home/"; //"http://www.flcourts18.org/page.php?2";

        public override Task<string> Execute()
        {
            collection = new ObservableCollection<Judge>();

            _ = Scraper.Scan(URL, (driver, wait) =>
            {
                try
                {
                    var hrefs = driver.FindElement(By.XPath("//*[@id='wppb-builder-container']/div[2]/div/div/div[1]/div/div")).FindElements(By.TagName("a")).Where(e => e.Text.Contains("Judge")).Select(e => e.GetAttribute("href")).ToList();

                    foreach (var href in hrefs)
                    {
                        var category = driver.FindElement(By.XPath(string.Format("//a[@href='{0}']", href)));

                        string county = category.Text.Contains("County") ? "County" : category.Text.Contains("Circuit") ? "Circuit" : string.Empty;

                        category.Click();

                        var document = new HtmlWeb().Load(driver.Url);

                        foreach (var node in document.DocumentNode.SelectNodes("//div[@class='wppb-flipbox-panel']"))
                        {
                            var x = node.Descendants("h4").First().InnerText;
                            var y = node.Descendants("div").Where(e => e.Attributes.Any(x => x.Value == "wppb-flip-front-intro")).First().InnerText;

                            var lines = string.Join("|", Regex.Split(y, "Judicial Assistant:|Division:|Office:")).Split("|", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                            var judge = new Judge()
                            {
                                Circuit = Alias,
                                Type = county,
                                JudicialAssistant = lines[0],
                                SubDivision = lines[1],
                                Phone = Regex.Match(lines[2], "\\(\\d{3}\\) \\d{3}-\\d{4}").Value,
                            };

                            if (x.Contains(" "))
                            {
                                judge.LastName = x.Substring(0, x.LastIndexOf(" ")).Trim();
                                judge.FirstName = x.Substring(x.LastIndexOf(" ")).Trim();
                            }
                            else
                            {
                                judge.LastName = x;
                            }

                            var z = lines[2].Contains(",") ? lines[2].Replace(judge.Phone, string.Empty).Split(",") : new string[] { lines[2].Substring(0, lines[2].LastIndexOf(" ")), lines[2].Substring(lines[2].LastIndexOf(" ")).Trim() };

                            judge.Location = z[0].Substring(0, z[0].LastIndexOf(" ")).Replace(judge.Phone, string.Empty).Trim();
                            judge.City = z[0].Substring(z[0].LastIndexOf(" ")).Trim();
                            judge.County = z[1].Trim();

                            collection.Add(judge);

                        }
                    }

                    if (driver.FindElements(By.XPath("//a[contains(text(), 'Get Courthouse Directions')]")).Count > 0)
                    {
                        driver.FindElement(By.XPath("//a[contains(text(), 'Get Courthouse Directions')]")).Click();

                        try
                        {
                            if (driver.WindowHandles.Count > 1)
                            {
                                driver.SwitchTo().Window(driver.WindowHandles.Last());
                            }

                            wait.Until(e => By.XPath("//h1[contains(text(), 'Brevard & Seminole Counties')]"));

                            var address = driver.FindElements(By.XPath("//div[@class='su-tabs-panes']/div")).Select(e => e.GetAttribute("innerText")).Distinct().ToList();

                            var first = address[0].Split("\r\n\r\n\t\r\n \r\n\r\n");

                            address = address.TakeLast(address.Count - 1).Concat(first).ToList();

                            address.ForEach(e =>
                            {
                                var list = e.Split(new string[] { "\r\n", ", FL ", " FL ", " FL, ", "," }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

                                collection.Where(e => list.Any(x => e.Location.Contains(x)) || (e.Location == "Civil Courthouse" && list.Any(x => x == "Seminole Civil Court House"))).ToList().ForEach(e =>
                                {
                                    e.Zip = list.Where(y => Regex.IsMatch(y, "\\d{5}(-\\d{4})?")).FirstOrDefault()!;
                                    e.Street = list[list.IndexOf(e.Zip) - 2];
                                });

                            });

                            if (driver.WindowHandles.Count > 1)
                            {
                                driver.SwitchTo().Window(driver.WindowHandles.Last()).Close();
                                driver.SwitchTo().Window(driver.WindowHandles.First());
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Logger.Error(ex.StackTrace);
                        }
                    }

                }
                catch (Exception ex)
                {
                    Log.Logger.Error(ex.StackTrace);
                }
            }, allowImages: true);

            return base.Execute();
        }
    }
}
