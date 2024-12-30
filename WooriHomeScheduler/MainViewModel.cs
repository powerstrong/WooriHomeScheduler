using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WooriHomeScheduler
{
    partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private string workers = string.Empty;

        [ObservableProperty]
        private string startDate = "2025-01-01";

        [ObservableProperty]
        private string endDate = "2025-01-01";

        public required ObservableCollection<DateTime> SelectedDates { get; set; }

    }
}
