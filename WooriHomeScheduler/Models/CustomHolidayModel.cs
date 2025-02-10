using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WooriHomeScheduler.Models
{
    internal class CustomHolidayModel(DateTime dateTime, bool isFree)
    {
        public DateTime Date { get; set; } = dateTime;
        public string DateString => Date.ToString("yyyy-MM-dd(ddd)");
        public bool IsFree { get; set; } = isFree;
    }
}
