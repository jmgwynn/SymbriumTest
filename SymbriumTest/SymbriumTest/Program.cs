using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;
using System.Data.SQLite;
using System.Threading.Tasks;

namespace SymbriumTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(DateTime.Now);
            DataClass DC = new DataClass();
            Console.ReadLine();
        }

        class DataClass
        {
            private string NameAndNotes;
            private string DBName;
            private string FilePath;
            private int NumOfValues;
            private double ErrorCriteria;
            public SQLiteConnection sqlite;

            public DataClass()
            {

                Console.WriteLine("Please provide your name and any notes.");
                NameAndNotes = Console.ReadLine();
                bool check = true;
                while (check)
                {
                    Console.WriteLine("What is the name of the database?");
                    DBName = Console.ReadLine();
                    Console.WriteLine("What is the file path of the folder the database is in?");
                    FilePath = Console.ReadLine();
                    FilePath = FilePath + "\\" + DBName + ".DB3";
                    if (!checkIfValid())
                    {
                        Console.WriteLine("Inaccessable file path. Please Try again");
                    }
                    else
                    {
                        check = false;
                    }
                }
                sqlite = new SQLiteConnection("Data Source=" + FilePath);
                NumOfValues = 0;
                DetermineNumOfInvValues();
                InputErrorCriteria();
                ParseDataBase();
            }

            //Makes sure the file path is valid
            public bool checkIfValid()
            {
                //Credit where credit is due, I got this method on stack overflow when trying to figure out the best way to do this.
                //https://stackoverflow.com/questions/38519688/how-do-i-check-if-a-file-is-a-sqlite-database-in-c
                try
                { 
                    byte[] bytes = new byte[17];
                    using (System.IO.FileStream fs = new System.IO.FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        fs.Read(bytes, 0, 16);
                    }
                    string chkStr = System.Text.ASCIIEncoding.ASCII.GetString(bytes);
                    return chkStr.Contains("SQLite format");
                }
                catch
                {
                    return false;
                }
            }

            //Runs through the database to determine how many values are in it.
            public void DetermineNumOfInvValues()
            {
                sqlite.Open();
                SQLiteCommand command = new SQLiteCommand("select * from invValues order by valueFloat desc", sqlite);
                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    NumOfValues++;
                }
                Console.WriteLine("Number of Values in the Database: " + NumOfValues);
                sqlite.Close();
            }

            //Asks the user for their error criteria
            public void InputErrorCriteria()
            {
                Console.WriteLine("What is your error criteria?\nInput a value for X between 4 and 8 (inclusive) for \"error<=10^-X\".");
                bool check = true;
                while (check)
                {
                    ErrorCriteria = Convert.ToDouble(Console.ReadLine());
                    if (ErrorCriteria >= 4 && ErrorCriteria <= 8)
                    {
                        check = false;
                    }
                    else
                    {
                        Console.WriteLine("Not a valid input, please try again.");
                    }
                }
                ErrorCriteria *= -1;
            }

            //Goes through the database and solves the problem for each value, and stores it in the results database
            public void ParseDataBase()
            {
                sqlite.Open();
                SQLiteCommand command = new SQLiteCommand("select * from invValues order by valueFloat desc", sqlite);
                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    double valueInRads = SolveForValue((double)reader["valueFloat"]);
                    double valueInDegrees = valueInRads * 180 / Math.PI;
                    DateTime timeStamp = DateTime.Now;
                    SQLiteCommand command2 = new SQLiteCommand("insert into results (result_uid, valueRad, valueDeg, error, time, inv_uid) values (NULL, " + valueInRads + ", " + valueInDegrees + ", " + 
                        ErrorCriteria + ", " + timeStamp.Ticks + ", "+ reader["inv_uid"] + ")", sqlite);
                    command2.ExecuteNonQuery();
                }
                sqlite.Close();
            }

            //Uses the involute function to find phi^
            private double SolveForValue(double angle)
            {
                double val = Math.Pow(10, ErrorCriteria) + InvoluteFunction(angle);
                return InvoluteFunction(val);
            }

            //involute function of an angle
            private double InvoluteFunction(double angle)
            {
                double result = Math.Tan(angle) - angle;
                return result;
            }
        }
    }
}