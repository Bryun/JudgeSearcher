using HtmlAgilityPack;
using JudgeSearcher.Models;
using JudgeSearcher.Utility;
using OpenQA.Selenium;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace JudgeSearcher.Circuits
{
    internal class Twentienth : Base
    {
        public Twentienth() : base()
        {
            Status = 1;
        }

        public override string Alias => "Twentienth";

        public override string Description => "Charlotte, Collier, Glades, Hendry and Lee";

        public override string URL => "https://www.ca.cjis20.org/About-The-Court/judiciary.aspx"; //"http://www.ca.cjis20.org/home/main/homepage.asp";

        public override Task<string> Execute()
        {
            collection = new ObservableCollection<Judge>();

            _ = Scraper.Scan(URL, (driver, wait) =>
            {
                try
                {
                    wait.Until((e) => By.XPath("///head/title[contains(text(), 'Judges, Magistrates & Hearing Officers')]"));

                    var tags = driver.FindElements(By.XPath("//div[@class='judgelist']/div/ul/li/div/div/a")).Select(e => e.GetAttribute("href")).ToArray();

                    foreach (var tag in tags)
                    {
                        Judge judge = new Judge()
                        {
                            Circuit = Alias
                        };

                        var xpath = tag.Substring(tag.IndexOf("/About-The-Court"));

                        var a = driver.FindElement(By.XPath(string.Format("//a[@href='{0}']", xpath)));

                        judge.Type = a.FindElement(By.XPath("./div/p")).Text.Split("\r\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? string.Empty;

                        IJavaScriptExecutor executor = (IJavaScriptExecutor)driver;
                        executor.ExecuteScript("arguments[0].click();", a);

                        wait.Until((e) => By.XPath("//h1[contains(text(), 'Judicial Profile')]"));


                        if (string.IsNullOrEmpty(judge.Type) && driver.FindElements(By.XPath("//*[@id='ctl00_ContentPlaceHolder1_jud_page_content']/div[1]/div[2]/h3")).Count > 0)
                            judge.Type = driver.FindElement(By.XPath("//*[@id='ctl00_ContentPlaceHolder1_jud_page_content']/div[1]/div[2]/h3")).Text;

                        var document = new HtmlWeb().Load(driver.Url);

                        var context = document.DocumentNode.SelectSingleNode("//div[@id='ctl00_ContentPlaceHolder1_jud_page_content']");

                        var name = context.Descendants("h2").First().InnerText;
                        name = name.Replace("The Honorable ", string.Empty).Replace("Chief Judge ", string.Empty);

                        judge.LastName = name.Substring(name.LastIndexOf(" ")).Trim();
                        judge.FirstName = name.Substring(0, name.LastIndexOf(" ")).Trim();

                        var content = context.Descendants("p").Select(e => e.InnerText.Split("\r\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)).SelectMany(e => e).ToList();

                        content = content.GetRange(0, content.IndexOf("Mailing Address") + 4);

                        var county = content.Where(e => e.StartsWith("Division")).First().Replace("Division", string.Empty).Trim();

                        judge.SubDivision = county.ToLower().Contains("county") ? string.Empty : county;
                        judge.County = county.ToLower().Contains("county") ? county : string.Empty;
                        judge.JudicialAssistant = content.Where(e => e.Contains("Assistant")).First().Replace("Judicial Assistant ", string.Empty).Replace("Assistant", string.Empty).Trim();
                        judge.Phone = content.Where(e => e.StartsWith("Phone")).First().Replace("Phone", string.Empty).Trim();

                        var address = string.Join("\r\n", content.GetRange(content.IndexOf("Mailing Address") + 1, 3)).Address().ToList();

                        judge.Zip = address.Where(e => Regex.IsMatch(e, "[0-9]{5}")).FirstOrDefault();
                        judge.City = address.IndexOf(judge.Zip) - 1 >= 0 ? address[address.IndexOf(judge.Zip) - 1] : string.Empty;
                        judge.Street = address.IndexOf(judge.City) - 1 >= 0 ? address[address.IndexOf(judge.City) - 1] : string.Empty;
                        judge.Location = address.FirstOrDefault().Equals(judge.Street) ? string.Empty : address.FirstOrDefault();

                        if (string.IsNullOrEmpty(judge.County) && !string.IsNullOrEmpty(judge.Location) && judge.Location.Contains("County"))
                            judge.County = judge.Location.Substring(0, judge.Location.IndexOf("County") + 6);

                        collection.Add(judge);

                        Thread.Sleep(TimeSpan.FromSeconds(2));

                        driver.Navigate().Back();
                    }
                }
                catch (Exception ex)
                {
                    Log.Logger.Error(ex.Message);
                }

            }, allowImages: true);

            return base.Execute();
        }
    }
}
