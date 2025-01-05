using System.Windows.Controls;

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
                schedule[day] = new List<string>();

                while (schedule[day].Count < 4)
                {
                    var worker = workerQueue.Dequeue();
                    workerCount[worker]++;

                    schedule[day].Add($"{worker}({workerCount[worker]})");
                    workerQueue.Enqueue(worker);
                }
            }

            // 추가 배치
            //var additionalDays = holidays.Count(h => h.DayOfWeek != DayOfWeek.Wednesday) * 4;
            //foreach (var day in EachDay(startDate, endDate).Where(d => weekdays.Contains(d.DayOfWeek)))
            //{
            //    if (additionalDays == 0) break;

            //    if (!schedule.ContainsKey(day))
            //    {
            //        schedule[day] = new List<string>();
            //    }

            //    if (schedule[day].Count < 5)
            //    {
            //        var additionalWorker = additionalWorkersQueue.Dequeue();
            //        if (!workerCount.ContainsKey(additionalWorker)) workerCount[additionalWorker] = 0;
            //        workerCount[additionalWorker]++;

            //        if (!schedule[day].Contains(additionalWorker))
            //        {
            //            schedule[day].Add($"{additionalWorker}({workerCount[additionalWorker]})");
            //            additionalWorkersQueue.Enqueue(additionalWorker);
            //            additionalDays--;
            //        }
            //        else
            //        {
            //            additionalWorkersQueue.Enqueue(additionalWorker);
            //        }
            //    }
            //}

            return schedule;
        }

        private static IEnumerable<DateTime> EachDay(DateTime from, DateTime to)
        {
            for (var day = from.Date; day <= to.Date; day = day.AddDays(1))
                yield return day;
        }
    }

}
