using JudgeSearcher.Models;
using JudgeSearcher.Utility;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace JudgeSearcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            try
            {
                InitializeComponent();

                Log.Logger = new LoggerConfiguration()
                .WriteTo.File(Path.Join(Environment.CurrentDirectory, "log-.txt"), rollingInterval: RollingInterval.Day)
                .CreateLogger();

                Log.Logger.Information("Judge Searcher application has started...");

                //Database.Drop();
                Database.Create();

                //string main = "https://www.floridabar.org/directories/courts/maps-circuit/";

                this.DataContext = Source = new Florida();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex.StackTrace);
            }
        }

        public Florida Source { get; set; }


        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
