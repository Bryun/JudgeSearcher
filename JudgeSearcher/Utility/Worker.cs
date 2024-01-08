using System;
using System.ComponentModel;

namespace JudgeSearcher.Utility
{
    /// <summary>
    /// Worker worker = new Worker((w) => { }, (p) => { }, (c) => { });
    /// </summary>
    public class Worker : IDisposable
    {
        #region Declaration

        BackgroundWorker robot;
        Action<object> work, progress, complete;

        #endregion

        #region Constructor

        public Worker(Action<object> work, Action<object>? progress, Action<object>? complete)
        {
            this.work = work;
            this.progress = progress;
            this.complete = complete;

            robot = new BackgroundWorker()
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };

            robot.DoWork += Robot_DoWork;

            if (progress != null)
                robot.ProgressChanged += Robot_ProgressChanged;

            robot.RunWorkerCompleted += Robot_RunWorkerCompleted;
        }

        #endregion

        #region Events

        private void Robot_DoWork(object? sender, DoWorkEventArgs e)
        {
            Work(e);
            //robot.ReportProgress(1);
        }

        private void Robot_ProgressChanged(object? sender, ProgressChangedEventArgs e) => Progress(e);

        private void Robot_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e) => Complete(e);

        #endregion

        #region Methods

        public void Start(object? parameter)
        {
            if (!robot.IsBusy)
                robot.RunWorkerAsync(parameter);
        }

        public void Work(object? parameter) => work(parameter);

        public void Progress(object? parameter) => progress(parameter);

        public void Complete(object? parameter) => complete(parameter);

        public void Dispose() => robot.Dispose();

        #endregion
    }
}
