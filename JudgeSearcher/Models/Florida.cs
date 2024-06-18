using JudgeSearcher.Circuits;
using JudgeSearcher.Utility;
using System.Collections.ObjectModel;
using System.Windows;

namespace JudgeSearcher.Models
{
    public class Florida : Notification
    {
        #region Declarations

        bool browser = false, checklist = false;
        Visibility visible = Visibility.Collapsed;
        Base circuit;
        ObservableCollection<Base> circuits;

        #endregion

        #region Constructor

        public Florida()
        {
            Circuits = new ObservableCollection<Base>
            {
                new All(),
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
                new Twentienth(),
            };

            Validate = new Commander((e) =>
            {
                Circuit.View = true;
            }, (e) => Circuit != null && Circuit.Checklist != null && Circuit.Checklist.Count > 0);
        }

        #endregion

        #region Properties

        public bool Browser
        {
            get => browser;
            set
            {
                browser = value;
                OnPropertyChanged();
            }
        }

        public bool Checklist
        {
            get => checklist;
            set
            {
                checklist = value;
                OnPropertyChanged();
            }
        }

        public Base Circuit
        {
            get => circuit;
            set
            {
                circuit = value;

                if (circuit != null && Visible != Visibility.Visible)
                {
                    Visible = Visibility.Visible;
                }

                circuit.Refresh();
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Base> Circuits
        {
            get => circuits;
            set
            {
                circuits = value;
                OnPropertyChanged();
            }
        }

        public Visibility Visible
        {
            get => visible;
            set
            {
                visible = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Commands

        public Commander Validate { get; set; }

        #endregion
    }
}
