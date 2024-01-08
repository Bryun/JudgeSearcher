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
    public class First : Base
    {
        public First() : base()
        {
            Status = 1;
        }

        #region Properties

        public override string Alias => "First";

        public override string Description => "Escambia, Okaloosa, Santa Rosa and Walton";

        public override string URL => "http://www.firstjudicialcircuit.org/";

        #endregion

        #region Methods

        public override Task<string> Execute()
        {
            collection = new ObservableCollection<Judge>();

            _ = Scraper.Scan(URL, (driver, wait) =>
            {
                driver.FindElement(By.LinkText("Judges")).Click();

                wait.Until(e => By.LinkText("All Judges"));
                driver.FindElement(By.LinkText("All Judges")).Click();

                wait.Until(e => By.XPath("//h1[contains(text(), 'All Judges')]"));
                var rows = driver.FindElements(By.XPath("//*[@id='block-system-main']/div/div/div/table/tbody/tr"));

                foreach (var tr in rows)
                {
                    try
                    {
                        var td = tr.FindElements(By.TagName("td"));
                        Judge judge = new Judge()
                        {
                            LastName = td[0].Text.Trim(),
                            FirstName = td[1].Text.Trim(),
                            Circuit = Alias
                        };

                        tr.FindElement(By.TagName("a")).Click();

                        wait.Until(e => By.XPath("//article/div"));

                        judge.Type = driver.FindElement(By.XPath("//div[starts-with(@class, 'field field-name-field-judge-type')]")).Text.Type();
                        judge.County = driver.FindElement(By.XPath("//div[starts-with(@class, 'field field-name-field-judge-county')]")).Text.County();
                        judge.SubDivision = driver.FindElement(By.XPath("//div[starts-with(@class, 'field field-name-field-judge-division')]")).Text.Division();
                        //driver.FindElement(By.XPath("//div[starts-with(@class, 'field field-name-field-judge-jurisdiction')]")).Text;
                        judge.JudicialAssistant = driver.FindElement(By.XPath("//div[starts-with(@class, 'field field-name-field-judge-assistant')]")).Text.Assistant();
                        judge.Phone = driver.FindElement(By.XPath("//div[starts-with(@class, 'field field-name-field-judge-phone')]")).Text.Phone();

                        string[] address = null;

                        if (driver.FindElements(By.XPath("//div[starts-with(@class, 'field field-name-field-judge-address')]")).Count > 0)
                            address = driver.FindElement(By.XPath("//div[starts-with(@class, 'field field-name-field-judge-address')]")).Text.Address();
                        else if (driver.FindElements(By.XPath("//*[@id=\"node-165\"]/div[7]/div/div/p[2]")).Count > 0)
                            address = driver.FindElement(By.XPath("//*[@id=\"node-165\"]/div[7]/div/div/p[2]")).Text.Address();

                        judge.Location = address.FirstOrDefault();
                        judge.CourtRoom = address.Where(e => Regex.IsMatch(e, "Floor")).FirstOrDefault();
                        judge.Zip = address.Where(e => Regex.IsMatch(e, "[0-9]{5}")).FirstOrDefault()!;
                        judge.City = address[Array.IndexOf(address, judge.Zip) - 1];
                        judge.Street = address.Where(e => !new string[] { judge.Location, judge.CourtRoom, judge.City, judge.Zip }.Contains(e)).FirstOrDefault();

                        collection.Add(judge);
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Error(ex.StackTrace);
                    }

                    driver.Navigate().Back();
                }
            });

            return base.Execute();

        }

        #endregion

    }
}
