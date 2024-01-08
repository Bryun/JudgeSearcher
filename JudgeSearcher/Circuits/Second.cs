using HtmlAgilityPack;
using JudgeSearcher.Models;
using JudgeSearcher.Utility;
using OpenQA.Selenium;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeSearcher.Circuits
{
    public class Second : Base
    {

        public Second() : base()
        {
            Status = 1;
        }

        public override string Alias => "Second";

        public override string Description => "Franklin, Gadsden, Jefferson, Leon, Liberty, and Wakulla";

        public override string URL => "http://2ndcircuit.leoncountyfl.gov/";

        public override Task<string> Execute()
        {
            collection = new ObservableCollection<Judge>();

            _ = Scraper.Scan(URL, (driver, wait) =>
            {
                driver.FindElement(By.LinkText("CIRCUIT JUDGES")).Click();

                wait.Until(e => By.LinkText("Judicial Directory"));
                driver.FindElement(By.LinkText("Judicial Directory")).Click();

                wait.Until(e => By.XPath("//table/tbody"));

                var document = new HtmlWeb().Load(driver.Url);

                foreach (var body in document.DocumentNode.SelectNodes("//table/tbody"))
                {
                    var cells = body.Descendants("td").ToList();

                    var judge = new Judge()
                    {
                        LastName = cells[0].InnerText,
                        FirstName = cells[1].InnerText,
                        Type = cells[2].InnerText,
                        JudicialAssistant = cells[3].InnerText,
                        Phone = cells[5].InnerText,
                        Circuit = Alias
                    };

                    var context = cells[4].OuterHtml;

                    while (context.Contains("</a>"))
                    {
                        var splice = context.Substring(context.IndexOf("<a"), (context.IndexOf("</a>") + 4) - context.IndexOf("<a"));
                        context = context.Replace(splice, string.Empty);
                    }

                    context = context.Replace("<td>", string.Empty).Replace("</td>", string.Empty);

                    var address = context.Split("<br>", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                    judge.Location = address[1].Trim();
                    judge.Street = address[0].Trim();
                    judge.City = address[2].Trim().Substring(0, address[2].Trim().IndexOf(","));
                    judge.Zip = address[2].Substring(address[2].LastIndexOf(" ")).Trim();

                    collection.Add(judge);
                }

                driver.FindElement(By.LinkText("COUNTY JUDGES")).Click();

                wait.Until(e => By.LinkText("Judicial Directory"));
                driver.FindElement(By.LinkText("Judicial Directory")).Click();

                wait.Until(e => By.XPath("//table/tbody"));

                document = new HtmlWeb().Load(driver.Url);

                foreach (var body in document.DocumentNode.SelectNodes("//table/tbody"))
                {
                    var cells = body.Descendants("td").ToList();

                    var judge = new Judge()
                    {
                        LastName = cells[0].InnerText,
                        FirstName = cells[1].InnerText,
                        Type = cells[2].InnerText,
                        JudicialAssistant = cells[3].InnerText,
                        Phone = cells[5].InnerText,
                        Circuit = Alias
                    };

                    if (judge.Type.Contains("County"))
                    {
                        judge.County = judge.Type.Substring(0, judge.Type.IndexOf(" Judge"));
                        judge.Type = judge.Type.Substring(judge.Type.IndexOf("County"));
                    }

                    var context = cells[4].OuterHtml;

                    while (context.Contains("</a>"))
                    {
                        var splice = context.Substring(context.IndexOf("<a"), (context.IndexOf("</a>") + 4) - context.IndexOf("<a"));
                        context = context.Replace(splice, string.Empty);
                    }

                    context = context.Replace("<td>", string.Empty).Replace("</td>", string.Empty);

                    var address = context.Split("<br>", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                    judge.Location = address[1].Trim();
                    judge.Street = address[0].Trim();
                    judge.City = address[2].Trim().Substring(0, address[2].Trim().IndexOf(","));
                    judge.Zip = address[2].Substring(address[2].LastIndexOf(" ")).Trim();

                    collection.Add(judge);
                }
            });

            return base.Execute();
        }
    }
}
