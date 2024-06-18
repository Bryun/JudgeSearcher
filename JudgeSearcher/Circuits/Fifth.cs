using HtmlAgilityPack;
using JudgeSearcher.Models;
using JudgeSearcher.Utility;
using OpenQA.Selenium;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeSearcher.Circuits
{

    public class Fifth : Base
    {

        public Fifth() : base()
        {
            Status = 1;
        }

        public override string Alias => "Fifth";

        public override string Description => "Citrus, Hernando, Lake, Marion and Sumter";

        public override string URL => "http://www.circuit5.org/";

        public override Task<string> Execute()
        {
            judges = new ObservableCollection<Judge>();

            _ = Scraper.Scan(URL, (driver, wait) =>
            {
                try
                {


                    driver.FindElement(By.Id("menu-item-46")).Click();

                    wait.Until(e => By.XPath("//h1[text()='Courts & Judges']"));

                    var document = new HtmlWeb().Load(driver.Url);

                    var paths = document.DocumentNode.SelectNodes("//div[@class='box-holder']").Where((e) => e.NodeType == HtmlNodeType.Element).Select((e) => e.XPath).ToList();

                    foreach (var xPath in paths)
                    {
                        var box = driver.FindElement(By.XPath(xPath));

                        var location = box.FindElement(By.TagName("h4")).Text;
                        var contacts = box.FindElements(By.TagName("a"));
                        var address = contacts[0].Text.Split("\r\n");
                        var street = address[0];
                        var city = address[1].Substring(0, address[1].IndexOf(",")).Trim();
                        var zip = address[1].Substring(address[1].LastIndexOf(" ")).Trim();
                        var phone = contacts[1].Text;

                        box.FindElement(By.XPath("./div[starts-with(@id, 'mk-button-')]")).Click();

                        wait.Until(e => By.XPath("./h3[text()='The Judiciary']"));

                        foreach (var name in driver.FindElement(By.CssSelector("#text-block-13,#text-block-15")).FindElements(By.TagName("a")))
                        {
                            Judge judge = new Judge()
                            {
                                Circuit = Alias,
                                Location = location,
                                County = location.Replace(" Courthouse", string.Empty),
                                Street = street,
                                City = city,
                                Zip = zip,
                                Phone = phone,
                                LastName = name.Text.Substring(name.Text.LastIndexOf(" ")).Trim(),
                                FirstName = name.Text.Substring(0, name.Text.LastIndexOf(" ")).Trim(),
                            };

                            name.Click();

                            wait.Until(e => By.LinkText("Office Information"));

                            var type = driver.FindElement(By.Id("text-block-3"));

                            judge.Type = type != null ? type.Text.Split("\r\n")[1] : string.Empty;

                            var line = driver.FindElement(By.Id("text-block-5")).Text.Split("\r\n")[0];

                            judge.JudicialAssistant = line.Substring(line.IndexOf(":")).Replace(": ", string.Empty);

                            judges.Add(judge);

                            driver.Navigate().Back();
                            wait.Until(e => By.XPath("//h3[text()='The Judiciary']"));
                        }

                        driver.Navigate().Back();
                        wait.Until(e => By.XPath("//h1[text()='Courts & Judges']"));
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
