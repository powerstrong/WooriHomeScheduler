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
            Dictionary<string, int> workerCount = workers.ToDictionary(w => w, w => 0);

            // 모든 날짜와 근무일 준비
            List<DateTime> eachDays = EachDay(startDate, endDate).ToList();
            List<DateTime> workDays = GetPriorityWorkDays(eachDays);

            // 기본 배치
            foreach (var day in workDays)
            {
                if (holidays.Contains(day)) continue;
                schedule[day] = new List<(string, int)>();
                int baseWorkerCount = customWorkdays.GetValueOrDefault(day, 4);
                AssignWorkers(schedule, day, baseWorkerCount, workerQueue, workerCount);
            }

            // 추가 근무 계산 및 배치
            var nonWednesdayHolidays = holidays.Where(d => d.DayOfWeek != DayOfWeek.Wednesday).ToList();
            int remainWork = (nonWednesdayHolidays.Count - freeHolidayCount) * 4;

            if (remainWork > 0)
            {
                AssignExtraWorkersByPriority(schedule, workDays, holidays, customWorkdays, workerCount, remainWork, workers);
            }

            return schedule;
        }

        // 요일 우선순위에 따른 근무일 반환
        private static List<DateTime> GetPriorityWorkDays(List<DateTime> eachDays)
        {
            var priorityOrder = new[] { DayOfWeek.Saturday, DayOfWeek.Monday, DayOfWeek.Thursday, DayOfWeek.Tuesday, DayOfWeek.Friday, DayOfWeek.Sunday, DayOfWeek.Wednesday };
            var workDays = new List<DateTime>();
            foreach (var dayOfWeek in priorityOrder)
            {
                workDays.AddRange(eachDays.Where(d => d.DayOfWeek == dayOfWeek));
            }
            return workDays;
        }

        // 기본 근무자 배치
        private static void AssignWorkers(Dictionary<DateTime, List<(string, int)>> schedule, DateTime day, int count, Queue<string> workerQueue, Dictionary<string, int> workerCount)
        {
            for (int i = 0; i < count; i++)
            {
                var worker = workerQueue.Dequeue();
                workerCount[worker]++;
                schedule[day].Add((worker, workerCount[worker]));
                workerQueue.Enqueue(worker);
            }
        }

        // 추가 근무를 우선순위에 따라 배치
        private static void AssignExtraWorkersByPriority(Dictionary<DateTime, List<(string, int)>> schedule, List<DateTime> workDays, List<DateTime> holidays, Dictionary<DateTime, int> customWorkdays, Dictionary<string, int> workerCount, int remainWork, List<string> workers)
        {
            int baseMaxWorkersPerDay = 5; // 기본 최대 근무자 수 (4 + 1)
            int currentMaxWorkers = baseMaxWorkersPerDay;
            int maxAllowedWorkers = workers.Count; // 최대 근무자 수는 전체 근무자 수로 제한

            while (remainWork > 0)
            {
                bool assignedThisRound = false;

                foreach (var day in workDays)
                {
                    if (holidays.Contains(day) || customWorkdays.ContainsKey(day) || schedule[day].Count >= currentMaxWorkers) continue;

                    // 현재 근무 횟수가 가장 적은 근무자 선택
                    var leastBusyWorker = workers
                        .Where(w => !schedule[day].Any(entry => entry.Item1 == w)) // 중복 배치 방지
                        .OrderBy(w => workerCount[w])
                        .FirstOrDefault();

                    if (leastBusyWorker != null)
                    {
                        workerCount[leastBusyWorker]++;
                        schedule[day].Add((leastBusyWorker, workerCount[leastBusyWorker]));
                        remainWork--;
                        assignedThisRound = true;
                    }

                    if (remainWork <= 0) return;
                }

                // 이번 라운드에서 배치가 하나도 안 됐다면
                if (!assignedThisRound)
                {
                    if (currentMaxWorkers >= maxAllowedWorkers)
                    {
                        // 모든 근무자가 꽉 찼으므로 종료
                        if (remainWork > 0)
                        {
                            MessageBox.Show($"모든 근무자가 최대치로 배치되었습니다! 필요 근무가 {remainWork}개 남았습니다.");
                        }
                        return;
                    }
                    currentMaxWorkers++; // 최대 근무자 수 증가
                }
            }
        }

        private static IEnumerable<DateTime> EachDay(DateTime from, DateTime to)
        {
            for (var day = from.Date; day <= to.Date; day = day.AddDays(1))
                yield return day;
        }
    }

}
