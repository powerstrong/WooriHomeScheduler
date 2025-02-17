using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WooriHomeScheduler.Models;
using WooriHomeScheduler.Services;

namespace WooriHomeScheduler
{
    partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private string workers = "AAA BBB CCC DDD EEE FFF";

        [ObservableProperty]
        private DateTime startDate = DateTime.Now;

        private ObservableCollection<CustomHolidayModel> _customHolidays = new();
        public ObservableCollection<CustomHolidayModel> CustomHolidays
        {
            get => _customHolidays;
            set
            {
                _customHolidays = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<CustomWorkdayModel> _customWorkdays = new();
        public ObservableCollection<CustomWorkdayModel> CustomWorkdays
        {
            get => _customWorkdays;
            set
            {
                _customWorkdays = value;
                OnPropertyChanged();
            }
        }

        partial void OnStartDateChanged(DateTime oldValue, DateTime newValue)
        {
            if (EndDate < newValue)
            {
                EndDate = newValue;
            }

            UpdateHolidays();
        }

        [ObservableProperty]
        private DateTime endDate = DateTime.Now + TimeSpan.FromDays(28);

        partial void OnEndDateChanged(DateTime oldValue, DateTime newValue)
        {
            if (StartDate > newValue)
            {
                StartDate = newValue;
            }

            UpdateHolidays();
        }

        [ObservableProperty]
        private DateTime selectedHoliDate;

        partial void OnSelectedHoliDateChanged(DateTime oldValue, DateTime newValue)
        {
            // 컬렉션에서 newValue와 같은 날짜가 있는지 확인
            var existingItem = CustomHolidays.FirstOrDefault(item => item.Date.Date == newValue.Date);

            if (existingItem != null)
            {
                // 이미 존재하면 컬렉션에서 제거
                CustomHolidays.Remove(existingItem);
            }
            else
            {
                // 존재하지 않으면 새로운 항목 추가
                CustomHolidays.Add(new CustomHolidayModel(newValue, false));
            }

            UpdateHolidays();
        }

        [ObservableProperty]
        private DateTime selectedWorkDate;

        partial void OnSelectedWorkDateChanged(DateTime oldValue, DateTime newValue)
        {
            // 컬렉션에서 newValue와 같은 날짜가 있는지 확인
            var existingItem = CustomWorkdays.FirstOrDefault(item => item.Date.Date == newValue.Date);

            if (existingItem != null)
            {
                // 이미 존재하면 컬렉션에서 제거
                CustomWorkdays.Remove(existingItem);
            }
            else
            {
                // 존재하지 않으면 새로운 항목 추가
                CustomWorkdays.Add(new CustomWorkdayModel(newValue, 4));
            }
        }

        [RelayCommand]
        void DeleteHoliday(object parameter)
        {
            if (parameter is CustomHolidayModel item)
            {
                CustomHolidays.Remove(item);
            }
        }

        [RelayCommand]
        void DeleteWorkday(object parameter)
        {
            if (parameter is CustomWorkdayModel item)
            {
                CustomWorkdays.Remove(item);
            }
        }

        [ObservableProperty]
        private string holidays = "휴무일";

        [ObservableProperty]
        private string everyWednesdays = "";

        [ObservableProperty]
        private string lastSundays = "";

        [ObservableProperty]
        private string outputText = "";

        [RelayCommand]
        private void Calculate()
        {
            List<string> workers = Workers.Split(' ').ToList();
            List<DateTime> customHolidayDates = CustomHolidays.Select(h => h.Date).ToList();
            int freeHolidayCount = CustomHolidays.Count(h => h.IsFree);
            Dictionary<DateTime, int> customWorkdayDictionary = CustomWorkdays.ToDictionary(w => w.Date, w => w.WorkerCount);

            if (customWorkdayDictionary.Values.Any(count => count > workers.Count))
            {
                OutputText = "근무자 수보다 많은 근무자가 배치되어 있습니다.";
                return;
            }

            var holidays = GetWednesdays(StartDate, EndDate).Concat(GetLastSundays(StartDate, EndDate)).Concat(customHolidayDates).ToList();
            var schedule = ScheduleGenerator.Generate(StartDate, EndDate, workers, holidays, customWorkdayDictionary, freeHolidayCount);

            // 기간 중 수요일이 아닌 휴일 구하기
            var nonWednesdayHolidays = holidays.Where(date => date.DayOfWeek != DayOfWeek.Wednesday).ToList();
            OutputText = $"근무일 : {(EndDate-StartDate).Days+1-holidays.Count}일, 수요일이 아닌 휴무일 : {nonWednesdayHolidays.Count}일\n";
            OutputText += $"근무 : (근무일+수요일이 아닌 휴무일) * 4 = {((EndDate - StartDate).Days + 1 - holidays.Count + nonWednesdayHolidays.Count) * 4}\n";
            OutputText += "근무배치 우선순위 : 토월목화금일\n";
            OutputText += "-----------------------------------------------------------\n";

            for (var day = StartDate.Date; day <= EndDate.Date; day = day.AddDays(1))
            {
                if (schedule.ContainsKey(day))
                {
                    OutputText += $"{day.ToString("yyyy-MM-dd(ddd)")}[{schedule[day].Count}] : ";

                    var workersOfDay = schedule[day];
                    foreach (var worker in workersOfDay)
                    {
                        OutputText += $"{worker.Item1}({worker.Item2}), ";
                    }

                    OutputText = OutputText.Remove(OutputText.Length - 2);
                    OutputText += "\n";
                }
                else
                {
                    OutputText += $"{day.ToString("yyyy-MM-dd(ddd)")} : 휴무일\n";
                }
            }
        }

        void UpdateHolidays()
        {
            var w = GetWednesdays(StartDate, EndDate);
            var s = GetLastSundays(StartDate, EndDate);
            List<DateTime> c = CustomHolidays.Select(h => h.Date).ToList();

            EveryWednesdays = string.Join("\n", w.Select(d => d.ToString("yyyy-MM-dd(ddd)")));
            LastSundays = string.Join("\n", s.Select(d => d.ToString("yyyy-MM-dd(ddd)")));

            List<DateTime> allHolidays = w.Concat(s).Concat(c).ToList();
            allHolidays = allHolidays.Distinct().ToList();

            Holidays = $"휴무일 : {allHolidays.Count}일";
        }

        static List<DateTime> GetLastSundays(DateTime start, DateTime end)
        {
            var lastSundays = new List<DateTime>();

            // 시작 월부터 종료 월까지 순회
            DateTime current = new DateTime(start.Year, start.Month, 1);
            while (current <= end)
            {
                // 해당 월의 마지막 날 구하기
                DateTime lastDayOfMonth = new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month));

                // 마지막 일요일로 이동
                while (lastDayOfMonth.DayOfWeek != DayOfWeek.Sunday)
                {
                    lastDayOfMonth = lastDayOfMonth.AddDays(-1);
                }

                // 마지막 일요일이 범위 내에 있는지 확인
                if (lastDayOfMonth >= start && lastDayOfMonth <= end)
                {
                    lastSundays.Add(lastDayOfMonth);
                }

                // 다음 달로 이동
                current = current.AddMonths(1);
            }

            return lastSundays;
        }

        static List<DateTime> GetWednesdays(DateTime start, DateTime end)
        {
            var wednesdays = new List<DateTime>();

            // 시작 날짜를 첫 번째 수요일로 이동
            DateTime current = start;
            while (current.DayOfWeek != DayOfWeek.Wednesday)
            {
                current = current.AddDays(1);
            }

            // 모든 수요일을 리스트에 추가
            while (current <= end)
            {
                wednesdays.Add(current);
                current = current.AddDays(7); // 다음 수요일로 이동
            }

            return wednesdays;
        }
    }
}
