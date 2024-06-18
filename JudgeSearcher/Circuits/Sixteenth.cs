using HtmlAgilityPack;
using JudgeSearcher.Models;
using JudgeSearcher.Utility;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeSearcher.Circuits
{
    internal class Sixteenth : Base
    {
        public Sixteenth() : base()
        {
            Status = 1;
        }

        public override string Alias => "Sixteenth";

        public override string Description => "Monroe";

        public override string URL => "http://www.keyscourts.net/";

        public override Task<string> Execute()
        {
            judges = new ObservableCollection<Judge>();

            _ = Scraper.Scan(URL, (driver, wait) =>
            {
                driver.FindElement(By.LinkText("Judges")).Click();

                wait.Until((e) => By.XPath("//h1[contains(text(), 'Judges')]"));

                HtmlWeb web = new HtmlWeb();
                var document = web.Load(driver.Url);
                var nodes = document.DocumentNode.SelectNodes("//span[@class='cta-button cta-icon-left  cta-custom-color-0 cta-button-small cta-button-nomoicon']");

                foreach (var node in nodes)
                {
                    var button = driver.FindElement(By.XPath(node.XPath));

                    button.Click();

                    wait.Until((e) => By.Id("pageTitle"));

                    document = web.Load(driver.Url);

                    var table = document.DocumentNode.SelectSingleNode("//*[contains(@id,'sectionGroup')]/div[1]");

                    var exclusions = new string[] { "script", "span" };

                    var columns = table.ChildNodes.ToArray();

                    Dictionary<string, List<string>> map = new Dictionary<string, List<string>>();

                    foreach (var column in columns)
                    {
                        if (!exclusions.Contains(column.Name))
                        {
                            var header = column.FirstChild.FirstChild.FirstChild.InnerText;
                            var rows = column.FirstChild.FirstChild.ChildNodes[1].FirstChild.FirstChild.ChildNodes.Select((e) => e.InnerText).ToList();

                            if (header == "Judge ")
                            {
                                var links = column.FirstChild.FirstChild.ChildNodes[1].FirstChild.FirstChild.Descendants("a").ToList();

                                foreach (var link in links)
                                {
                                    driver.FindElement(By.XPath(link.XPath)).Click();

                                    if (driver.WindowHandles.Count > 1)
                                    {
                                        driver.SwitchTo().Window(driver.WindowHandles.Last());

                                        var address = driver.FindElements(By.XPath("//div[contains(@class, 'contentAreaElement elmRte rte-content-holder')]")).Where((e) => e.Text.Contains("Sixteenth Judicial Circuit")).Select((e) => e.Text).First();

                                        if (!map.Keys.Contains("Address"))
                                        {
                                            map["Address"] = new List<string> { address };
                                        }
                                        else
                                        {
                                            map["Address"].Add(address);
                                        }

                                        driver.SwitchTo().Window(driver.WindowHandles.Last()).Close();

                                        driver.SwitchTo().Window(driver.WindowHandles.First());
                                    }
                                    else
                                    {
                                        string address = driver.FindElements(By.XPath("//div[contains(@class, 'contentAreaElement elmRte rte-content-holder')]")).Where((e) => e.Text.Contains("Sixteenth Judicial Circuit")).Select((e) => e.Text).First();

                                        if (!map.Keys.Contains("Address"))
                                        {
                                            map["Address"] = new List<string> { address };
                                        }
                                        else
                                        {
                                            map["Address"].Add(address);
                                        }

                                        driver.Navigate().Back();
                                    }
                                }
                            }

                            map[header] = rows;
                        }
                    }

                    for (int i = 0; i < map["Judge "].Count; i++)
                    {
                        Judge judge = new Judge()
                        {
                            Circuit = Alias,
                            County = Description
                        };

                        var name = map["Judge "][i].Replace("Honorable Judge ", string.Empty).Trim();

                        judge.LastName = name.Substring(name.LastIndexOf(" "));
                        judge.FirstName = name.Replace(judge.LastName, string.Empty);
                        judge.JudicialAssistant = map.Keys.Contains("Judicial Assistant ") ? map["Judicial Assistant "][i].Trim() : string.Empty;

                        if (map.Keys.Contains("Contact Information "))
                        {
                            var contacts = map["Contact Information "][i].Trim().Split(" &nbsp;");
                            judge.Phone = contacts[0].Replace("Office: ", string.Empty);
                        }

                        var address = map["Address"][i].Split("\r\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

                        address.Remove("Sixteenth Judicial Circuit");

                        judge.Type = address.Where(e => e.Contains("Judge")).FirstOrDefault();
                        address.Remove(judge.Type);

                        judge.Location = address[0];
                        address.Remove(judge.Location);

                        string _city_zip = address.Where(e => e.Contains(", Fl") || e.Contains(", FL")).FirstOrDefault();

                        judge.City = _city_zip.Substring(0, _city_zip.IndexOf(","));
                        judge.Zip = _city_zip.Substring(_city_zip.LastIndexOf(" "));

                        address.Remove(_city_zip);

                        judge.Street = string.Join(", ", address);

                        judges.Add(judge);
                    }

                    driver.Navigate().Back();
                }
            });

            return base.Execute();
        }
    }
}
