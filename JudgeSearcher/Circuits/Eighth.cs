using HtmlAgilityPack;
using JudgeSearcher.Models;
using JudgeSearcher.Utility;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace JudgeSearcher.Circuits
{
    internal class Eighth : Base
    {
        public Eighth() : base()
        {
            Status = 1;
        }

        public override string Alias => "Eighth";

        public override string Description => "Alachua, Baker, Bradford, Gilchrist, Levy, and Union";

        public override string URL => "http://www.circuit8.org/";

        public override Task<string> Execute()
        {
            collection = new ObservableCollection<Judge>();

            _ = Scraper.Scan(URL, (driver, wait) =>
            {
                Actions action = new Actions(driver);

                action.MoveToElement(driver.FindElement(By.XPath("//a[contains(text(), 'Contact & Information')]"))).Perform();

                action.MoveToElement(driver.FindElement(By.XPath("//a[contains(text(), 'Telephone Directory')]"))).Click().Perform();

                wait.Until(e => By.XPath("//a[contains(text(), 'EJCC – Telephone Directory')]"));

                var document = new HtmlWeb().Load(driver.Url);

                var tables = document.DocumentNode.Descendants("tbody").Take(7).ToArray();

                foreach (var table in tables)
                {
                    string _county = string.Empty;

                    if (table.ParentNode.ParentNode.Descendants("p").FirstOrDefault() != null)
                    {
                        _county = table.ParentNode.ParentNode.Descendants("p").FirstOrDefault()!.InnerText;
                    }

                    var rows = table.Descendants("tr");

                    foreach (var row in rows)
                    {
                        var cells = row.Descendants("td").Select(e => e.InnerText).ToArray();

                        Judge judge = new Judge()
                        {
                            Circuit = Alias,
                            JudicialAssistant = cells[1],
                            Phone = cells[2],
                            County = _county
                        };

                        var title = string.Join(", ", Regex.Split(cells[0], ",|-")).Split(", ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        var name = title[0].Replace("Hon. ", string.Empty);

                        judge.Type = title.Length > 1 ? title[1] : string.Empty;

                        if (judge.Type.Contains("County") && string.IsNullOrEmpty(judge.County))
                        {
                            judge.County = judge.Type.Substring(0, judge.Type.IndexOf("County") + 6).Trim();
                        }

                        judge.LastName = name.Substring(name.LastIndexOf(" ")).Trim();
                        judge.FirstName = name.Substring(0, name.LastIndexOf(" ")).Trim();
                        judge.Location = cells[5];

                        collection.Add(judge);
                    }
                }

                //---------------------------------------------------------------------------------

                var secondary = new List<Judge>();

                action.MoveToElement(driver.FindElement(By.XPath("//a[contains(text(), 'Courts & Judges')]"))).Perform();
                action.MoveToElement(driver.FindElement(By.XPath("//a[contains(text(), 'Circuit Judges')]"))).Click().Perform();
                wait.Until(e => By.XPath("//h2[contains(text(), 'Circuit Judges')]"));

                var more_infos = driver.FindElements(By.XPath("//a[contains(text(), 'More Info')]")).Select(e => e.GetAttribute("href")).ToList();

                foreach (var href in more_infos)
                {
                    try
                    {
                        driver.FindElement(By.XPath(string.Format("//a[@href='{0}']", href))).Click();

                        if (driver.WindowHandles.Count > 1)
                        {
                            driver.SwitchTo().Window(driver.WindowHandles.Last());
                        }

                        wait.Until(e => By.XPath("//h3[contains(text(), 'Current Assignment')]"));

                        var root = new HtmlWeb().Load(driver.Url);

                        var map = new Judge();

                        ReadOnlyCollection<IWebElement> assignments = driver.FindElements(By.XPath("//h3[contains(text(), 'Current Assignment')]/../../following-sibling::div/descendant::p"));
                        Thread.Sleep(TimeSpan.FromSeconds(1));
                        map.Type = assignments.Count > 0 ? assignments.FirstOrDefault()!.Text.Split(",")[0] : string.Empty;

                        driver.FindElement(By.XPath("//a/span[contains(text(), 'Judicial Assistant')]/..")).Click();
                        wait.Until(e => By.XPath("//h3[contains(text(), 'Judicial Assistant')]"));
                        ReadOnlyCollection<IWebElement> assistants = driver.FindElements(By.XPath("//h3[contains(text(), 'Office')]/../../following-sibling::div/descendant::p"));
                        Thread.Sleep(TimeSpan.FromSeconds(1));
                        map.JudicialAssistant = assistants.Count > 0 ? assistants.FirstOrDefault()!.Text.Split("\r\n")[0] : string.Empty;

                        driver.FindElement(By.XPath("//a/span[contains(text(), 'Office')]/..")).Click();
                        wait.Until(e => By.XPath("//h3[contains(text(), 'Office')]"));
                        ReadOnlyCollection<IWebElement> values = driver.FindElements(By.XPath("//h3[contains(text(), 'Office')]/../../following-sibling::div/descendant::p"));

                        if (values.Count == 0)
                        {
                            values = driver.FindElements(By.XPath("//h3[contains(text(), 'Office')]/following-sibling::p"));
                        }

                        Thread.Sleep(TimeSpan.FromSeconds(2));

                        if (values.Count > 0)
                        {
                            var address = values.FirstOrDefault()!.Text.Split("\r\n");

                            var street_regex = "Avenue|Street";

                            var street = !string.IsNullOrEmpty(address.Where(e => Regex.IsMatch(e, street_regex)).FirstOrDefault()) ? address.Where(e => Regex.IsMatch(e, street_regex)).FirstOrDefault() : string.Empty;

                            map.Street = street.Contains(",") ? street.Substring(0, street.IndexOf(",")) : street;
                            map.Location = address[Array.IndexOf(address, street) - 1];
                            map.CourtRoom = street.Contains("Room") ? street.Substring(street.IndexOf(",") + 1) : string.Empty;

                            var town = address.Where(e => e.Contains(", FL ")).FirstOrDefault()!.Split(", FL", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                            map.City = town[0];
                            map.Zip = town[1];

                            var phone_regex = "\\(\\d{3}\\) \\d{3}-\\d{4}";
                            var phone = address.Where(e => Regex.IsMatch(e, phone_regex)).FirstOrDefault();

                            map.Phone = !string.IsNullOrEmpty(phone) ? Regex.Match(phone, phone_regex).Value : string.Empty;
                        }

                        var names = driver.FindElements(By.TagName("h2"));

                        if (names.Count > 0)
                        {
                            var t = names[0].Text;
                            t = t.Replace("Judge ", string.Empty);
                            map.LastName = t.Substring(t.LastIndexOf(" ") + 1);
                            map.FirstName = t.Substring(0, t.LastIndexOf(" "));

                            secondary.Add(map);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Error(ex.StackTrace);
                    }

                    if (driver.WindowHandles.Count > 1)
                    {
                        driver.SwitchTo().Window(driver.WindowHandles.Last()).Close();
                        driver.SwitchTo().Window(driver.WindowHandles.First());
                    }
                    else
                        driver.Navigate().Back();


                }

                //---------------------------------------------------------------------------------

                var counties = new string[] { "Alachua County", "Baker County", "Bradford County", "Gilchrist County", "Levy County", "Union County" };


                foreach (var county in counties)
                {
                    action.MoveToElement(driver.FindElement(By.XPath("//a[contains(text(), 'Courts & Judges')]"))).Perform();

                    var element = driver.FindElement(By.XPath(string.Format("//a[contains(text(), '{0}')]", county)));
                    var a = element.FindElement(By.XPath("./../../../following-sibling::div/div/ul/li/a[contains(text(), 'Judiciary')]"));

                    action.MoveToElement(a).Click().Perform();
                    wait.Until(e => By.XPath(string.Format("//h2[contains(text(), '{0}')]", county)));

                    var hrefs = driver.FindElements(By.XPath("//a[contains(text(), 'More Info')]")).Select(e => e.GetAttribute("href")).ToList();

                    foreach (var href in hrefs)
                    {
                        try
                        {
                            var path = href.Replace("https://circuit8.org", string.Empty);

                            var more_info = driver.FindElements(By.XPath(string.Format("//a[@href='{0}']", path))).Count > 0 ? driver.FindElement(By.XPath(string.Format("//a[@href='{0}']", path))) : driver.FindElement(By.XPath(string.Format("//a[@href='{0}']", href)));

                            more_info.Click();

                            if (driver.WindowHandles.Count > 1)
                            {
                                driver.SwitchTo().Window(driver.WindowHandles.Last());
                            }

                            wait.Until(e => By.XPath("//h3[contains(text(), 'Current Assignment')]"));

                            var root = new HtmlWeb().Load(driver.Url);

                            var map = new Judge()
                            {
                                County = county,
                                Circuit = Alias
                            };

                            ReadOnlyCollection<IWebElement> assignments = driver.FindElements(By.XPath("//h3[contains(text(), 'Current Assignment')]/../../following-sibling::div/descendant::p"));
                            Thread.Sleep(TimeSpan.FromSeconds(1));
                            map.Type = assignments.Count > 0 ? assignments.FirstOrDefault()!.Text.Split(",")[0] : string.Empty;

                            driver.FindElement(By.XPath("//a/span[contains(text(), 'Judicial Assistant')]/..")).Click();
                            wait.Until(e => By.XPath("//h3[contains(text(), 'Judicial Assistant')]"));
                            ReadOnlyCollection<IWebElement> assistants = driver.FindElements(By.XPath("//h3[contains(text(), 'Office')]/../../following-sibling::div/descendant::p"));
                            Thread.Sleep(TimeSpan.FromSeconds(1));
                            map.JudicialAssistant = assistants.Count > 0 ? assistants.FirstOrDefault()!.Text.Split("\r\n")[0] : string.Empty;

                            driver.FindElement(By.XPath("//a/span[contains(text(), 'Office')]/..")).Click();
                            wait.Until(e => By.XPath("//h3[contains(text(), 'Office')]"));
                            ReadOnlyCollection<IWebElement> values = driver.FindElements(By.XPath("//h3[contains(text(), 'Office')]/../../following-sibling::div/descendant::p"));
                            Thread.Sleep(TimeSpan.FromSeconds(2));

                            if (values.Count > 0)
                            {
                                var address = values.FirstOrDefault()!.Text.Split("\r\n");

                                var street_regex = "Avenue|Street";

                                var street = !string.IsNullOrEmpty(address.Where(e => Regex.IsMatch(e, street_regex)).FirstOrDefault()) ? address.Where(e => Regex.IsMatch(e, street_regex)).FirstOrDefault() : string.Empty;

                                map.Street = street.Contains(",") ? street.Substring(0, street.IndexOf(",")) : street;
                                map.Location = address[Array.IndexOf(address, street) - 1];
                                map.CourtRoom = street.Contains("Room") ? street.Substring(street.IndexOf(",") + 1) : string.Empty;

                                var town = address.Where(e => e.Contains(", FL ")).FirstOrDefault()!.Split(", FL", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                                map.City = town[0];
                                map.Zip = town[1];

                                var phone_regex = "\\(\\d{3}\\) \\d{3}-\\d{4}";
                                var phone = address.Where(e => Regex.IsMatch(e, phone_regex)).FirstOrDefault();

                                map.Phone = !string.IsNullOrEmpty(phone) ? Regex.Match(phone, phone_regex).Value : string.Empty;
                            }

                            var names = driver.FindElements(By.TagName("h2"));

                            if (names.Count > 0)
                            {
                                var t = names[0].Text;
                                t = t.Replace("Judge ", string.Empty);
                                map.LastName = t.Substring(t.LastIndexOf(" ") + 1);
                                map.FirstName = t.Substring(0, t.LastIndexOf(" "));

                                secondary.Add(map);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Logger.Error(ex.StackTrace);
                        }

                        if (driver.WindowHandles.Count > 1)
                        {
                            driver.SwitchTo().Window(driver.WindowHandles.Last()).Close();
                            driver.SwitchTo().Window(driver.WindowHandles.First());
                        }
                        else
                            driver.Navigate().Back();
                    }
                }

                foreach (Judge x in secondary)
                    foreach (Judge y in collection)
                    {
                        if (x.FirstName.Equals(y.FirstName) && x.LastName.Equals(y.LastName))
                        {
                            y.Street = string.IsNullOrEmpty(x.Street) ? y.Street : x.Street;
                            y.Location = string.IsNullOrEmpty(x.Location) ? y.Location : x.Location;
                            y.CourtRoom = !string.IsNullOrEmpty(x.CourtRoom) ? x.CourtRoom : y.CourtRoom;
                            y.City = string.IsNullOrEmpty(x.City) ? y.City : x.City;
                            y.Zip = string.IsNullOrEmpty(x.Zip) ? y.Zip : x.Zip;
                            y.County = string.IsNullOrEmpty(x.County) ? y.County : x.County;
                        }
                    }

                var outstanding = secondary.Where(e => !collection.Any(f => e.FirstName.Equals(f.FirstName) && e.LastName.Equals(f.LastName))).ToList();

                outstanding.ForEach(e => collection.Add(e));

            });

            return base.Execute();
        }

    }
}
