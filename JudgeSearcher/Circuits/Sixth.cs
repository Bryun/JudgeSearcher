using JudgeSearcher.Models;
using JudgeSearcher.Utility;
using OpenQA.Selenium;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JudgeSearcher.Circuits
{
    internal class Sixth : Base
    {
        public Sixth() : base()
        {
            Status = 1;
        }

        public override string Alias => "Sixth";

        public override string Description => "Pasco and Pinellas";

        public override string URL => "http://www.jud6.org/";

        public override Task<string> Execute()
        {
            judges = new ObservableCollection<Judge>();
            var map = new Dictionary<string, Dictionary<string, string>>();

            _ = Scraper.Scan(URL, (driver, wait) =>
            {
                try
                {
                    driver.FindElement(By.LinkText("Judges & Calendars")).Click();

                    wait.Until(e => By.LinkText("Judges' Contact Information"));

                    driver.FindElement(By.LinkText("Judges' Contact Information")).Click();

                    wait.Until(e => By.LinkText("JUDGES OF THE SIXTH JUDICIAL CIRCUIT OF FLORIDA"));

                    //driver.SwitchTo().Frame("frSheet");

                    var exclusions = new string[] { "Circuit Judges", "Chief Judge", "County Judges", string.Empty, " ", "&nbsp;", "*         ", "*   " };

                    string header = string.Empty;

                    foreach (var row in driver.FindElements(By.TagName("tr")))
                    {
                        var columns = row.FindElements(By.TagName("td")).Select(e => e.Text).ToList();

                        if (columns.Count == 7 && exclusions.Contains(columns[0]))
                        {
                            header = columns[0];
                        }
                        else if (columns.Count == 7 && !exclusions.Contains(columns[0]))
                        {
                            Judge judge = new Judge()
                            {
                                Type = header,
                                Circuit = Alias,
                                LastName = columns[0].Split(", ")[0],
                                FirstName = columns[0].Split(", ")[1].Trim(),
                                Phone = columns[1],
                                JudicialAssistant = columns[2],
                                Location = columns[3],
                                SubDivision = columns[4],
                                CourtRoom = columns[5],
                                HearingRoom = columns[6]
                            };

                            judges.Add(judge);
                        }
                        else if (columns.Count == 4 && !exclusions.Contains(columns[0]))
                        {
                            var context = columns.Where(e => !string.IsNullOrEmpty(e)).ToArray();

                            var cells = context.First().Split(",");

                            if (cells.Length >= 4)
                            { 
                                map[cells[0]] = new Dictionary<string, string>()
                                {
                                    { "Street", cells[1]},
                                    { "City", cells[2]},
                                    { "Zip", cells[3].Substring(cells[3].LastIndexOf(" "))},
                                };

                                cells = context.Last().Split(",");

                                map[cells[0]] = new Dictionary<string, string>()
                                {
                                    { "Street", cells[1]},
                                    { "City", cells[2]},
                                    { "Zip", cells[3].Substring(cells[3].LastIndexOf(" "))},
                                };
                            }
                        }
                    }

                    foreach (KeyValuePair<string, Dictionary<string, string>> pair in map)
                    {
                        foreach (var e in judges)
                        {
                            if (e.Location == pair.Key)
                            {
                                e.Street = pair.Value["Street"];
                                e.City = pair.Value["City"];
                                e.Zip = pair.Value["Zip"];
                            }
                        }
                    }

                    driver.Navigate().Back();

                    driver.FindElement(By.LinkText("Judges & Calendars")).Click();

                    wait.Until(e => By.LinkText("Courthouse Locations"));

                    driver.FindElement(By.LinkText("Courthouse Locations")).Click();

                    wait.Until(e => By.XPath("//td/strong[contains(text(), 'Courthouse & Locations')]"));

                    var link = new Dictionary<string, string>()
                    {
                        {"NPR","NPR"},
                        {"CJC","PCJC"},
                        {"DC","DC"},
                        {"SP","SPJB"},
                        {"CLWR","NCH"},
                        {"OCH","OCH"},
                        {"NCT","NCT"},
                        {"501","SP 501"},
                    };

                    foreach (var row in driver.FindElements(By.XPath("//table/tbody/tr")))
                    {
                        var cells = row.FindElements(By.TagName("td")).Select(e => e.Text).ToList();

                        if (cells.Count == 4 && cells[0] != "Location Code")
                        {
                            foreach (var e in judges)
                            {
                                if (e.Location == link[cells[0]])
                                {
                                    e.Location = string.Join(" ", cells[1].Split("\r\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

                                    var lines = Regex.Split(cells[2], "\\r\\n|, FL ");

                                    e.Street = lines[0];
                                    e.City = lines[1];
                                    e.Zip = lines[2];
                                }
                            }
                        }
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
