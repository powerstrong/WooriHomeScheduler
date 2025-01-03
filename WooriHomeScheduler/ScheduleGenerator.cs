namespace WooriHomeScheduler
{
    public class ScheduleGenerator
    {
        public static Dictionary<DateTime, List<string>> Generate(
            DateTime startDate,
            DateTime endDate,
            List<string> workers,
            List<DateTime> holidays)
        {
            var schedule = new Dictionary<DateTime, List<string>>();
            var random = new Random();
            var days = Enumerable.Range(0, (endDate - startDate).Days + 1)
                                 .Select(offset => startDate.AddDays(offset));
            var remainingWorkers = new List<string>(workers);

            foreach (var day in days)
            {
                // 매주 일요일 또는 지정된 휴일인 경우 스킵
                if (day.DayOfWeek == DayOfWeek.Sunday || holidays.Contains(day) || IsLastWednesday(day))
                {
                    continue;
                }

                // 근무자 4명을 랜덤 배치
                var todayWorkers = remainingWorkers.OrderBy(_ => random.Next()).Take(4).ToList();
                schedule[day] = todayWorkers;

                // 배정된 근무자 제거
                remainingWorkers.RemoveAll(w => todayWorkers.Contains(w));

                // 모든 근무자가 배정되었다면 다시 초기화
                if (remainingWorkers.Count < 4)
                {
                    remainingWorkers = new List<string>(workers);
                }
            }

            // 추가 배치 처리
            DistributeRemainingWorkers(schedule, workers, startDate, endDate, holidays, DayOfWeek.Saturday);
            DistributeRemainingWorkers(schedule, workers, startDate, endDate, holidays, DayOfWeek.Monday);

            return schedule;
        }

        private static bool IsLastWednesday(DateTime date)
        {
            // 마지막 수요일인지 확인
            return date.DayOfWeek == DayOfWeek.Wednesday &&
                   date.AddDays(7).Month != date.Month;
        }

        private static void DistributeRemainingWorkers(
            Dictionary<DateTime, List<string>> schedule,
            List<string> workers,
            DateTime startDate,
            DateTime endDate,
            List<DateTime> holidays,
            DayOfWeek targetDay)
        {
            var random = new Random();
            var days = Enumerable.Range(0, (endDate - startDate).Days + 1)
                                 .Select(offset => startDate.AddDays(offset))
                                 .Where(day => day.DayOfWeek == targetDay &&
                                               !holidays.Contains(day) &&
                                               !schedule.ContainsKey(day))
                                 .ToList();

            var workerCounts = workers.ToDictionary(w => w, w => 0);

            foreach (var day in days)
            {
                var availableWorkers = workers.OrderBy(w => workerCounts[w])
                                              .ThenBy(_ => random.Next())
                                              .Take(1)
                                              .ToList();

                schedule[day] = availableWorkers;
                foreach (var worker in availableWorkers)
                {
                    workerCounts[worker]++;
                }
            }
        }
    }

}
