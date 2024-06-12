using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SQLitePCL;
using System;
using System.Threading.Tasks;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using WebDriverManager.Helpers;

namespace JudgeSearcher.Utility
{
    public static class Scraper
    {

        public static async Task<bool> Scan(string url, Action<ChromeDriver, WebDriverWait> action, bool visible = true, bool allowImages = false, TimeSpan? period = null)
        {
            new DriverManager().SetUpDriver(new ChromeConfig(), VersionResolveStrategy.MatchingBrowser);

            ChromeOptions options = new ChromeOptions();

            if (!allowImages)
                options.AddUserProfilePreference("profile.default_content_setting_values.images", 2);

            //options.AddUserProfilePreference("javascript.enabled", false);
            options.AddArgument("--start-maximized");

            if (!visible) { options.AddArgument("headless"); }

            using (ChromeDriver driver = new ChromeDriver(options))
            {
                driver.Manage().Timeouts().ImplicitWait = period ?? TimeSpan.FromSeconds(20);
                driver.Navigate().GoToUrl(url);

                WebDriverWait wait = new WebDriverWait(driver, period ?? TimeSpan.FromSeconds(20));

                action(driver, wait);

                return true;
            }
        }
    }
}
