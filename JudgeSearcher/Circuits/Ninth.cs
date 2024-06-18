using JudgeSearcher.Models;
using JudgeSearcher.Utility;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JudgeSearcher.Circuits
{
    internal class Ninth : Base
    {

        public Ninth() : base()
        {
            Status = 1;
        }

        public override string Alias => "Ninth";

        public override string Description => "Orange and Osceola";

        public override string URL => "http://www.ninthcircuit.org/";

        public override Task<string> Execute()
        {
            judges = new ObservableCollection<Judge>();

            _ = Scraper.Scan(URL, (driver, wait) =>
            {
                driver.FindElement(By.XPath("//*[@id='block-ninth-main-menu']/ul/li[2]/a")).Click();

                wait.Until((e) => By.XPath("//h1[contains(text(), 'Judges')]"));

                foreach (var card in driver.FindElements(By.XPath("//div[@class = 'judge card full-height']")))
                {
                    Judge judge = new Judge()
                    {
                        Circuit = Alias
                    };

                    var name = card.FindElement(By.ClassName("judge__name")).Text;

                    judge.LastName = name.Substring(name.LastIndexOf(" "));
                    judge.FirstName = name.Replace(judge.LastName, string.Empty);

                    var _type = card.FindElement(By.ClassName("judge__type")).Text;

                    judge.Type = _type.Contains("County") ? "County" : _type;

                    var phone = card.FindElement(By.ClassName("judge__info")).Text;

                    judge.Phone = phone.Substring(phone.LastIndexOf(" ")).Replace(".", " ");

                    var lines = card.FindElement(By.ClassName("middle")).Text.Split("\r\n");

                    judge.Location = lines[0].Replace("Chamber ", string.Empty);
                    judge.County = lines[0].Contains("County") ? Regex.Replace(lines[0], " Courthouse|Jon B. Morgan |Chamber ", string.Empty) : string.Empty;
                    judge.CourtRoom = lines[1].Replace("Courtroom ", string.Empty);
                    judge.HearingRoom = lines[2].Replace("Sub Division ", string.Empty);
                    judge.SubDivision = lines[3].Replace("Assignment ", string.Empty);
                    judge.JudicialAssistant = lines[4].Replace("Judicial Assistant ", string.Empty);

                    judges.Add(judge);
                }

                driver.FindElement(By.XPath("//a[contains(text(), 'Court Directory')]")).Click();

                var locations = new string[] { "Orange County Courthouse", "Jon B. Morgan Osceola County Courthouse" };

                foreach (var location in locations)
                {
                    var map = new Dictionary<string, string>();

                    var element = driver.FindElement(By.XPath(string.Format("//h5[contains(text(), '{0}')]", location)));

                    var address = element.FindElement(By.XPath("./following-sibling::div[3]")).Text;

                    var list = Regex.Split(address, "\\r\\n|,");

                    judges.Where(e => e.Location == element.Text).ToList().ForEach(e =>
                    {
                        e.Street = list[0];
                        e.City = list[1];
                        e.Zip = list[2].Replace("Florida", string.Empty).Trim();
                    });
                }

                driver.FindElement(By.XPath("//a[contains(text(), 'Thomas S. Kirk Juvenile Justice Center')]")).Click();

                wait.Until(e => By.XPath("//span[contains(text(), 'Thomas S. Kirk Juvenile Justice Center')]"));

                var _address = driver.FindElement(By.XPath("//h6[contains(text(), 'Address')]/following-sibling::p")).Text;

                var _list = string.Join("|", Regex.Split(_address, "\\r\\n|,")).Split("|", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                judges.Where(e => e.Location == "Thomas S. Kirk Juvenile Justice Center").ToList().ForEach(e =>
                {
                    e.Street = _list[0];
                    e.City = _list[1];
                    e.Zip = _list[2].Replace("Florida", string.Empty).Trim();
                });
            });

            return base.Execute();
        }
    }
}
