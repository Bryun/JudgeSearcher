using Microsoft.Win32;
using System;

namespace JudgeSearcher.Utility
{
    public static class Document
    {
        public static void Load(Action<string> action, string title = "Browse for Excel file.", string filter = "Excel files (*.xlsx)|*.xlsx", string extension = "xlsx")
        {
            OpenFileDialog dialog = new OpenFileDialog()
            {
                Title = title,
                Filter = filter,
                DefaultExt = extension,
                RestoreDirectory = true,
                CheckFileExists = true,
                CheckPathExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                action(dialog.FileName);
            }
        }

        public static void Save(Action<string> action, string title = "Save to Excel file.", string filter = "Excel files (*.xlsx)|*.xlsx", string extension = "xlsx")
        {
            SaveFileDialog dialog = new SaveFileDialog()
            {
                Title = title,
                Filter = filter,
                DefaultExt = extension,
                RestoreDirectory = true,
                CheckPathExists = true,
                OverwritePrompt = false
            };

            if (dialog.ShowDialog() == true)
            {
                action(dialog.FileName);
            }
        }
    }
}
