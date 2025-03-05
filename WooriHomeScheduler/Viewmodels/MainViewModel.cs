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
        private string thursAndSundays = "";

        [ObservableProperty]
        private string outputText = "";

        [ObservableProperty]
        private string statisticText = "";

        [RelayCommand]
        private void Calculate()
        {
            OutputText = StatisticText = string.Empty;

            List<string> workers = Workers.Split(' ').ToList();
            List<DateTime> customHolidayDates = CustomHolidays.Select(h => h.Date).ToList();
            int freeHolidayCount = CustomHolidays.Count(h => h.IsFree);
            Dictionary<DateTime, int> customWorkdayDictionary = CustomWorkdays.ToDictionary(w => w.Date, w => w.WorkerCount);

            if (customWorkdayDictionary.Values.Any(count => count > workers.Count))
            {
                OutputText = "근무자 수보다 많은 근무자가 배치되어 있습니다.";
                return;
            }

            var holidays = GetWednesdays(StartDate, EndDate).Concat(GetSecondThursdays(StartDate, EndDate)).Concat(GetFourthSundays(StartDate, EndDate)).Concat(customHolidayDates).ToList();
            var schedule = ScheduleGenerator.Generate(StartDate, EndDate, workers, holidays, customWorkdayDictionary, freeHolidayCount);

            StatisticText += "<기본배치>\n";
            StatisticText += " - 먼저 각 근무일마다 4명씩, 또는 커스텀 근무자수만큼을 돌아가며 배치합니다.\n";
            StatisticText += " - 근무배치 우선순위 : 토월목화금일\n";
            StatisticText += "\n";
            StatisticText += "<추가배치>\n";
            StatisticText += " - 추가근무 = {(공짜 휴일, 수요일)이 아닌 휴무일} x4\n";
            StatisticText += " - 커스텀 근무일이 있으면 마진만큼 빼줍니다. (6명 일했다? -2명 ㄱㄱ)\n";
            StatisticText += " - 근무가 적은 사람 순으로, 추가근무 수만큼 들어가기 시작합니다.\n";
            StatisticText += "\n";
            StatisticText += "<인원별 통계>\n";

            // 근무자별, 요일별 근무 시간을 통계로 작성한다
            var workerCount = workers.ToDictionary(worker => worker, worker => 0);
            var workerDayCount = workers.ToDictionary(worker => worker, worker => 0);
            var workerDayOfWeekCount = workers.ToDictionary(worker => worker, worker => new Dictionary<DayOfWeek, int>());

            // StatisticText에 근무자별 근무 횟수를 출력한다
            Dictionary<DayOfWeek, string> dictEnglishDayToKoreanDay = new()
            {
                { DayOfWeek.Sunday, "일" },
                { DayOfWeek.Monday, "월" },
                { DayOfWeek.Tuesday, "화" },
                { DayOfWeek.Wednesday, "수" },
                { DayOfWeek.Thursday, "목" },
                { DayOfWeek.Friday, "금" },
                { DayOfWeek.Saturday, "토" }
            };
            foreach (var worker in workers)
            {
                foreach (var day in schedule.Keys)
                {
                    if (schedule[day].Any(workerEntry => workerEntry.Item1 == worker))
                    {
                        workerCount[worker]++;
                        workerDayCount[worker]++;
                        if (!workerDayOfWeekCount[worker].ContainsKey(day.DayOfWeek))
                        {
                            workerDayOfWeekCount[worker][day.DayOfWeek] = 0;
                        }
                        workerDayOfWeekCount[worker][day.DayOfWeek]++;
                    }
                }
                // workerDayOfWeekCount를 요일 순으로 정렬한다
                workerDayOfWeekCount[worker] = workerDayOfWeekCount[worker].OrderBy(pair => pair.Key).ToDictionary(pair => pair.Key, pair => pair.Value);

                StatisticText += $" - {worker}({workerCount[worker]}) : ";

                StatisticText += string.Join(", ", workerDayOfWeekCount[worker].Select(pair => $"{dictEnglishDayToKoreanDay[pair.Key]} {pair.Value}"));
                StatisticText += "\n";
            }

            // 전체 기간 날짜 수
            StatisticText += "\n<기타등등 통계>\n";
            StatisticText += $" - 기간 : {StartDate.ToString("yyyy-MM-dd(ddd)")} ~ {EndDate.ToString("yyyy-MM-dd(ddd)")}, 총 {(EndDate - StartDate).Days + 1}일\n";

            // 기간 내 근무일, 휴무일 통계
            StatisticText += $" - 근무일 : {schedule.Count}일\n";
            StatisticText += $" - 휴무일 : {holidays.Count}일\n";

            // 전체 근무 수
            StatisticText += $" - 총 근무 수 : {workerCount.Values.Sum()}회\n";


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
            var ts = GetFourthSundays(StartDate, EndDate).Concat(GetSecondThursdays(StartDate, EndDate)).OrderBy(date => date);

            List<DateTime> c = CustomHolidays.Select(h => h.Date).ToList();

            EveryWednesdays = string.Join("\n", w.Select(d => d.ToString("yyyy-MM-dd(ddd)")));
            ThursAndSundays = string.Join("\n", ts.Select(d => d.ToString("yyyy-MM-dd(ddd)")));

            List<DateTime> allHolidays = w.Concat(ts).Concat(c).ToList();
            allHolidays = allHolidays.Distinct().ToList();

            Holidays = $"휴무일 : {allHolidays.Count}일";
        }

        static List<DateTime> GetFourthSundays(DateTime start, DateTime end)
        {
            var fourthSundays = new List<DateTime>();

            // 시작 월부터 종료 월까지 순회
            DateTime current = new DateTime(start.Year, start.Month, 1);
            while (current <= end)
            {
                // 해당 월의 첫 번째 일요일로 이동
                DateTime firstSunday = current;
                while (firstSunday.DayOfWeek != DayOfWeek.Sunday)
                {
                    firstSunday = firstSunday.AddDays(1);
                }

                // 네 번째 일요일로 이동
                DateTime fourthSunday = firstSunday.AddDays(21);

                // 네 번째 일요일이 범위 내에 있는지 확인
                if (fourthSunday >= start && fourthSunday <= end)
                {
                    fourthSundays.Add(fourthSunday);
                }

                // 다음 달로 이동
                current = current.AddMonths(1);
            }

            return fourthSundays;
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

        static List<DateTime> GetSecondThursdays(DateTime start, DateTime end)
        {
            var secondThursdays = new List<DateTime>();

            // 시작 월부터 종료 월까지 순회
            DateTime current = new DateTime(start.Year, start.Month, 1);
            while (current <= end)
            {
                // 해당 월의 첫 번째 목요일로 이동
                DateTime firstThursday = current;
                while (firstThursday.DayOfWeek != DayOfWeek.Thursday)
                {
                    firstThursday = firstThursday.AddDays(1);
                }

                // 두 번째 목요일로 이동
                DateTime secondThursday = firstThursday.AddDays(7);

                // 두 번째 목요일이 범위 내에 있는지 확인
                if (secondThursday >= start && secondThursday <= end)
                {
                    secondThursdays.Add(secondThursday);
                }

                // 다음 달로 이동
                current = current.AddMonths(1);
            }

            return secondThursdays;
        }
    }
}
