using HtmlAgilityPack;
using JudgeSearcher.Models;
using JudgeSearcher.Utility;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace JudgeSearcher.Circuits
{
    internal class Seventeenth : Base
    {

        public Seventeenth() : base()
        {
            Status = 1;
        }

        public override string Alias => "Seventeenth";

        public override string Description => "Broward";

        public override string URL => "http://www.17th.flcourts.org/";

        public override Task<string> Execute()
        {
            judges = new ObservableCollection<Judge>();

            _ = Scraper.Scan(URL, (driver, wait) =>
            {
                try
                {
                    Actions action = new Actions(driver);

                    var judges = driver.FindElement(By.XPath("//*[@id='menu-item-141']/a"));
                    action.MoveToElement(judges).Perform();

                    var judiciary_list_by_division = driver.FindElement(By.XPath("//*[@id='menu-item-2628']/a"));
                    action.MoveToElement(judiciary_list_by_division).Click().Perform();

                    Thread.Sleep(TimeSpan.FromSeconds(10));

                    var categories = driver.FindElement(By.Id("slsUlCats")).FindElements(By.TagName("a")).Where(e => !e.Text.Equals("ALL JUDGES")).ToList();

                    categories.Where(e => e.Text.Contains("COUNTY")).ToList().ForEach(e =>
                    {
                        try
                        {
                            e.Click();
                            Thread.Sleep(TimeSpan.FromSeconds(5));
                            wait.Until(x => By.XPath("//div[@id='slsTbl_ltr_processing'][@style='display: none;']"));
                            Scan(driver, "County");
                        }
                        catch (Exception ex)
                        {
                            Log.Logger.Error(ex.StackTrace);
                        }

                    });

                    categories.Where(e => !e.Text.Contains("COUNTY")).ToList().ForEach(e =>
                    {
                        try
                        {
                            e.Click();
                            Thread.Sleep(TimeSpan.FromSeconds(5));
                            wait.Until(x => By.XPath("//div[@id='slsTbl_ltr_processing'][@style='display: none;']"));
                            Scan(driver, "Circuit");
                        }
                        catch (Exception ex)
                        {
                            Log.Logger.Error(ex.StackTrace);
                        }
                    });

                }
                catch (Exception ex)
                {
                    Log.Logger.Error(ex.StackTrace);
                }
            });

            return base.Execute();
        }

        private void Scan(ChromeDriver driver, string type)
        {
            var exclusions = new string[] {

                "Detention Weekday Juvenile",
                "Division FL In-Custody Change of Plea",
                "Division FM In-Custody AM Docket",
                "Division FQ In-Custody PM Docket",
                "Division FS In-Custody Additional PM",
                "First Appearance Zoom"
            };

            bool search = true;

            while (search)
            {
                var document = new HtmlWeb().Load(driver.Url);
                var rows = driver.FindElements(By.XPath("//table[@id='slsTbl_ltr']/tbody/tr"));

                foreach (var row in rows)
                {
                    try
                    {
                        var cells = row.FindElements(By.XPath("./td")).Select((e) => e.Text).ToList();

                        if (cells.Count > 1)
                        {
                            if (exclusions.Contains(cells[1]))
                            {
                                continue;
                            }

                            var indexes = cells[1].ToCharArray().Select((x, y) => x.Equals(' ') ? y : -1).Where(i => i != -1).ToArray();

                            string lastname = string.Empty;
                            string firstname = string.Empty;

                            if (cells[1].Contains(" "))
                            {
                                lastname = Regex.IsMatch(cells[1], "Jr.|JR.,|JR.") ? cells[1].Substring(0, indexes[1]) : cells[1].Substring(0, indexes[0]);
                                firstname = Regex.IsMatch(cells[1], "Jr.|JR.,|JR.") ? cells[1].Substring(indexes[1]) : cells[1].Substring(indexes[0]);
                            }
                            else
                            {
                                lastname = cells[1];
                            }

                            Judge judge = new Judge()
                            {
                                Type = type,
                                LastName = lastname.Trim(),
                                FirstName = firstname.Trim(),
                                JudicialAssistant = cells[2],
                                Phone = cells[3],
                                HearingRoom = cells[4],
                                CourtRoom = cells[5],
                                SubDivision = cells[6],
                                County = Description,
                                Circuit = Alias
                            };

                            judges.Add(judge);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Error(ex.StackTrace);
                    }
                }

                var next = driver.FindElement(By.XPath("//a[@id='slsTbl_ltr_next']"));
                search = !next.GetAttribute("class").Contains("disabled");
                next.Click();
            }
        }
    }
}
