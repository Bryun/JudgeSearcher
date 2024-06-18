using JudgeSearcher.Interfaces;
using JudgeSearcher.Models;
using JudgeSearcher.Utility;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace JudgeSearcher.Circuits
{
    public class Base : Notification, ICircuit
    {
        #region Declaration

        public ObservableCollection<Judge> judges;
        private ObservableCollection<Validated> checklist;
        bool isBusy, view;
        int status = 0;

        #endregion

        #region Constructor

        public Base() { }

        #endregion

        #region Properties

        public virtual string Name => string.Format("{0} Circuit", Alias);

        public virtual string Alias => string.Empty;

        public virtual string Description => throw new NotImplementedException();

        public virtual string URL => string.Empty;

        public virtual bool IsBusy
        {
            get => isBusy;
            set
            {
                isBusy = value;
                OnPropertyChanged();
            }
        }

        public virtual int Status
        {
            get => status;
            set
            {
                status = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Judge> Judges
        {
            get => judges;
            set
            {
                judges = value;

                if (Checklist != null && Checklist.Count > 0)
                {
                    foreach (var item in Checklist)
                    {
                        item.Exists = judges.Where(e => item.County.Equals(e.County) && item.Type.Equals(e.Type) && item.FirstName.Equals(e.FirstName) && item.LastName.Equals(e.LastName)).Count() > 0;
                    }
                }

                OnPropertyChanged();
            }
        }

        public virtual bool View
        {
            get => view;
            set
            {
                view = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Validated> Checklist
        {
            get => checklist;
            set
            {
                checklist = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Commands

        public virtual Commander Scrape => new Commander((e) =>
        {
            Worker worker = new Worker(async (w) =>
            {
                Log.Logger.Information(string.Format("Circuit {0} is being scraped...", Alias));
                IsBusy = true;
                ((DoWorkEventArgs)w).Result = await Execute();
            },
            null,
            (e) =>
            {
                IsBusy = false;
                MessageBox.Show(((RunWorkerCompletedEventArgs)e).Result.ToString(), "Scrape", MessageBoxButton.OK, MessageBoxImage.Information);
            });

            worker.Start(null);

        }, (e) => true);

        public virtual Commander Export => new Commander((e) =>
        {
            Worker worker = new Worker(async (w) =>
            {
                Log.Logger.Information(string.Format("Circuit {0} is being exported...", Alias));
                IsBusy = true;
                Document.Save(async (path) =>
                {
                    ((DoWorkEventArgs)w).Result = await Excelsior.Save(Database.Export(Alias), path, Alias);
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

        public virtual Commander Clear => new Commander((e) =>
        {
            Worker worker = new Worker(async (w) =>
            {
                IsBusy = true;
                await Database.Delete(Alias);
                Refresh();
            },
            null,
            (e) =>
            {
                IsBusy = false;
                MessageBox.Show("Data cleared successfully!", "Clear", MessageBoxButton.OK, MessageBoxImage.Information);
            });

            worker.Start(null);
        }, (e) =>
        {
            return Judges != null && Judges.Count > 0;
        });

        #endregion

        #region Methods

        public void Refresh()
        {
            Judges = new ObservableCollection<Judge>(Database.Select(Alias));
        }

        public virtual async Task<string> Execute()
        {
            try
            {
                OnPropertyChanged(nameof(Judges));

                if (Judges.Count > 0)
                {
                    var table = new DataTable(Alias);

                    table.Columns.AddRange(new Judge().Map.Keys.Select(e => new DataColumn(e)).ToArray());

                    foreach (var judge in Judges)
                    {
                        DataRow row = table.NewRow();

                        foreach (KeyValuePair<string, string> item in judge.Map)
                        {
                            row[item.Key] = item.Value;
                        }

                        table.Rows.Add(row);
                    }

                    await Database.Delete(Alias);
                    await Database.Batch(table);
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex.StackTrace);
            }

            return "Scraping completed successfully.";
        }

        public void Display(string message)
        {
            Debug.WriteLine("--------------------------------------------------------------------------------------------------------------------");
            Debug.WriteLine("-- START --");
            Debug.WriteLine("--------------------------------------------------------------------------------------------------------------------");
            Debug.WriteLine(message);
            Debug.WriteLine("--------------------------------------------------------------------------------------------------------------------");
            Debug.WriteLine("-- END --");
            Debug.WriteLine("--------------------------------------------------------------------------------------------------------------------");
        }

        #endregion

    }
}
