using Accord.Genetic;
using DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BL
{
    #region TimeTableChromosome
    class TimeTableChromosome : ChromosomeBase
    {
        HoursBL hoursBL = new HoursBL();
        Subject_Teacher_ClassBL subject_Teacher_ClassBL = new Subject_Teacher_ClassBL();
        private readonly DBConnection dBConnection;
        static Random Random = new Random();
        public List<Hours_Schedule> Value;

        int SchoolCode;
        //פונקציה המגרילה שעה מתוך נתוני טבלת השעות בדטה בביס
        public int RandomStartTime(List<int> listNam)
        {

            int num = Random.Next(0,listNam.Count()-1);
            return listNam[num];
        }
        //פעולה בונה המקבל קישור לדטה בביס וקוד בית ספר ויוצרת מערכת ראשונית
        public TimeTableChromosome(DBConnection db, int schoolCode)
        {
            dBConnection = db;
            SchoolCode = schoolCode;
            Generate();
        }
        //פעולה בנוה המקבלת מערכת וקישור לדטה בייס ומאתחלת את המערכת שהתקבלה להיות המערכת של העצם שעליו הופעל הפונקציה
        public TimeTableChromosome(List<Hours_Schedule> slots, DBConnection db)
        {
            dBConnection = db;
            Value = slots.ToList()
                .ToList();
        }
        //פונקציה לחלוקה אקראית ראשונית
        public override void Generate()
        {
            IEnumerable<Hours_Schedule> generateRandomSlots()
            {
                Hours_ScheduleBL ho = new Hours_ScheduleBL();
                HoursBL hoursBL = new HoursBL();
                List<HouersScheduleModel> hours_Schedules = ho.GetAllHours_Schedule().Where(a => a.Hours_Schedule_School_ID == SchoolCode).ToList();

                Dictionary<int, List<Subject_Teacher_Class>> s_t_c = new Dictionary<int, List<Subject_Teacher_Class>>();
                for(int i=1;i<5;i++)
                { 
                List<Subject_Teacher_Class> listsubject_Teacher_Classes = subject_Teacher_ClassBL.GetAllSubject_Teacher_Class2()
                    .Where(a => a.Subject_Teacher_Class_School_ID == SchoolCode && a.Class_Code==i).ToList();
                s_t_c.Add(i,listsubject_Teacher_Classes);
                }
                for(int j =1;j<=s_t_c.Count();j++)
                {
                    Dictionary<int, List<int>> r = new Dictionary<int, List<int>>();
                    r.Add(1, new List<int>() { 8, 9, 10, 11, 12, 13, 14 });
                    r.Add(2, new List<int>() { 8, 9, 10, 11, 12, 13, 14 });
                    r.Add(3, new List<int>() { 8, 9, 10, 11, 12, 13, 14 });
                    r.Add(4, new List<int>() { 8, 9, 10, 11, 12, 13, 14 });
                    r.Add(5, new List<int>() { 8, 9, 10, 11, 12, 13, 14 });
                    int num;
                    int Day;
                    foreach (var stc in s_t_c[j])
                    {
                        Day = RandomStartTime(r.Keys.ToList());
                        num = Random.Next(0, r[Day].Count());
                        yield return new Hours_Schedule()
                        {
                            Hours_Schedule_Code = 90000,
                            Day_code = Day,
                            Hour_code_start = (r[Day][num]),
                            Hour_code_end = (r[Day][num]),
                            STC_Code = stc.Code,
                            Hours_Schedule_School_ID = stc.Subject_Teacher_Class_School_ID
                        };
                        r[Day].RemoveAt(num);
                        if (r[Day].Count == 0)
                            r.Remove(Day);
                    }
                }
               
            }
            Value = generateRandomSlots().ToList();
        }
        //יוצרת מערכת חדשה 
        public override IChromosome CreateNew()
        {
            var timeTableChromosome = new TimeTableChromosome(dBConnection, SchoolCode);
            timeTableChromosome.Generate();
            return timeTableChromosome;
        }
        //שכפול המערכת
        public override IChromosome Clone()
        {
            return new TimeTableChromosome(Value, dBConnection);
        }
        //מוטציה - שינוי אקראי
        public override void Mutate()
        {
            var index = Random.Next(0, Value.Count - 1);
            var timeSlotChromosome = Value.ElementAt(index);
            int num = Random.Next(8, 15);
            timeSlotChromosome.Hour_code_start = num;
            timeSlotChromosome.Hour_code_end = num;
            timeSlotChromosome.Day_code = Random.Next(1, 6);
            Value[index] = timeSlotChromosome;
        }
        //חיבור של 2 המערכות הטובות
        public override void Crossover(IChromosome pair)
        {
            var randomVal = Random.Next(0, Value.Count - 2);
            var otherChromsome = pair as TimeTableChromosome;
            for (int index = randomVal; index < otherChromsome.Value.Count; index++)
            {
                Value[index] = otherChromsome.Value[index];
            }
        }
        //קלאס הנותן ציון לכל מערכת 
        public class FitnessFunction : IFitnessFunction
        {
            Subject_Teacher_ClassBL subject_Teacher_ClassBL = new Subject_Teacher_ClassBL();
            ConstraintsBL constraintsBL = new ConstraintsBL();
            public double Evaluate(IChromosome chromosome)
            {
                double score = 1;
                double sc = 0;
                var values = (chromosome as TimeTableChromosome).Value;
                var GetoverLaps = new Func<Hours_Schedule, List<Hours_Schedule>>(current => values.Except(new[] { current })
                .Where(slot => slot.Day_code == current.Day_code)
                .Where(slot => slot.Hour_code_start == current.Hour_code_start)
                        .ToList());
                foreach (var value in values)
                {
                    var overLaps = GetoverLaps(value);
                    var STC = overLaps.Select(slot => slot.STC_Code).ToList();
                    if (STC.Count() > 1)
                        sc = score;
                    //כמה מורות חוזרות על עצמם באותם תווך שעות
                    List<string> techers = new List<string>();
                    foreach (var stc in STC)
                    {
                        var t = subject_Teacher_ClassBL.GetAllSubject_Teacher_Class()
                             .Where(s => s.Subject_Teacher_Class_School_ID == (chromosome as TimeTableChromosome).SchoolCode).FirstOrDefault(s => s.Code == stc);
                        if (t != null)
                            techers.Add(t.Teacher_Id);
                    }
                    score -= techers.GroupBy(t => t).Sum(x => x.Count());
                    //כמה כיתות לומדות באותם תווך שעות
                    List<int?> classes = new List<int?>();
                    foreach (var stc in STC)
                    {

                        var c = subject_Teacher_ClassBL.GetAllSubject_Teacher_Class()
                            .FirstOrDefault(s => s.Code == stc);
                        if (c != null)
                            classes.Add(c.Class_Code);
                    }
                    score -= classes.GroupBy(t => t).Sum(x => x.Count());
                    //כמה מקצועות חוזרים באותם תווך שעות
                    List<int?> subjects = new List<int?>();
                    foreach (var stc in STC)
                    {
                        var su = subject_Teacher_ClassBL.GetAllSubject_Teacher_Class()
                            .FirstOrDefault(s => s.Code == stc
                        && s.Class_Code == subject_Teacher_ClassBL.listSubject_Teacher_Class[(int.Parse(stc.ToString())) - 1].Class_Code
                        );
                        if (su != null)
                            subjects.Add(su.Subject_Code);
                    }
                    score -= subjects.GroupBy(t => t).Sum(x => x.Count());
                }
                //אילוצים
                List<Constraints> listconstraints = constraintsBL.GetAllConstraints2();
                //רשימת המורות שבכל משבצת במערכת
                List<string> alltechers = new List<string>();
                foreach (var value in values)
                {
                    var t = subject_Teacher_ClassBL.GetAllSubject_Teacher_Class()
                        .FirstOrDefault(s => s.Code == value.STC_Code);
                    if (t != null)
                        alltechers.Add(t.Teacher_Id);
                }
                //להוריד נקודה לכל מורה שהאילוץ שלה לא מתקיים לפי המערכת
                foreach (var constraints in listconstraints)
                {
                    foreach (var techer in alltechers)
                    {
                        if (techer == constraints.Teacher_Id)
                        {
                            foreach (var slot in values)
                            {
                                if (constraints.Day_code == slot.Day_code&&constraints.Hoer_code==slot.Hour_code_start)
                                    score--;
                            }
                        }
                    }

                }
                return Math.Pow(Math.Abs(score), -1);
            }
        }
    }
    #endregion
}