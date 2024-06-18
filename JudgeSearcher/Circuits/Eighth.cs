using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office.CustomUI;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using HtmlAgilityPack;
using JudgeSearcher.Interfaces;
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
        #region Constructor

        public Eighth() : base()
        {
            Status = 1;
            Checklist = new ObservableCollection<Validated>
            {                           
                new Validated("Eighth", "Alachua County", "Circuit Judge", "Craig C.", "DeThomasis"),
                new Validated("Eighth", "Alachua County", "Circuit Judge", "David P.", "Kreider"),
                new Validated("Eighth", "Alachua County", "Circuit Judge", "Denise R.", "Ferrero"),
                new Validated("Eighth", "Alachua County", "Circuit Judge", "Donna M.", "Keim"),
                new Validated("Eighth", "Alachua County", "Circuit Judge", "George M.", "Wright"),
                new Validated("Eighth", "Alachua County", "Circuit Judge", "James M.", "Colaw"),
                new Validated("Eighth", "Alachua County", "Circuit Judge", "Mark W.", "Moseley"),
                new Validated("Eighth", "Alachua County", "Circuit Judge", "Phillip A.", "Pena"),
                new Validated("Eighth", "Alachua County", "Circuit Judge", "Robert K.", "Groeb"),
                new Validated("Eighth", "Alachua County", "Circuit Judge", "Sean", "Brewer"),
                new Validated("Eighth", "Alachua County", "Circuit Judge", "Susanne Wilson", "Bullard"),
                new Validated("Eighth", "Alachua County", "Circuit Judge", "William E.", "Davis"),
                new Validated("Eighth", "Alachua County", "Circuit Judge", "Gloria R.", "Walker"),
                new Validated("Eighth", "Alachua County", "County Judge", "Jonathan D.", "Ramsey"),
                new Validated("Eighth", "Alachua County", "County Judge", "Kristine Van", "Vorst"),
                new Validated("Eighth", "Alachua County", "County Judge", "Meshon T.", "Rawls"),
                new Validated("Eighth", "Alachua County", "County Judge", "Susan", "Miller-Jones"),
                new Validated("Eighth", "Baker County", "Circuit Judge", "Phillip A.", "Pena"),
                new Validated("Eighth", "Baker County", "Circuit Judge", "Sean", "Brewer"),
                new Validated("Eighth", "Baker County", "County Judge", "Lorelie P.", "Brannan"),
                new Validated("Eighth", "Bradford County", "Circuit Judge", "George M.", "Wright"),
                new Validated("Eighth", "Bradford County", "Circuit Judge", "James M.", "Colaw"),
                new Validated("Eighth", "Bradford County", "County Judge", "D. Tatum", "Davis"),
                new Validated("Eighth", "Gilchrist County", "Circuit Judge", "David P.", "Kreider"),
                new Validated("Eighth", "Gilchrist County", "Circuit Judge", "Robert K.", "Groeb"),
                new Validated("Eighth", "Gilchrist County", "County Judge", "Sheree H.", "Lancaster"),
                new Validated("Eighth", "Levy County", "Circuit Judge", "Craig C.", "DeThomasis"),
                new Validated("Eighth", "Levy County", "Circuit Judge", "William E.", "Davis"),
                new Validated("Eighth", "Levy County", "County Judge", "Luis", "Bustamante"),
                new Validated("Eighth", "Union County", "Circuit Judge", "Robert K.", "Groeb"),
                new Validated("Eighth", "Union County", "County Judge", "Mitchell D.", "Bishop")
            };

           
        }

        #endregion

        #region Properties

        public override string Alias => "Eighth";

        public override string Description => "Alachua, Baker, Bradford, Gilchrist, Levy, and Union";

        public override string URL => "http://www.circuit8.org/";

        public List<Judge> Rows { get; set; }

        #endregion

        #region Methods

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
                                judgeType = Regex.IsMatch(title.Item2, "County") ? "County Judge" : Regex.IsMatch(title.Item2, "Circuit") ? "Circuit Judge" : "Unknown";

                            if (!Regex.IsMatch(title.Item2, "County|Judiciary$|Announced$"))
                            {
                                var name = Regex.Match(title.Item2, @"(?:Judge )(.*)(?=\r)?").Groups[1].Value.Trim();
                                var surname = Regex.Match(name, @"(?:\s)[A-Za-z-]+$").Value.Trim();

                                Judge judge = new Judge()
                                {
                                    Circuit = Alias,
                                    Type = judgeType,
                                    County = titles.Where(e => e.Item1.Equals("h1")).First().Item2,
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

                                driver.FindElement(By.XPath("//a/span[contains(text(), 'Judicial Assistant')]/..")).Click();

                                var assistants = wait.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(By.XPath("//h3[contains(text(), 'Judicial Assistant')]/../../following-sibling::div/descendant::p | //h3[contains(text(), 'Judicial Assistant')]/../../following-sibling::div/div/div"))).Select(e => e.Text).ToList();

                                judge.JudicialAssistant = new Regex(@"(.*(?=,))|(.*(?=\r\n))|(.*)").Match(assistants.FirstOrDefault()).Value;

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

            Judges = new ObservableCollection<Judge>(Rows);

            return base.Execute();
        }

        #endregion

    }
}
