using JudgeSearcher.Models;
using JudgeSearcher.Utility;
using OpenQA.Selenium;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
                    foreach (var row in driver.FindElement(By.XPath("//table[1]")).FindElements(By.XPath("//tbody/tr")))
                    {
                        if (row.FindElements(By.TagName("td")).Count > 0)
                        {
                            var cells = row.FindElements(By.TagName("td"));
                            var values = row.FindElements(By.TagName("td")).Select(e => e.Text).ToList();
                            var exclusion = new string[] { "COUNTY COURTS", "CIRCUIT COURTS", "DIVISION" };
                            var indexes = new int[] { 6, 7, 8 };
                            var id = string.Empty;

                            if (row.FindElements(By.TagName("a")).Count > 0)
                            {
                                id = row.FindElement(By.TagName("a")).XPath(identify: "/JudicialDirectory/");
                            }

                            if (indexes.Contains(cells.Count()) && !exclusion.Contains(values[0]) && !string.IsNullOrEmpty(values[3]) && !values.All(e => string.IsNullOrEmpty(e.Trim())))
                            {
                                try
                                {
                                    Judge judge = new Judge()
                                    {
                                        Circuit = Alias,
                                        County = Description,
                                        Type = values[0] == "COUNTY COURTS" ? "County" : "Circuit"
                                    };

                                    int idx = values.FindIndex(e => Regex.IsMatch(e, "\\d{3}-\\d{3}-\\d{4}"));

                                    if (values.Count() == 8) 
                                    {
                                        judge.SubDivision = values[idx - 4];
                                        judge.ID = id;
                                        judge.LastName = values[idx - 3].Split(", ")[0];
                                        judge.FirstName = values[idx - 3].Split(", ").Length > 1 ? values[idx - 3].Split(", ")[1] : string.Empty;
                                        judge.Street = values[idx - 2];
                                        judge.CourtRoom = values[idx - 1];
                                        judge.Phone = values[idx];
                                        judge.JudicialAssistant = values[idx + 1];
                                    }
                                    else if (values.Count() == 7) 
                                    {
                                        judge.SubDivision = values[idx - 5];
                                        judge.ID = id;
                                        judge.LastName = values[idx - 3].Split(", ")[0];
                                        judge.FirstName = values[idx - 3].Split(", ").Length > 1 ? values[idx - 3].Split(", ")[1] : string.Empty;
                                        judge.Street = values[idx - 2];
                                        judge.CourtRoom = values[idx - 1];
                                        judge.Phone = values[idx];
                                        judge.JudicialAssistant = values[idx + 1];
                                    }
                                    else 
                                    {
                                        judge.ID = id;
                                        judge.LastName = values[idx - 3].Split(", ").FirstOrDefault();
                                        judge.FirstName = values[idx - 3].Split(", ").LastOrDefault();
                                        judge.Street = values[idx - 2];
                                        judge.CourtRoom = values[idx - 1];
                                        judge.Phone = values[idx];
                                        judge.JudicialAssistant = values[idx + 1];
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
                            if (!string.IsNullOrEmpty(e.ID) && driver.FindElements(By.XPath(e.ID)).Count > 0)
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
            }, timeout: 10);

            return base.Execute();
        }
    }
}
