using JudgeSearcher.Models;
using JudgeSearcher.Utility;
using System.Collections.ObjectModel;

namespace JudgeSearcher.Interfaces
{
    public interface ICircuit
    {
        string Name { get; }

        string Alias { get; }

        string Description { get; }

        string URL { get; }

        bool IsBusy { get; set; }

        int Status { get; set; }

        ObservableCollection<Judge> Judges { get; set; }

        Commander Scrape { get; }

        Commander Export { get; }
    }
}
