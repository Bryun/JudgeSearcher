using HtmlAgilityPack;
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
    internal class Tenth : Base
    {
        public Tenth() : base()
        {
            Status = 1;
        }

        public override string Alias => "Tenth";

        public override string Description => "Hardee, Highlands, and Polk";

        public override string URL => "http://www.jud10.flcourts.org/";

        public override Task<string> Execute()
        {
            collection = new ObservableCollection<Judge>();

            _ = Scraper.Scan(URL, (driver, wait) =>
            {
                driver.FindElement(By.XPath("//a[contains(text(), ' Judges ')]")).Click();

                wait.Until((e) => By.XPath("//h2[contains(text(), 'Primary tabs')]"));

                var web = new HtmlWeb();
                var document = web.Load(driver.Url);

                var menu = document.DocumentNode.SelectNodes("//ul[@class ='tabs primary']/li").Take(3).ToArray();

                foreach (var tab in menu)
                {
                    driver.FindElement(By.XPath(tab.XPath)).Click();

                    var references = driver.FindElements(By.XPath("//div[@class='item-list']/ul/li/h2/a")).Select(e => e.GetAttribute("href")).ToArray();

                    foreach (var reference in references)
                    {
                        try
                        {
                            var xpath = string.Format("//a[@href='{0}']", reference.Replace("https://www.jud10.flcourts.org", string.Empty));

                            if (driver.FindElements(By.XPath(xpath)).Count > 0)
                            {
                                driver.FindElement(By.XPath(xpath)).Click();

                                var fullname = driver.FindElement(By.XPath("//div[starts-with(@class, 'field field-name-title')]")).Text.FullName();
                                var type = driver.FindElement(By.XPath("//div[starts-with(@class, 'field field-name-field-type')]")).Text;
                                var assignment = driver.FindElement(By.XPath("//div[starts-with(@class, 'field field-name-field-assignment')]")).Text.Division();
                                var assistant = driver.FindElement(By.XPath("//div[starts-with(@class, 'field field-name-field-judge-ja')]")).Text.Assistant();
                                var phone = driver.FindElement(By.XPath("//div[starts-with(@class, 'field field-name-field-judge-phone')]")).Text.Phone();
                                var address = string.Format("{0}\r\n{1}", new string[] { "//div[starts-with(@class, 'field field-name-field-address')]", "//div[starts-with(@class, 'field field-name-field-address-city-and-state')]" }.Select(e => driver.FindElements(By.XPath(e)).Count > 0 ? driver.FindElement(By.XPath(e)).Text : string.Empty).ToArray()).Address();

                                Judge judge = new Judge()
                                {
                                    Circuit = Alias,
                                    FirstName = fullname[0],
                                    LastName = fullname[1],
                                    SubDivision = assignment,
                                    JudicialAssistant = assistant,
                                    Phone = phone
                                };

                                if (type.Contains("County"))
                                {
                                    judge.Type = "County";
                                    judge.County = type;
                                }
                                else
                                    judge.Type = type;

                                if (judge.SubDivision.Contains("Polk "))
                                {
                                    judge.County = "Polk County";
                                }
                                else if (judge.SubDivision.Contains("Highlands "))
                                {
                                    judge.County = "Highlands County";
                                }
                                else if (judge.SubDivision.Contains("Hardee "))
                                {
                                    judge.County = "Hardee County";
                                }

                                if (!address.Any(e => e.Contains("P.O. Box ")))
                                {
                                    switch (address.Length)
                                    {
                                        case 3:
                                            judge.Street = address[0];
                                            judge.City = address[1];
                                            judge.Zip = address[2];
                                            break;
                                        case 4:
                                            judge.Location = address[0];
                                            judge.Street = address[1];
                                            judge.City = address[2];
                                            judge.Zip = address[3];
                                            break;
                                        default:
                                            break;
                                    }
                                }

                                collection.Add(judge);
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Logger.Error(e.StackTrace);
                        }

                        driver.Navigate().Back();
                    }
                }

                try
                {
                    driver.FindElement(By.XPath("//a[contains(text(), 'Contacts')]")).Click();

                    wait.Until(e => By.XPath("//h1[contains(text(), 'Contacts')]"));

                    var element = driver.FindElement(By.XPath("//li/a[contains(text(), 'Courthouse Locations')]"));

                    IJavaScriptExecutor executor = (IJavaScriptExecutor)driver;
                    executor.ExecuteScript("arguments[0].click();", element);

                    //driver.FindElements(By.XPath("//div[@class='map-container']//div[@class='map-info']")).Select(e => e.Text).ToList().ForEach(e => Log.Logger.Information(e));

                    foreach (var container in driver.FindElements(By.XPath("//div[@class='map-container']")))
                    {
                        var county = container.FindElement(By.TagName("h2")).Text;
                        var info = Location(container.FindElement(By.XPath("./div[@class='map-info']")).Text);

                        foreach (var e in collection)
                        {
                            if (e.County == county)
                            {
                                e.Location = county;
                                e.Street = info[0];
                                e.City = info[1];
                                e.Zip = info[2];
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

        private string[] Location(string value) => string.Join("|", Regex.Split(value, "\\r\\n|, FL ")).Split("|", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        
    }
}
