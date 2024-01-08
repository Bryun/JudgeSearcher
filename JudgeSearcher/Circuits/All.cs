using JudgeSearcher.Utility;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;

namespace JudgeSearcher.Circuits
{
    internal class All : Base
    {
        #region Constructor

        public All() : base()
        {
            Status = 1;
        }

        #endregion

        #region Properties

        public override string Alias => "All";

        public override string Name => "All";

        public override string Description => "All Florida circuits.";

        public override Commander Export => new Commander((e) =>
        {
            Worker worker = new Worker(async (w) =>
            {
                IsBusy = true;
                Document.Save(async (path) =>
                {
                    var circuits = new List<string>() { "All", "First", "Second", "Third", "Fourth", "Fifth", "Sixth", "Seventh", "Eighth", "Ninth", "Tenth", "Eleventh", "Twelveth", "Thirteenth", "Fourteenth", "Fifteenth", "Sixteenth", "Seventeenth", "Eighteenth", "Nineteenth", "Twentienth" };

                    foreach (var circuit in circuits)
                    {
                        await Excelsior.Save(Database.Export(circuit), path, circuit);
                    }

                    ((DoWorkEventArgs)w).Result = "Exported successfully.";
                });
            },
            null,
            (e) =>
            {
                IsBusy = false;
                MessageBox.Show(((RunWorkerCompletedEventArgs)e).Result.ToString(), "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            });

            worker.Start(null);
        }, (e) =>
        {
            return Judges != null && Judges.Count > 0;
        });

        public override Commander Scrape => new Commander((e) => { }, (e) => false);

        #endregion

        #region Methods

        public override Task<string> Execute()
        {
            new List<Base>()
            {
                new First(),
                new Second(),
                new Third(),
                new Fourth(),
                new Fifth(),
                new Sixth(),
                new Seventh(),
                new Eighth(),
                new Ninth(),
                new Tenth(),
                new Eleventh(),
                new Twelveth(),
                new Thirteenth(),
                new Fourteenth(),
                new Fifteenth(),
                new Sixteenth(),
                new Seventeenth(),
                new Eighteenth(),
                new Nineteenth(),
                new Twentienth()
            }.ForEach(async (e) => await e.Execute());

            return Task.Run(() => "Scraping completed successfully.");
        }

        #endregion
    }
}
