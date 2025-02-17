using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WooriHomeScheduler.Models
{
    internal class CustomWorkdayModel(DateTime dateTime, int workerCount)
    {
        public DateTime Date { get; set; } = dateTime;
        public string DateString => Date.ToString("yyyy-MM-dd(ddd)");
        public int WorkerCount { get; set; } = workerCount;
    }
}
