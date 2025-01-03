using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
        private string workers = "장원영 안유진 레이 이서 가을 리즈";

        [ObservableProperty]
        private DateTime startDate = DateTime.Now;

        [ObservableProperty]
        private DateTime endDate = DateTime.Now + TimeSpan.FromDays(28);

        public required ObservableCollection<DateTime> SelectedDates { get; set; }

        [ObservableProperty]
        private string outputText = "";

        [RelayCommand]
        private void Calculate()
        {
            List<DateTime> holidays = [
                DateTime.Parse("2025-01-06"),
                DateTime.Parse("2025-01-07"),
                DateTime.Parse("2025-01-08"),
                DateTime.Parse("2025-01-13"),
            ];
            var schedule = ScheduleGenerator.Generate(StartDate, EndDate, Workers.Split(' ').ToList(), holidays);

            OutputText = "";
            foreach (var dayTable in schedule)
            {
                OutputText += $"{dayTable.Key.ToString("yyyy-MM-dd")} : {string.Join(", ", dayTable.Value)}\n";
            }
        }
    }
}
