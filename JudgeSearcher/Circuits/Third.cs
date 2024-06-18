using JudgeSearcher.Models;
using JudgeSearcher.Utility;
using OpenQA.Selenium;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JudgeSearcher.Circuits
{
    internal class Third : Base
    {
        public Third() : base()
        {
            Status = 1;
        }

        public override string Alias => "Third";

        public override string Description => "Columbia, Dixie, Hamilton, Lafayette, Madison, Suwannee and Taylor";

        public override string URL => "https://thirdcircuitfl.org/judges-page/"; //"http://www.jud3.flcourts.org/";

        private Dictionary<string, string> Address(string line)
        {
            var collection = line.Split("\r\n");
            var inclusions = new string[] { ", FL.", ", Fl.", ", FL" };
            var map = new Dictionary<string, string>() { { "Street", collection[0] } };

            foreach (string include in inclusions)
            {
                if (collection.Any((e) => e.Contains(include)))
                {
                    var context = collection.Where((e) => e.Contains(include)).First();
                    map["City"] = context.Substring(0, context.IndexOf(include));
                    map["Zip"] = context.Substring(context.IndexOf(include) + include.Length);
                    break;
                }
            }

            if (collection.Any((e) => e.Contains("Room")))
            {
                var room = collection.Where((e) => e.Contains("Room")).First();
                map["Room"] = room.Substring(room.IndexOf("Room"));
            }

            return map;
        }

        public override Task<string> Execute()
        {
            judges = new ObservableCollection<Judge>();

            _ = Scraper.Scan(URL, (driver, wait) =>
            {

                driver.FindElement(By.XPath("//a[contains(text(), 'Directory')]")).Click();

                wait.Until((e) => By.XPath("//div[@id='tag_cloud-3']/div/a"));

                var level1hrefs = driver.FindElements(By.XPath("//div[@id='tag_cloud-3']/div/a")).Select((e) => e.GetAttribute("href")).ToList();

                foreach (var level1href in level1hrefs)
                {
                    try
                    {
                        var element = driver.FindElement(By.XPath(string.Format("//a[@href='{0}']", level1href)));
                        var county = element.Text.Substring(0, element.Text.IndexOf("COUNTY") + 6);
                        element.Click();

                        wait.Until((e) => By.XPath("//div[@class='blog-entry-readmore clr']/a"));

                        var level2hrefs = driver.FindElements(By.XPath("//div[@class='blog-entry-readmore clr']/a")).Select((e) => e.GetAttribute("href")).ToList();

                        foreach (var level2href in level2hrefs)
                        {
                            try
                            {
                                driver.FindElement(By.XPath(string.Format("//a[@href='{0}']", level2href))).Click();

                                wait.Until((e) => By.XPath("//*[@id='content']/div/section[1]/div/div/div/div/div/h1"));

                                var full_name = driver.FindElement(By.XPath("//*[@id='content']/div/section[1]/div/div/div/div/div/h1")).Text;
                                full_name = full_name.Replace("Judge ", string.Empty);

                                var _type = driver.FindElement(By.XPath("//*[@id='content']/div/section[2]/div/div[1]/div/div[2]/div")).Text.Split(Environment.NewLine).Where(e => Regex.IsMatch(e, "County Judge.+Present$|Circuit Judge.+Present$")).FirstOrDefault();

                                var surname = full_name.EndsWith("Jr.") ? string.Join(" ", full_name.Split(" ").TakeLast(2)) : full_name.Substring(full_name.LastIndexOf(" ")).Trim();
                                var name = full_name.Replace(surname, string.Empty);

                                var address = Address(driver.FindElement(By.XPath("//*[@id='content']/div/section[2]/div/div[2]/div/section[2]/div/div[2]/div/div[2]/div")).Text);

                                var phone = driver.FindElement(By.XPath("//*[@id='content']/div/section[2]/div/div[2]/div/section[2]/div/div[2]/div/div[3]/div")).Text;
                                phone = phone.Replace("Phone: ", string.Empty);

                                var assistant = driver.FindElement(By.XPath("//*[@id='content']/div/section[2]/div/div[2]/div/section[2]/div/div[2]/div/div[5]/div")).Text;
                                assistant = assistant.Replace("Judicial Assistant: ", string.Empty);

                                Judge judge = new Judge()
                                {
                                    County = county,
                                    Type = Regex.Match(_type, "County Judge|Circuit Judge").Value,
                                    Circuit = Alias,
                                    LastName = surname,
                                    FirstName = name,
                                    Phone = phone,
                                    JudicialAssistant = assistant,
                                    Street = address["Street"],
                                    City = address["City"],
                                    Zip = address["Zip"],
                                    CourtRoom = address.Keys.Contains("Room") ? address["Room"] : string.Empty
                                };

                                judges.Add(judge);
                            }
                            catch (Exception ex)
                            {
                                Log.Logger.Error(ex.StackTrace);
                            }

                            driver.Navigate().Back();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Error(ex.StackTrace);
                    }
                }

                driver.Navigate().Back();
            });

            return base.Execute();
        }
    }
}
