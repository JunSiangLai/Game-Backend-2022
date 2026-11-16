using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SERVER
{
    class Program
    {
        static void Main(string[] args)
        {
            Database db = new Database();
            db.Connect("localhost", 3306, "new_schema", "root", "YOUR_DB_PASSWORD" /* replace with your MySQL password, see README */);

            while (true)
            {
                DataTable results = db.Query("SELECT * FROM new_schema.student");
                List<Student> students = Database.DataTableToList<Student>(results);
                foreach (Student s in students)
                {
                    Console.WriteLine("#{0}: {1} {2}", s.id, s.first_name, s.last_name);
                }

                db.Update(Console.ReadLine());
            }
            db.Disconnect();
        }
    }

    public class Student
    {
        public int id { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
    }
}
