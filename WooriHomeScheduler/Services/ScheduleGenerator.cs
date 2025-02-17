using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace WooriHomeScheduler.Services
{
    public class ScheduleGenerator
    {
        public static Dictionary<DateTime, List<(string, int)>> Generate(DateTime startDate, DateTime endDate, List<string> workers, List<DateTime> holidays, Dictionary<DateTime, int> customWorkdays, int freeHolidayCount)
        {
            var schedule = new Dictionary<DateTime, List<(string, int)>>();
            var workerQueue = new Queue<string>(workers);
            //var additionalWorkersQueue = new Queue<string>(workers);

            Dictionary<string, int> workerCount = [];
            foreach (var worker in workers)
            {
                workerCount[worker] = 0;
            }

            // 근무배치 우선순위 : 토월목화금일
            List<DateTime> eachDays = EachDay(startDate, endDate).ToList();
            List<DateTime> workDays = new();
            workDays.AddRange(eachDays.Where(d => d.DayOfWeek == DayOfWeek.Saturday));
            workDays.AddRange(eachDays.Where(d => d.DayOfWeek == DayOfWeek.Monday));
            workDays.AddRange(eachDays.Where(d => d.DayOfWeek == DayOfWeek.Thursday));
            workDays.AddRange(eachDays.Where(d => d.DayOfWeek == DayOfWeek.Tuesday));
            workDays.AddRange(eachDays.Where(d => d.DayOfWeek == DayOfWeek.Friday));
            workDays.AddRange(eachDays.Where(d => d.DayOfWeek == DayOfWeek.Sunday));

            // 기본배치 가즈아
            foreach (var day in workDays)
            {
                if (holidays.Contains(day)) continue;
                schedule[day] = new List<(string, int)>();

                int baseWorkerCount = customWorkdays.ContainsKey(day) ? customWorkdays[day] : 4;
                while (schedule[day].Count < baseWorkerCount)
                {
                    var worker = workerQueue.Dequeue();
                    workerCount[worker]++;

                    schedule[day].Add((worker, workerCount[worker]));
                    workerQueue.Enqueue(worker);
                }
            }

            var nonWednesdayHolidays = holidays.Where(d => d.DayOfWeek != DayOfWeek.Wednesday).ToList();
            if (schedule.Count < nonWednesdayHolidays.Count * 2)
            {
                MessageBox.Show("너무 많이 쉰다");
                return schedule;
            }

            //// 토요일 근무가 적은 사람부터 들어가세요
            //var additionalWorkersQueue = new Queue<string>(GetWorkersSortedBySaturdayCount(schedule, workers));

            Queue<DayOfWeek> dayPriorityQueue = new([DayOfWeek.Saturday, DayOfWeek.Monday, DayOfWeek.Thursday, DayOfWeek.Tuesday, DayOfWeek.Friday, DayOfWeek.Sunday]);

            var remainWork = (nonWednesdayHolidays.Count - freeHolidayCount) * 4;

            // custom workday 마진만큼 더 빼주도록 하자
            var howManyCustomWorkDay = customWorkdays.Count;
            var customWorkdayCount = customWorkdays.Values.Sum();
            var customWorkdayMargin = customWorkdayCount - howManyCustomWorkDay * 4;
            remainWork -= customWorkdayMargin;

            while (remainWork > 0)
            {
                var day = dayPriorityQueue.Dequeue();
                dayPriorityQueue.Enqueue(day);

                var extraWork = eachDays.Where(d => d.DayOfWeek == day).ToList();

                foreach (var workDay in extraWork)
                {
                    // 현재 근무 수가 가장 적은 수로 정렬
                    var additionalWorkersQueue = new Queue<string>(workers.OrderBy(worker => workerCount[worker]));

                    if (holidays.Contains(workDay)) continue;
                    if (customWorkdays.ContainsKey(workDay)) continue;

                    bool done = false;
                    while (done == false) // 기존 4명 + 추가 1명으로 5명까지 배치
                    {
                        var potentialWorker = additionalWorkersQueue.Dequeue();

                        // 이미 배치된 근무자들과 중복이 없는지 확인
                        if (!schedule[workDay].Any(workerEntry => workerEntry.Item1 == potentialWorker))
                        {
                            workerCount[potentialWorker]++;
                            schedule[workDay].Add((potentialWorker, workerCount[potentialWorker]++));

                            done = true;
                            remainWork--;
                        }

                        // 선택된 근무자를 다시 Queue에 추가하여 순환 구조 유지
                        additionalWorkersQueue.Enqueue(potentialWorker);
                    }

                    if (remainWork == 0) break;
                }

                if (remainWork == 0) break;
            }

            return schedule;
        }

        private static IEnumerable<DateTime> EachDay(DateTime from, DateTime to)
        {
            for (var day = from.Date; day <= to.Date; day = day.AddDays(1))
                yield return day;
        }

        // 토요일 근무 횟수에 따라 근무자 리스트 정렬
        public static List<string> GetWorkersSortedBySaturdayCount(Dictionary<DateTime, List<(string, int)>> schedule, List<string> workers)
        {
            var saturdayCounts = workers.ToDictionary(worker => worker, worker => 0);

            foreach (var entry in schedule)
            {
                if (entry.Key.DayOfWeek == DayOfWeek.Saturday)
                {
                    foreach (var (worker, _) in entry.Value)
                    {
                        saturdayCounts[worker]++;
                    }
                }
            }

            return saturdayCounts.OrderBy(kv => kv.Value).Select(kv => kv.Key).ToList();
        }
    }

}
