using HtmlAgilityPack;
using JudgeSearcher.Models;
using JudgeSearcher.Utility;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace JudgeSearcher.Circuits
{
    internal class Seventh : Base
    {
        public Seventh() : base()
        {
            Status = 1;
        }

        public override string Alias => "Seventh";

        public override string Description => "St. Johns, Volusia, Flagler and Putnam";

        public override string URL => "http://www.circuit7.org/";

        public override Task<string> Execute()
        {
            collection = new ObservableCollection<Judge>();

            _ = Scraper.Scan(URL, (driver, wait) =>
            {
                Actions actions = new Actions(driver);

                actions.MoveToElement(driver.FindElement(By.XPath("//*[@id='jet-menu-item-279']/a"))).Perform();
                actions.MoveToElement(driver.FindElement(By.XPath("//*[@id='jet-menu-item-704']/a"))).Click().Perform();
                wait.Until(e => By.XPath("//h2[contains()text(), 'Courthouse Locations']"));

                var locations = driver.FindElements(By.XPath("//a[contains(text(), 'Read More')]/../preceding-sibling::p")).Select(e => string.Join("|", Regex.Split(e.Text, ", FL |\\r\\n")).Split("|", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)).ToList();

                Thread.Sleep(TimeSpan.FromSeconds(3));

                actions.MoveToElement(driver.FindElement(By.XPath("//*[@id='jet-menu-item-279']/a"))).Perform();
                actions.MoveToElement(driver.FindElement(By.XPath("//*[@id='jet-menu-item-3019']/a"))).Perform();
                actions.MoveToElement(driver.FindElement(By.XPath("//*[@id='jet-menu-item-3014']/a"))).Click().Perform();
                wait.Until(e => By.XPath("//h2[contains(text(), 'Judges of the Seventh Judicial Circuit')]"));

                try
                {
                    var h2s = driver.FindElements(By.XPath("//*[self::h2]")).ToList();
                    var start = h2s.Where(e => e.Text == "Circuit Judges").FirstOrDefault();
                    var chief = h2s.Where(e => e.Text == "Chief").FirstOrDefault();
                    var filtered = h2s.TakeLast(h2s.Count - h2s.IndexOf(start)).ToList();

                    filtered.Remove(chief);

                    var charlie = filtered.Select(e =>
                    {
                        try
                        {
                            if (e.Text.Contains("Judges"))
                            {
                                return new Dictionary<string, dynamic> { { "Type", e.Text } };
                            }
                            else
                            {
                                var details = e.FindElements(By.XPath("./../../following-sibling::*")).Select(e => e.Text).ToList();

                                var name = string.IsNullOrEmpty(Regex.Replace(e.Text, "Chief Judge |Judge |Chief ", string.Empty)) ? Regex.Replace(details.FirstOrDefault()!, "Chief Judge |Judge |Chief ", string.Empty) : Regex.Replace(e.Text, "Chief Judge |Judge |Chief ", string.Empty);
                                var assistant = details.Where(e => e.Contains("Judicial Assistant: ")).FirstOrDefault()!.Replace("Judicial Assistant: ", string.Empty);
                                var phone = details.Where(e => e.Contains("Phone Number: ")).FirstOrDefault()!.Replace("Phone Number: ", string.Empty);
                                var city = Regex.Replace(details.Where(e => e.Contains("Location: ")).FirstOrDefault()!, "Location: | &#8211;&nbsp;| – ", string.Empty);
                                var county = details.LastOrDefault();

                                var names = name.FullName();

                                return new Dictionary<string, dynamic> {
                                    { "href", e.FindElement(By.XPath("./a")).GetAttribute("href") },
                                    { "Judge", new Judge()
                                        {
                                            FirstName = names.FirstOrDefault(),
                                            LastName = names.LastOrDefault(),
                                            JudicialAssistant = assistant,
                                            Phone = phone,
                                            City = city,
                                            County = county,
                                            Circuit = Alias
                                        }
                                    },
                                };
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Logger.Error(ex.StackTrace);
                        }

                        return new Dictionary<string, dynamic>();

                    }).ToList();

                    var _type = string.Empty;

                    foreach (var context in charlie)
                    {
                        if (context.ContainsKey("Type"))
                        {
                            _type = context["Type"];
                        }
                        else
                        {
                            Judge judge = context["Judge"];
                            judge.Type = _type;

                            driver.FindElement(By.XPath(string.Format("//a[@href='{0}']", context["href"]))).Click();

                            wait.Until((e) => By.XPath("//h2[@class='elementor-heading-title elementor-size-default']"));

                            var document = new HtmlWeb().Load(driver.Url);

                            var division = document.DocumentNode.SelectNodes("//div[@class = 'elementor-text-editor elementor-clearfix']").Where((e) => e.InnerText.Contains("Division")).First();

                            judge.SubDivision = division.InnerText.Replace(" &#8211; ", ", ").Trim();

                            var address = string.Join("|", Regex.Split(division.ParentNode.ParentNode.NextSibling.NextSibling.InnerText, "\\r\\n|\\n|\\t|, FL |,")).Split("|", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                            judge.CourtRoom = address.Where(e => Regex.IsMatch(e, "Rm.|Room|Courtroom|Bldg\\.|Suite")).FirstOrDefault() ?? string.Empty;
                            judge.Zip = address.Where(e => Regex.IsMatch(e, "[0-9]{5}$")).FirstOrDefault()!;
                            judge.City = address[address.IndexOf(judge.Zip) - 1];
                            judge.Location = address.Where(e => Regex.IsMatch(e, "Courthouse|Justice Center")).FirstOrDefault() ?? string.Empty;

                            address.Remove(judge.Location);
                            address.Remove(judge.CourtRoom);
                            address.Remove(judge.City);
                            address.Remove(judge.Zip);

                            judge.Street = address.FirstOrDefault() ?? string.Empty;

                            collection.Add(judge);

                            driver.Navigate().Back();
                        }
                    }

                    var mapping = new Dictionary<string, string>()
                    {
                        { "101 N. Alabama Ave." , "101 N. Alabama Ave."},
                        { "101 N. Alabama Avenue" , "101 N. Alabama Ave."},
                        { "125 E. Orange Ave." , "125 E. Orange Ave."},
                        { "125 E. Orange Avenue" , "125 E. Orange Ave."},
                        { "125 East Orange Avenue"  , "125 E. Orange Ave."},
                        { "1769 E. Moody Blvd. Bldg #1" , "1769 E. Moody Blvd."},
                        { "1769 E. Moody Blvd." , "1769 E. Moody Blvd."},
                        { "2400 S Ridgewood Ave." , "2400 S Ridgewood Ave."},
                        { "251 N. Ridgewood Ave." , "251 N. Ridgewood Ave."},
                        { "4010 Lewis Speedway" , "4010 Lewis Speedway"},
                        { "410 St. Johns Ave." , "410 St. Johns Ave."},
                        { "P.O. Box 758" , "P.O. Box 758"},
                    };

                    foreach (var location in locations)
                    {
                        collection.Where(e => mapping[e.Street].Equals(location[1])).ToList().ForEach(e =>
                        {
                            e.Location = location[0];
                        });
                    }
                }
                catch (Exception ex)
                {
                    Log.Logger.Error(ex.StackTrace);
                }
            });

            return base.Execute();
        }
    }
}
