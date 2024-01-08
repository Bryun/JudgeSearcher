using HtmlAgilityPack;
using JudgeSearcher.Models;
using JudgeSearcher.Utility;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace JudgeSearcher.Circuits
{
    internal class Fifteenth : Base
    {
        public Fifteenth() : base()
        {
            Status = 1;
        }

        public override string Alias => "Fifteenth";

        public override string Description => "Palm Beach";

        public override string URL => "https://www.15thcircuit.com/";

        public override Task<string> Execute()
        {
            collection = new ObservableCollection<Judge>();

            _ = Scraper.Scan(URL, (driver, wait) =>
            {
                driver.FindElement(By.LinkText("About the Court")).Click();
                wait.Until((e) => By.XPath("//h1[contains(text(), 'About the Court')]"));

                driver.FindElement(By.XPath("//a[contains(text(), 'Judicial Directory')]")).Click();
                wait.Until((e) => By.XPath("//table/tbody"));


                if (driver.FindElements(By.Id("edit-type-1")).Count > 0)
                {
                    var element = driver.FindElement(By.Id("edit-type-1"));

                    var select = new SelectElement(element);
                    select.SelectByText("Judge");

                    driver.FindElement(By.Id("edit-submit-directory-of-people")).Click();

                    Thread.Sleep(TimeSpan.FromSeconds(2));
                }

                bool search = true;

                while (search)
                {
                    var items = driver.FindElements(By.XPath("//table/tbody/tr"));
                    var batch = items.TakeLast(items.Count - 1).ToArray();

                    foreach (var row in batch)
                    {
                        try
                        {
                            var elements = row.FindElements(By.XPath("./td"));
                            var cells = elements.Select((e) => e.Text).ToList();

                            if (cells.Count == 7 && !Regex.IsMatch(cells.FirstOrDefault(), "Circuit|Vacant"))
                            {
                                var names = cells.FirstOrDefault().FullName();

                                Debug.WriteLine(string.Join(" | ", cells));
                                Debug.WriteLine(string.Join(" ##### ", names));

                                var y = elements.FirstOrDefault().FindElement(By.TagName("a")).HREF("https://www.15thcircuit.com");

                                Judge judge = new Judge()
                                {
                                    ID = elements.FirstOrDefault().FindElement(By.TagName("a")).HREF("https://www.15thcircuit.com"),
                                    LastName = names.LastOrDefault(),
                                    FirstName = names.FirstOrDefault(),
                                    SubDivision = cells[1],
                                    Location = cells[2],
                                    CourtRoom = cells[3],
                                    JudicialAssistant = cells[4],
                                    Phone = cells[5],
                                    HearingRoom = cells[6],
                                    Circuit = Alias,
                                    County = Description
                                };

                                collection.Add(judge);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.StackTrace);
                            Log.Logger.Error(ex.StackTrace);
                        }

                        Debug.WriteLine("test");
                    }

                    collection.Where(e => !string.IsNullOrEmpty(e.ID)).ToList().ForEach(e =>
                    {
                        try
                        {
                            if (driver.FindElements(By.XPath(e.ID)).Count > 0)
                            {
                                driver.FindElement(By.XPath(e.ID)).Click();

                                if (driver.FindElements(By.XPath("//div[@class='views-field views-field-field-court-type']")).Count > 0)
                                {
                                    e.District = driver.FindElement(By.XPath("//div[@class='views-field views-field-field-court-type']")).Text;
                                    e.Type = e.District.Contains("County") ? "County" : "Circuit";
                                    e.District = string.Empty;
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

                    if (driver.FindElements(By.XPath("//a[@title='Go to next page']")).Count > 0)
                    {
                        driver.FindElement(By.XPath("//a[@title='Go to next page']")).Click();
                    }
                    else
                        search = false;
                }

                driver.FindElement(By.LinkText("About the Court")).Click();
                wait.Until((e) => By.XPath("//h1[contains(text(), 'About the Court')]"));

                driver.FindElement(By.XPath("//a[contains(text(), 'Courthouses')]")).Click();
                wait.Until((e) => By.XPath("//h1[contains(text(), 'Courthouses')]"));

                var document = new HtmlWeb().Load(driver.Url);
                var courthouses = document.DocumentNode.SelectSingleNode("/html/body/div/div/div/div/section/div[2]/div/div/div/div");
                var rows = courthouses.SelectNodes("//div[@class='col-md-6']");

                foreach (var courthouse in rows)
                {
                    var location = courthouse.Descendants("h2").First().InnerText;
                    var address = courthouse.Descendants("p").First().InnerText.Address();

                    collection.Where(e => e.Location == location).ToList().ForEach(e =>
                    {
                        e.Zip = address.Where(e => Regex.IsMatch(e, "\\d{5}$")).FirstOrDefault();
                        e.City = address[Array.IndexOf(address, e.Zip) - 1];
                        e.Street = address[Array.IndexOf(address, e.City) - 1]; ;
                    });
                }
            });

            return base.Execute();
        }
    }
}
