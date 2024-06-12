using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office.CustomUI;
using DocumentFormat.OpenXml.Spreadsheet;
using HtmlAgilityPack;
using JudgeSearcher.Models;
using JudgeSearcher.Utility;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
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

        public List<Judge> Rows { get; set; }

        public override Task<string> Execute()
        {
            if (Rows == null)
                Rows = new List<Judge>();

            Rows.Clear();

            _ = Scraper.Scan(URL, (driver, wait) =>
            {
                Actions action = new Actions(driver);
                action.MoveToElement(driver.FindElement(By.XPath("//a[contains(text(), 'Courts & Judges')]"))).Perform();

                var judiciaries = driver.FindElements(By.XPath("//a[text()='Judiciary']")).Select(e => e.GetAttribute("href")).Distinct().ToList();

                foreach (var judiciary in judiciaries)
                {
                    driver.FindElement(By.XPath($"//a[@href='{judiciary}']")).Click();

                    var titles = driver.FindElements(By.XPath($"//h1|//h3")).Select(e => new Tuple<string, string>(e.TagName, e.Text)).ToList();

                    var judgeType = string.Empty;

                    foreach (var title in titles)
                    {
                        try
                        {
                            if (title.Item1.Equals("h3") && Regex.IsMatch(title.Item2, "Judiciary"))
                                judgeType = Regex.IsMatch(title.Item2, "County") ? "County Judge" : Regex.IsMatch(title.Item2, "Circuit") ? "Circuit Judge": "Unknown";

                            if (!Regex.IsMatch(title.Item2, "County|Judiciary$|Announced$"))
                            {
                                var name = Regex.Match(title.Item2, @"(?:Judge )(.*)(?=\r)?").Groups[1].Value.Trim();
                                var surname = Regex.Match(name, @"(?:\s)\w+$").Value.Trim();

                                //if (title.Item2.Contains("Pena"))
                                //    Debug.WriteLine("");

                                Judge judge = new Judge()
                                {
                                    Circuit = Alias,
                                    Type = judgeType,
                                    County = titles.Where(e => e.Item1.Equals("h1")).FirstOrDefault().Item2,
                                    LastName = surname
                                };

                                judge.FirstName = name.Replace(judge.LastName, string.Empty).Trim();

                                var _type = driver.FindElements(By.XPath($"//{title.Item1}[starts-with(text(), '{title.Item2.Split(Environment.NewLine)[0]}')]/../../following-sibling::div[1]/descendant::h4")).FirstOrDefault();

                                if (_type != null)
                                {
                                    if (Regex.IsMatch(_type.Text, "County"))
                                        judge.Type = "County Judge";
                                    else if (Regex.IsMatch(_type.Text, "Circuit"))
                                        judge.Type = "Circuit Judge";
                                }

                                wait.Until(ExpectedConditions.ElementExists(By.XPath($"//{title.Item1}[starts-with(text(), '{title.Item2.Split(Environment.NewLine)[0]}')]/../../following-sibling::div/descendant::a[text()='More Info']"))).Click();

                                if (driver.WindowHandles.Count > 1)
                                    driver.SwitchTo().Window(driver.WindowHandles.Last());

                                var current_assignment = wait.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(By.XPath("//h3[text()='Current Assignment']/../../following-sibling::div/descendant::p"))).Select(e => e.Text).ToList();

                                //current_assignment.ForEach(e => Debug.WriteLine($"Assignment:\r\n{e}\r\n"));

                                driver.FindElement(By.XPath("//a/span[contains(text(), 'Judicial Assistant')]/..")).Click();

                                var assistants = wait.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(By.XPath("//h3[contains(text(), 'Judicial Assistant')]/../../following-sibling::div/descendant::p | //h3[contains(text(), 'Judicial Assistant')]/../../following-sibling::div/div/div"))).Select(e => e.Text).ToList();

                                judge.JudicialAssistant = new Regex(@"(.*(?=,))|(.*(?=\r\n))|(.*)").Match(assistants.FirstOrDefault()).Value;

                                //assistants.ForEach(e => Debug.WriteLine($"Assistant:\r\n{e}\r\n"));

                                driver.FindElement(By.XPath("//a/span[contains(text(), 'Office')]/..")).Click();

                                var addresses = wait.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(By.XPath("//h3[contains(text(), 'Office')]/../../following-sibling::div/div/p | //h3[contains(text(), 'Office')]/../../following-sibling::div/descendant::p | //h3[contains(text(), 'Office')]/following-sibling::p"))).Select(e => e.Text).ToList();

                                addresses.ForEach(e => Debug.WriteLine($"Address:\r\n{e}r\n"));

                                foreach (var address in addresses)
                                {
                                    Judge clone = (Judge)judge.Clone();

                                    clone.Phone = Regex.Match(address, @"\(\d{3}\) \d{3}-\d{4}").Value;

                                    clone.Location = Regex.Match(address, @".*(?=\r\n)").Value;
                                    clone.Street = Regex.Match(address, @".*Avenue|.*Street").Value;
                                    clone.CourtRoom = Regex.Match(address, @"Room\s\w+").Value;
                                    clone.City = Regex.Match(address, @".*(?=,\sFL\s\d+|,\sFlorida\s\d+)").Value;
                                    clone.Zip = Regex.Match(address, @"(?:FL )\d+|(?:Florida )\d+").Value;

                                    Rows.Add(clone);
                                }

                                Debug.WriteLine(JsonSerializer.Serialize(judge, new JsonSerializerOptions
                                {
                                    WriteIndented = true,
                                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
                                }));

                                Debug.WriteLine("------------------------------------------------------------------------------------------");

                                if (driver.WindowHandles.Count > 1)
                                {
                                    driver.SwitchTo().Window(driver.WindowHandles.Last()).Close();
                                    driver.SwitchTo().Window(driver.WindowHandles.First());
                                }
                                else
                                    driver.Navigate().Back();
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                            Debug.WriteLine(ex.StackTrace);
                        }
                    }

                    action.MoveToElement(driver.FindElement(By.XPath("//a[contains(text(), 'Courts & Judges')]"))).Perform();
                }
            }, period: TimeSpan.FromSeconds(3));

            Rows = Rows.Where(e => !string.IsNullOrEmpty(e.Location) && !e.Location.Contains("All packages")).Select(e => e).ToList();

            collection = new ObservableCollection<Judge>(Rows);

            return base.Execute();
        }

        public Task<string> ExecuteOld()
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
                        driver.FindElement(By.XPath($"//a[@href='{href}']")).Click();

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

                            var street = !string.IsNullOrEmpty(address.Where(e => Regex.IsMatch(e, "Avenue|Street")).FirstOrDefault()) ? address.Where(e => Regex.IsMatch(e, "Avenue|Street")).FirstOrDefault() : string.Empty;

                            map.Street = street.Contains(",") ? street.Substring(0, street.IndexOf(",")) : street;
                            map.Location = address[Array.IndexOf(address, street) - 1];
                            map.CourtRoom = street.Contains("Room") ? street.Substring(street.IndexOf(",") + 1) : string.Empty;

                            var town = address.Where(e => e.Contains(", FL ")).FirstOrDefault()!.Split(", FL", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                            map.City = town[0];
                            map.Zip = town[1];

                            var phone = address.Where(e => Regex.IsMatch(e, @"\(\d{3}\) \d{3}-\d{4}")).FirstOrDefault();

                            map.Phone = !string.IsNullOrEmpty(phone) ? Regex.Match(phone, @"\(\d{3}\) \d{3}-\d{4}").Value : string.Empty;
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

                                var street = !string.IsNullOrEmpty(address.Where(e => Regex.IsMatch(e, "Avenue|Street")).FirstOrDefault()) ? address.Where(e => Regex.IsMatch(e, "Avenue|Street")).FirstOrDefault() : string.Empty;

                                map.Street = street.Contains(",") ? street.Substring(0, street.IndexOf(",")) : street;
                                map.Location = address[Array.IndexOf(address, street) - 1];
                                map.CourtRoom = street.Contains("Room") ? street.Substring(street.IndexOf(",") + 1) : string.Empty;

                                var town = address.Where(e => e.Contains(", FL ")).FirstOrDefault()!.Split(", FL", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                                map.City = town[0];
                                map.Zip = town[1];

                                var phone = address.Where(e => Regex.IsMatch(e, @"\(\d{3}\) \d{3}-\d{4}")).FirstOrDefault();

                                map.Phone = !string.IsNullOrEmpty(phone) ? Regex.Match(phone, @"\(\d{3}\) \d{3}-\d{4}").Value : string.Empty;
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

            }, allowImages: true);

            return base.Execute();
        }

    }
}
