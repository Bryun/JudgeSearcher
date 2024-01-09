using JudgeSearcher.Models;
using JudgeSearcher.Utility;
using OpenQA.Selenium;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JudgeSearcher.Circuits
{
    internal class Thirteenth : Base
    {
        public Thirteenth() : base()
        {
            Status = 1;
        }

        public override string Alias => "Thirteenth";

        public override string Description => "Hillsborough";

        public override string URL => "http://www.fljud13.org/";

        public override Task<string> Execute()
        {
            collection = new ObservableCollection<Judge>();

            _ = Scraper.Scan(URL, (driver, wait) =>
            {
                driver.FindElement(By.XPath("//li/a[contains(text(), 'Judicial Directory')]")).Click();

                wait.Until((e) => By.LinkText("JUDICIAL DIRECTORY"));

                try
                {
                    string county = string.Empty;

                    foreach (var row in driver.FindElement(By.XPath("//table[1]")).FindElements(By.XPath("//tbody/tr")))
                    {
                        if (row.FindElements(By.TagName("td")).Count > 0)
                        {
                            var cells = row.FindElements(By.TagName("td"));
                            var exclusion = new string[] { "COUNTY COURTS", "CIRCUIT COURTS", "DIVISION" };
                            var indexes = new int[] { 6, 7, 8 };

                            //Log.Logger.Error(string.Join("|", cells.Select(e => e.Text).ToArray()));

                            switch (cells[0].Text)
                            {
                                case "COUNTY COURTS":
                                    county = "County";
                                    break;
                                case "CIRCUIT COURTS":
                                    county = "Circuit";
                                    break;
                                default:
                                    break;
                            }

                            if (indexes.Contains(cells.Count()) && !exclusion.Contains(cells[0].Text) && !string.IsNullOrEmpty(cells[3].Text))
                            {
                                try
                                {
                                    Judge judge = new Judge()
                                    {
                                        Circuit = Alias,
                                        County = Description,
                                        Type = county
                                    };

                                    string xpath = string.Empty;

                                    switch (cells.Count())
                                    {
                                        case 8:
                                            judge.SubDivision = cells[1].Text;
                                            judge.ID = cells[2].FindElements(By.TagName("a")).FirstOrDefault() != null ? cells[2].FindElements(By.TagName("a")).FirstOrDefault().XPath(identify: "/JudicialDirectory/") : string.Empty;
                                            judge.LastName = cells[2].Text.Split(", ")[0];
                                            judge.FirstName = cells[2].Text.Split(", ").Length > 1 ? cells[2].Text.Split(", ")[1] : string.Empty;
                                            judge.Street = cells[3].Text;
                                            judge.CourtRoom = cells[4].Text;
                                            judge.Phone = cells[5].Text;
                                            judge.JudicialAssistant = cells[6].Text;
                                            break;
                                        case 7:
                                            judge.SubDivision = cells[0].Text;
                                            judge.ID = cells[2].FindElements(By.TagName("a")).FirstOrDefault() != null ? cells[2].FindElements(By.TagName("a")).FirstOrDefault().XPath(identify: "/JudicialDirectory/") : string.Empty;
                                            judge.LastName = cells[2].Text.Split(", ")[0];
                                            judge.FirstName = cells[2].Text.Split(", ").Length > 1 ? cells[2].Text.Split(", ")[1] : string.Empty;
                                            judge.Street = cells[3].Text;
                                            judge.CourtRoom = cells[4].Text;
                                            judge.Phone = cells[5].Text;
                                            judge.JudicialAssistant = cells[6].Text;
                                            break;
                                        default:
                                            judge.ID = cells[1].FindElements(By.TagName("a")).FirstOrDefault() != null ? cells[1].FindElements(By.TagName("a")).FirstOrDefault().XPath(identify: "/JudicialDirectory/") : string.Empty;
                                            judge.LastName = cells[1].Text.Split(", ").FirstOrDefault();
                                            judge.FirstName = cells[1].Text.Split(", ").LastOrDefault();
                                            judge.Street = cells[2].Text;
                                            judge.CourtRoom = cells[3].Text;
                                            judge.Phone = cells[4].Text;
                                            judge.JudicialAssistant = cells[5].Text;
                                            break;
                                    }

                                    if (!judge.FirstName.Trim().Equals(string.Empty))
                                        collection.Add(judge);
                                }
                                catch (Exception ex)
                                {
                                    Log.Logger.Error(ex.StackTrace);
                                }

                            }
                        }
                    }

                    collection.ToList().ForEach(e =>
                    {
                        try
                        {
                            //Log.Logger.Error(e.ID);

                            if (driver.FindElements(By.XPath(e.ID)).Count > 0)
                            {
                                IJavaScriptExecutor executor = (IJavaScriptExecutor)driver;
                                executor.ExecuteScript("arguments[0].click();", driver.FindElement(By.XPath(e.ID)));

                                e.ID = string.Empty;

                                if (driver.WindowHandles.Count > 1)
                                {
                                    driver.SwitchTo().Window(driver.WindowHandles.Last());
                                }

                                wait.Until(e => By.XPath("//a[contains(text(), 'Contact')]"));

                                driver.FindElement(By.XPath("//a[contains(text(), 'Contact')]")).Click();

                                var detail_xpath = "//span[contains(text(), 'Contact')]/../../following-sibling::div";

                                if (driver.FindElements(By.XPath(detail_xpath)).Count > 0)
                                {
                                    var lines = driver.FindElement(By.XPath(detail_xpath)).Text.Split(new string[] { "\r\n", ", Florida ", ", FL ", ",", "- " }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

                                    lines.Remove(lines.Where(e => e.Contains("Room")).FirstOrDefault() ?? string.Empty);

                                    e.Street = lines[1];
                                    e.Zip = lines.Where(e => Regex.IsMatch(e, "[0-9]{5}")).FirstOrDefault();
                                    e.City = lines[lines.IndexOf(e.Zip) - 1];
                                    e.Location = lines[lines.IndexOf(e.City) - 1];
                                }

                                if (driver.WindowHandles.Count > 1)
                                {
                                    driver.SwitchTo().Window(driver.WindowHandles.Last()).Close();
                                    driver.SwitchTo().Window(driver.WindowHandles.First());
                                }
                                else
                                {
                                    driver.Navigate().Back();
                                    driver.Navigate().Back();
                                }
                            }
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
    }
}
