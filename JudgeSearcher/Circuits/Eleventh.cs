using JudgeSearcher.Models;
using JudgeSearcher.Utility;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeSearcher.Circuits
{
    internal class Eleventh : Base
    {
        public Eleventh() : base()
        {
            Status = 1;
        }

        public override string Alias => "Eleventh";

        public override string Description => "Dade";

        public override string URL => "http://www.jud11.flcourts.org/";

        public override Task<string> Execute()
        {
            collection = new ObservableCollection<Judge>();


            _ = Scraper.Scan(URL, (driver, wait) =>
            {
                Actions actions = new Actions(driver);

                var about_the_court = driver.FindElement(By.XPath("//*[@id='Form']/div[3]/div[1]/div[1]/div/header/div[4]/div/div[2]/ol/li[2]/span/span[1]"));
                actions.MoveToElement(about_the_court).Perform();

                var _judges = driver.FindElement(By.XPath("//*[@id='Form']/div[3]/div[1]/div[1]/div/header/div[4]/div/div[2]/ol/li[2]/div/div/ol/li[1]/span/span[1]"));
                actions.MoveToElement(_judges).Perform();

                var judicial_directory = driver.FindElement(By.XPath("//*[@id='Form']/div[3]/div[1]/div[1]/div/header/div[4]/div/div[2]/ol/li[2]/div/div/ol/li[1]/div/div/ol/li[1]/span/a"));
                actions.MoveToElement(judicial_directory).Click().Perform();

                wait.Until((e) => By.XPath("//h2[contains(text(), 'Circuit Court')]"));

                var people = driver.FindElements(By.XPath("//div[@class='hover-content']/a")).Select(e => e.GetAttribute("href")).ToList();


                foreach (var person in people)
                {
                    var path = string.Format("//a[@href='../../{0}']", person.Substring(person.IndexOf("Judge-Details")));

                    var a = driver.FindElement(By.XPath(path));

                    IJavaScriptExecutor executor = (IJavaScriptExecutor)driver;
                    executor.ExecuteScript("arguments[0].click();", a);

                    wait.Until((e) => By.ClassName("TeamDetail_right"));

                    Judge judge = new Judge()
                    {
                        LastName = driver.FindElement(By.Id("dnn_ctr1843_View_lblLastName")).Text,
                        FirstName = driver.FindElement(By.Id("dnn_ctr1843_View_lblFirstName")).Text,
                        Phone = driver.FindElement(By.Id("dnn_ctr1843_View_lblPhone")).Text,
                        CourtRoom = driver.FindElement(By.Id("dnn_ctr1843_View_lblRoomNumber")).Text,
                        JudicialAssistant = driver.FindElement(By.Id("dnn_ctr1843_View_lblJAName")).Text,
                        County = Description,
                        Circuit = Alias
                    };

                    var _location = driver.FindElement(By.Id("dnn_ctr1843_View_lblCourtHouseAddr")).Text;

                    if (_location.Contains("73 West Flagler Street"))
                        judge.Street = _location;
                    else
                        judge.Location = _location;

                    if (driver.FindElements(By.Id("dnn_ctr1843_View_lblDivCourt")).Count > 0)
                    {
                        var value = driver.FindElement(By.Id("dnn_ctr1843_View_lblDivCourt")).Text;

                        judge.Type = value;
                        judge.SubDivision = value;
                    }

                    if (driver.FindElements(By.Id("dnn_ctr1843_View_lblAdress")).Count > 0)
                    {
                        var address = driver.FindElement(By.Id("dnn_ctr1843_View_lblAdress")).Text.Address();

                        judge.Street = address[0];
                        judge.City = address[Array.IndexOf(address, address.LastOrDefault()!) - 1];
                        judge.Zip = address.LastOrDefault()!;
                    }

                    collection.Add(judge);

                    driver.Navigate().Back();
                }
            });

            return base.Execute();
        }
    }
}
