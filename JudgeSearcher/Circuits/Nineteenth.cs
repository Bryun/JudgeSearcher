using HtmlAgilityPack;
using JudgeSearcher.Models;
using JudgeSearcher.Utility;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JudgeSearcher.Circuits
{
    internal class Nineteenth : Base
    {
        #region Constructor

        public Nineteenth() : base()
        {
            Status = 1;
        }

        #endregion

        #region Properties

        public override string Alias => "Nineteenth";

        public override string Description => "Indian River, Martin, Okeechobee and St. Lucie";

        public override string URL => "http://www.circuit19.org/";

        #endregion

        #region Methods

        public override Task<string> Execute()
        {
            collection = new ObservableCollection<Judge>();

            _ = Scraper.Scan(URL, (driver, wait) =>
            {
                Actions action = new Actions(driver);

                var counties = new string[] { "Indian River", "Martin", "Okeechobee", "Saint Lucie" };

                foreach (var county in counties)
                {

                    var primary = driver.FindElement(By.XPath("//a[contains(text(), 'Judges ')]"));

                    action.MoveToElement(primary).Click().Perform();

                    var secondary = driver.FindElement(By.XPath(string.Format("//a[contains(text(), '{0} County Judges')]", county)));

                    action.MoveToElement(secondary).Click().Perform();

                    foreach (var card in driver.FindElements(By.XPath("//div[@class='views-field views-field-nothing']")))
                    {
                        var temp = Regex.Split(card.Text, ",|(\r\n)");

                        Judge judge = new Judge()
                        {
                            Circuit = Alias,
                            LastName = temp[0],
                            FirstName = temp[1],
                            County = county
                        };

                        card.Click();

                        wait.Until((e) => By.XPath("//article[@role='article']"));

                        HtmlWeb web = new HtmlWeb();
                        var document = web.Load(driver.Url);

                        var article = document.DocumentNode.SelectSingleNode("//article[@role='article']");

                        var lines = article.ChildNodes[1].ChildNodes.Where((e) => e.Name == "div").ToList();

                        for (int i = 1; i < lines.Count(); i++)
                        {
                            var t = lines[i].InnerText.Split("\n", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                            switch (t[0].Trim())
                            {
                                case "Position":
                                    judge.Type = t[1].Trim();
                                    break;
                                case "Office Address":
                                    judge.Street = t[1];
                                    var array = t[1].Address();

                                    switch (array.Count())
                                    {
                                        case 2:
                                            //judge.Street = array[0];
                                            judge.Zip = array[1];
                                            break;
                                        case 3:
                                            //judge.Street = array[0];
                                            judge.City = array[1];
                                            judge.Zip = array[2];
                                            break;
                                        case 4:
                                            //judge.Street = array[0];
                                            judge.City = array[2];
                                            judge.Zip = array[3];
                                            break;
                                        default:
                                            judge.Street = t[1];
                                            break;
                                    }

                                    break;
                                case "Phone":
                                    judge.Phone = t[1].Trim();
                                    break;
                                case "Judicial Assistant":
                                    judge.JudicialAssistant = t[1].Trim();
                                    break;
                                case "Division(s)":
                                    judge.SubDivision = string.Join(", ", t.Skip(1));
                                    break;
                                default:
                                    break;
                            }
                        }

                        collection.Add(judge);

                        driver.Navigate().Back();
                    }
                }
            });

            return base.Execute();
        }

        #endregion
    }
}
