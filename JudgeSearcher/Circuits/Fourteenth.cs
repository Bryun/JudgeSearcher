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
    internal class Fourteenth : Base
    {
        #region Declaration

        public Fourteenth() : base()
        {
            Status = 1;
        }

        #endregion

        public override string Alias => "Fourteenth";

        public override string Description => "Bay, Calhoun, Gulf, Holmes, Jackson and Washington";

        public override string URL => "https://www.jud14.flcourts.org/judges";

        public override Task<string> Execute()
        {
            judges = new ObservableCollection<Judge>();

            _ = Scraper.Scan(URL, (driver, wait) =>
            {

                var h3s = driver.FindElements(By.XPath("//article[@id='node-2']//h3"));

                foreach (var h3 in h3s)
                {
                    if (!h3.Text.Trim().Equals("Senior Judges"))
                    {
                        var lines = h3.FindElement(By.XPath("./following-sibling::table")).FindElements(By.XPath("./tbody/tr"));
                        var rows = lines.TakeLast(lines.Count - 1);

                        rows.ToList().ForEach(row =>
                        {
                            try
                            {
                                var cells = row.FindElements(By.TagName("td"));
                                var names = cells.FirstOrDefault()!.Text.FullName();
                                string href = string.Empty;

                                Judge judge = null;

                                if (cells.Count == 5)
                                {

                                    if (cells[0].FindElements(By.TagName("a")).Count > 0)
                                    {
                                        href = cells[0].FindElement(By.TagName("a")).XPath("/judges/");
                                    }

                                    judge = new Judge()
                                    {
                                        ID = href,
                                        Type = string.IsNullOrEmpty(h3.Text.Trim()) ? "Circuit Judges" : h3.Text,
                                        FirstName = names.FirstOrDefault(),
                                        LastName = names.LastOrDefault(),
                                        JudicialAssistant = cells[1].Text,
                                        County = cells[2].Text,
                                        Phone = cells[3].Text,
                                        Circuit = Alias,
                                    };
                                }
                                else if (cells.Count == 3)
                                {
                                    judge = judges.LastOrDefault().Clone() as Judge;
                                    judge.County = cells[0].Text;
                                    judge.Phone = cells[1].Text;
                                }


                                judges.Add(judge);
                            }
                            catch (Exception ex)
                            {
                                Log.Logger.Error(ex.StackTrace);
                            }
                        });
                    }
                }

                judges.Where(e => !string.IsNullOrEmpty(e.ID)).ToList().ForEach(e =>
                {
                    try
                    {
                        if (driver.FindElements(By.XPath(e.ID)).Count > 0)
                        {
                            driver.FindElement(By.XPath(e.ID)).Click();

                            if (driver.FindElements(By.XPath("//*[contains(text(), 'Judicial Assistant:')]")).Count > 0)
                            {
                                var address = driver.FindElement(By.XPath("//article/div/div/div")).Text.Address();

                                e.Zip = address.Where(x => Regex.IsMatch(x, "\\d{5}$")).FirstOrDefault();
                                e.City = address[Array.IndexOf(address, e.Zip) - 1];
                                e.Street = address[Array.IndexOf(address, e.City) - 1];
                                e.Location = address[Array.IndexOf(address, e.Street) - 1];
                            }

                            e.ID = string.Empty;

                            driver.Navigate().Back();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Error(ex.StackTrace);
                        driver.Navigate().Back();
                    }
                });

            });

            return base.Execute();
        }
    }
}
