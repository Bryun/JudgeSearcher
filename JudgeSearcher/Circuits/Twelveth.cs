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
using System.Threading.Tasks;

namespace JudgeSearcher.Circuits
{
    internal class Twelveth : Base
    {

        public Twelveth() : base()
        {
            Status = 1;
        }

        public override string Alias => "Twelveth";

        public override string Description => "DeSoto, Manatee, and Sarasota";

        public override string URL => "http://www.jud12.flcourts.org/";

        public override Task<string> Execute()
        {
            collection = new ObservableCollection<Judge>();

            _ = Scraper.Scan(URL, (driver, wait) =>
            {

                try
                {
                    var action = new Actions(driver);

                    action.MoveToElement(driver.FindElement(By.XPath("//a[contains(text(), 'About the Court')]"))).Perform();
                    action.MoveToElement(driver.FindElement(By.XPath("//a[contains(text(), 'Judges / Magistrates')]"))).Perform();
                    action.MoveToElement(driver.FindElement(By.XPath("//a[contains(text(), 'Printable Judicial Listing')]"))).Click().Perform();

                    wait.Until((e) => By.XPath("//h1[contains(text(), '12th JUDICIAL CIRCUIT COURT TELEPHONE LIST')]"));

                    foreach (var header in driver.FindElements(By.TagName("h3")))
                    {
                        var list = header.FindElements(By.XPath("./following-sibling::*")).TakeWhile(e => e.TagName.Equals("div")).ToList().Select(e =>
                        {
                            Judge judge = null;

                            try
                            {
                                var content = e.Text.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                                var name = content[0].Substring(0, content[0].IndexOf("-")).Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                                var division = content[0].Substring(content[0].IndexOf("-") + 1);
                                judge = new Judge()
                                {
                                    LastName = name[0],
                                    FirstName = name.Length > 1 ? name[1] : name[0],
                                    Type = division.Contains("Circuit") ? "Circuit" : division.Contains("County") ? "County" : division,
                                    SubDivision = content[0].Substring(content[0].IndexOf("-") + 1),
                                    JudicialAssistant = content[1],
                                    Circuit = Alias,
                                    County = header.Text,
                                    Phone = content.Where((f) => f.StartsWith("P:")).First().Phone(),
                                    Location = string.Empty,
                                    CourtRoom = content.Where(e => e.Contains("Courtroom")).FirstOrDefault()
                                };

                                switch (content.Length)
                                {
                                    case 4:
                                        judge.Location = content[3];
                                        break;
                                    case 5:
                                        judge.Location = content[4];
                                        break;
                                    case 6:
                                        judge.Location = content[5];

                                        break;
                                    default:
                                        break;
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Logger.Error(ex.StackTrace);
                            }

                            return judge;

                        }).ToList();

                        list.ForEach(e => collection.Add(e));
                    }

                    driver.Navigate().Back();

                    action.MoveToElement(driver.FindElement(By.XPath("//a[contains(text(), 'About the Court')]"))).Perform();
                    action.MoveToElement(driver.FindElement(By.XPath("//a[contains(text(), 'Courthouses')]"))).Click().Perform();

                    wait.Until(e => By.XPath("//span[contains(text(), 'Courthouse Locations')]"));

                    var hrefs = driver.FindElements(By.XPath("//*[@id=\"dnn_ctr1377_dnnTITLE_titleLabel\"]/../following-sibling::div/descendant::a")).Select(e => e.GetAttribute("href")).ToList();

                    var mapper = new Dictionary<string, string>
                    {
                        { "DESOTO COUNTY COURTHOUSE", "DeSoto County Courthouse" },
                        { "JUDGE LYNN N. SILVERTOOTH JUDICIAL CENTER", "Judge Lynn N. Silvertooth Judicial Center" },
                        { "MANATEE COUNTY JUDICIAL CENTER", "Manatee County Judicial Center" },
                        { "SOUTH SARASOTA COUNTY COURTHOUSE", "South County Courthouse" },
                        { "SARASOTA COUNTY JUSTICE CENTER", string.Empty },
                    };

                    foreach (var href in hrefs)
                    {
                        var xpath = href.Substring(href.IndexOf("/about/"));
                        var court = driver.FindElement(By.XPath(string.Format("//a[@href='{0}']", xpath)));

                        var location = court.Text.Replace("\r\n", " ");

                        court.Click();

                        var t = driver.FindElements(By.XPath("//li")).Where(e => e.Text.StartsWith("Address:")).Select(e => e.Text).FirstOrDefault()!.Address();

                        collection.Where(e => e.Location == mapper[location]).ToList().ForEach(e =>
                        {
                            e.Street = t.FirstOrDefault();
                            e.City = t[Array.IndexOf(t, t.Where(e => Regex.IsMatch(e, "[0-9]{5}")).FirstOrDefault()) - 1];
                            e.Zip = t.Where(e => Regex.IsMatch(e, "[0-9]{5}")).FirstOrDefault();
                        });

                        driver.Navigate().Back();
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
