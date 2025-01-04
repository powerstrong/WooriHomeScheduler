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
        private DateTime selectedCustomDate;

        private List<DateTime> _customHolidayList = new();

        partial void OnSelectedCustomDateChanged(DateTime oldValue, DateTime newValue)
        {
            if (_customHolidayList.Contains(newValue))
                _customHolidayList.Remove(newValue);
            else
                _customHolidayList.Add(newValue);

            UpdateHolidays();
        }

        [ObservableProperty]
        private string holidays = "휴무일";

        [ObservableProperty]
        private string everyWednesdays = "";

        [ObservableProperty]
        private string lastSundays = "";

        [ObservableProperty]
        private string customHolidays = "";

        [ObservableProperty]
        private string outputText = "";

        [RelayCommand]
        private void Calculate()
        {
            var holidays = GetWednesdays(StartDate, EndDate).Concat(GetLastSundays(StartDate, EndDate)).Concat(_customHolidayList).ToList();
            var schedule = ScheduleGenerator.Generate(StartDate, EndDate, Workers.Split(' ').ToList(), holidays);

            OutputText = "";
            foreach (var dayTable in schedule)
            {
                OutputText += $"{dayTable.Key.ToString("yyyy-MM-dd")} : {string.Join(", ", dayTable.Value)}\n";
            }
        }

        void UpdateHolidays()
        {
            var w = GetWednesdays(StartDate, EndDate);
            var s = GetLastSundays(StartDate, EndDate);
            var c = _customHolidayList;

            EveryWednesdays = string.Join("\n", w.Select(d => d.ToString("yyyy-MM-dd")));
            LastSundays = string.Join("\n", s.Select(d => d.ToString("yyyy-MM-dd")));
            CustomHolidays = string.Join("\n", c.Select(d => d.ToString("yyyy-MM-dd")));

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
