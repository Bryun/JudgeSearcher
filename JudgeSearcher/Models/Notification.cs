using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace JudgeSearcher.Models
{
    public class Notification : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
