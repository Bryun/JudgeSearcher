using JudgeSearcher.Models;
using JudgeSearcher.Utility;
using OpenQA.Selenium;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JudgeSearcher.Circuits
{
    public class Fourth : Base
    {
        public Fourth() : base()
        {
            Status = 1;
        }

        public override string Alias => "Fourth";

        public override string Description => "Clay, Duval and Nassau";

        public override string URL => "http://www.coj.net/departments/fourth-judicial-circuit-court.aspx";

        public override Task<string> Execute()
        {
            collection = new ObservableCollection<Judge>();

            _ = Scraper.Scan(URL, (driver, wait) =>
            {
                driver.FindElement(By.XPath("//*[@id='FeatureBox']/div/ul/li[2]/a")).Click();

                wait.Until((e) => By.XPath("//span[contains(text(), 'Circuit and County Judges of the Fourth Judicial Circuit')]"));

                var counties = driver.FindElements(By.XPath("//div[@class='TableInlineWrapper']/h2"));

                bool search = true;

                try
                {
                    foreach (var county in counties)
                    {
                        search = true;

                        string type = string.Empty;

                        IWebElement element = null;

                        while (search)
                        {
                            if (element == null)
                            {
                                if (county.FindElements(By.XPath("./following-sibling::*")).Count > 0)
                                    element = county.FindElement(By.XPath("./following-sibling::*"));
                            }
                            else if (element.FindElements(By.XPath("./following-sibling::*")).Count > 0)
                            {
                                element = element.FindElement(By.XPath("./following-sibling::*"));
                            }
                            else
                            {
                                search = false;
                                break;
                            }

                            switch (element.TagName)
                            {
                                case "h2":
                                    search = false;
                                    break;
                                case "h3":
                                    type = element.Text;
                                    break;
                                case "table":
                                    foreach (var row in element.FindElements(By.XPath("./tbody/tr")))
                                    {
                                        if (row.FindElements(By.XPath("./td")).Count > 0)
                                        {
                                            var cells = row.FindElements(By.XPath("./td")).Select(e => e.Text.Replace("&nbsp;", string.Empty).Trim()).ToList();

                                            try
                                            {
                                                if (cells.Count > 0 && !cells.Any(e => string.IsNullOrEmpty(e)))
                                                {
                                                    var judge = new Judge()
                                                    {
                                                        SubDivision = cells[0],
                                                        Phone = cells[2],
                                                        CourtRoom = cells.Count >= 4 ? cells[3] : string.Empty,
                                                        HearingRoom = cells.Count >= 4 ? cells[3] : string.Empty,
                                                        County = county.Text.Replace("&nbsp;", string.Empty),
                                                        Type = type,
                                                        Circuit = Alias
                                                    };

                                                    var indexes = cells[1].ToCharArray().Select((x, y) => x.Equals(' ') ? y : -1).Where(i => i != -1).ToArray();

                                                    if (indexes.Length > 0)
                                                    {
                                                        judge.FirstName = Regex.IsMatch(cells[1], ", III|, JR\\.") ? cells[1].Substring(0, indexes[indexes.Length - 2]).Trim() : cells[1].Substring(0, indexes[indexes.Length - 1]).Trim();
                                                        judge.LastName = Regex.IsMatch(cells[1], ", III|, JR\\.") ? cells[1].Substring(indexes[indexes.Length - 2]).Trim() : cells[1].Substring(indexes[indexes.Length - 1]).Trim();
                                                    }

                                                    collection.Add(judge);
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                Log.Logger.Error(ex.StackTrace);
                                            }
                                        }
                                    }
                                    break;
                                default:
                                    Log.Logger.Error(element.TagName);
                                    break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Logger.Error(ex.StackTrace);
                }

                driver.FindElement(By.XPath("//a[contains(text(), 'Courthouse Locations')]")).Click();

                wait.Until(e => By.XPath("//span[contains(text(), 'Courthouse Locations')]"));

                List<Dictionary<string, string>> map = new List<Dictionary<string, string>>();

                foreach (var row in driver.FindElements(By.XPath("//tbody/tr")))
                {
                    if (row.FindElements(By.XPath("./td")).Count > 0)
                    {
                        var cells = row.FindElements(By.XPath("./td")).Select(e => e.Text).ToList();

                        try
                        {
                            map.Add(new Dictionary<string, string> {
                                { "Name", cells[0]},
                                { "Address", cells[1]},
                                { "Phone", cells[2]}
                            });
                        }
                        catch (Exception ex)
                        {
                            Log.Logger.Error(ex.StackTrace);
                        }
                    }
                }

                foreach (var address in map)
                {
                    foreach (var item in collection)
                    {
                        if (address["Name"].Contains(item.County))
                        {
                            var lines = address["Address"].Split("\r\n");

                            item.Location = address["Name"];
                            item.Street = lines[0];

                            if (lines.Length > 1 && lines[1].Contains(", FL "))
                            {
                                item.City = lines[1].Split(", FL ")[0];
                                item.Zip = lines[1].Split(", FL ")[1];
                            }
                        }
                    }
                }
            });

            return base.Execute();
        }
    }
}
