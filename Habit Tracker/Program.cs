using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Data.Sqlite;

namespace habit_tracker
{
    class Program
    {
        static string connectionString = @"Data Source=habit-Tracker.db";
        static void Main(string[] args)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                var tableCmd = connection.CreateCommand();

                // Create drinking_water table
                tableCmd.CommandText =
                    @"CREATE TABLE IF NOT EXISTS drinking_water (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Date TEXT,
                        Quantity INTEGER
                        )";
                tableCmd.ExecuteNonQuery();

                // Create coding_hours table
                tableCmd.CommandText =
                    @"CREATE TABLE IF NOT EXISTS coding_hours (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Date TEXT,
                        Hours INTEGER
                        )";
                tableCmd.ExecuteNonQuery();

                connection.Close();
            }

            GetUserInput();
        }

        static void GetUserInput()
        {
            Console.Clear();
            bool closeApp = false;
            while (closeApp == false)
            {
                Console.WriteLine("\n\nMAIN MENU");
                Console.WriteLine("\nWhat would you like to track?");
                Console.WriteLine("\nType 0 to Close Application.");
                Console.WriteLine("Type 1 for Water Drinking Tracker.");
                Console.WriteLine("Type 2 for Coding Hours Tracker.");
                Console.WriteLine("------------------------------------------\n");

                string command = Console.ReadLine() ?? "";

                switch (command)
                {
                    case "0":
                        Console.WriteLine("\nGoodbye!\n");
                        closeApp = true;
                        Environment.Exit(0);
                        break;
                    case "1":
                        WaterTrackingMenu();
                        break;
                    case "2":
                        CodingTrackingMenu();
                        break;
                    default:
                        Console.WriteLine("\nInvalid Command. Please type a number from 0 to 2.\n");
                        break;
                }
            }
        }

        static void WaterTrackingMenu()
        {
            Console.Clear();
            bool goBack = false;
            while (!goBack)
            {
                Console.WriteLine("\n\nWATER DRINKING TRACKER");
                Console.WriteLine("\nWhat would you like to do?");
                Console.WriteLine("\nType 0 to Return to Main Menu.");
                Console.WriteLine("Type 1 to View All Records.");
                Console.WriteLine("Type 2 to Insert Record.");
                Console.WriteLine("Type 3 to Delete Record.");
                Console.WriteLine("Type 4 to Update Record.");
                Console.WriteLine("------------------------------------------\n");

                string command = Console.ReadLine() ?? "";

                switch (command)
                {
                    case "0":
                        goBack = true;
                        break;
                    case "1":
                        GetAllRecords("drinking_water");
                        break;
                    case "2":
                        Insert("drinking_water", "millilitres");
                        break;
                    case "3":
                        Delete("drinking_water");
                        break;
                    case "4":
                        Update("drinking_water", "millilitres");
                        break;
                    default:
                        Console.WriteLine("\nInvalid Command. Please type a number from 0 to 4.\n");
                        break;
                }
            }
        }

        static void CodingTrackingMenu()
        {
            Console.Clear();
            bool goBack = false;
            while (!goBack)
            {
                Console.WriteLine("\n\nCODING HOURS TRACKER");
                Console.WriteLine("\nWhat would you like to do?");
                Console.WriteLine("\nType 0 to Return to Main Menu.");
                Console.WriteLine("Type 1 to View All Records.");
                Console.WriteLine("Type 2 to Insert Record.");
                Console.WriteLine("Type 3 to Delete Record.");
                Console.WriteLine("Type 4 to Update Record.");
                Console.WriteLine("------------------------------------------\n");

                string command = Console.ReadLine() ?? "";

                switch (command)
                {
                    case "0":
                        goBack = true;
                        break;
                    case "1":
                        GetAllRecords("coding_hours");
                        break;
                    case "2":
                        Insert("coding_hours", "hours");
                        break;
                    case "3":
                        Delete("coding_hours");
                        break;
                    case "4":
                        Update("coding_hours", "hours");
                        break;
                    default:
                        Console.WriteLine("\nInvalid Command. Please type a number from 0 to 4.\n");
                        break;
                }
            }
        }

        private static void GetAllRecords(string tableName)
        {
            Console.Clear();
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                var tableCmd = connection.CreateCommand();
                tableCmd.CommandText = $"SELECT * FROM {tableName}";

                List<HabitRecord> tableData = new();

                SqliteDataReader reader = tableCmd.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        try
                        {
                            string dateString = reader.GetString(1);
                            DateTime parsedDate;

                            
                            if (DateTime.TryParseExact(dateString, "dd-MM-yy", new CultureInfo("en-US"), DateTimeStyles.None, out parsedDate))
                            {
                                tableData.Add(new HabitRecord
                                {
                                    Id = reader.GetInt32(0),
                                    Date = parsedDate,
                                    Value = reader.GetInt32(2)
                                });
                            }
                            else
                            {
                                
                                if (DateTime.TryParse(dateString, out parsedDate))
                                {
                                    tableData.Add(new HabitRecord
                                    {
                                        Id = reader.GetInt32(0),
                                        Date = parsedDate,
                                        Value = reader.GetInt32(2)
                                    });
                                }
                                else
                                {
                                    Console.WriteLine($"Warning: Could not parse date for record Id {reader.GetInt32(0)}: '{dateString}'");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error reading record: {ex.Message}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("No rows found");
                }

                connection.Close();

                Console.WriteLine("------------------------------------------\n");
                string unit = tableName == "drinking_water" ? "ml" : "hours";
                foreach (var record in tableData)
                {
                    Console.WriteLine($"{record.Id} - {record.Date.ToString("dd-MMM-yyyy")} - {unit}: {record.Value}");
                }
                Console.WriteLine("------------------------------------------\n");
            }
        }

        private static void Insert(string tableName, string unit)
        {
            string date = GetDateInput();
            int value = GetNumberInput($"\n\nPlease insert number of {unit}\n\n");

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                var tableCmd = connection.CreateCommand();

                string columnName = tableName == "drinking_water" ? "quantity" : "hours";
                tableCmd.CommandText = $"INSERT INTO {tableName}(date, {columnName}) VALUES(@date, @value)";
                tableCmd.Parameters.AddWithValue("@date", date);
                tableCmd.Parameters.AddWithValue("@value", value);

                tableCmd.ExecuteNonQuery();
                connection.Close();
            }

            Console.WriteLine("\n\nRecord inserted successfully!\n\n");
        }

        private static void Delete(string tableName)
        {
            Console.Clear();
            GetAllRecords(tableName);

            var recordId = GetNumberInput("\n\nPlease type the Id of the record you want to delete or type 0 to go back\n\n");

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                var tableCmd = connection.CreateCommand();

                tableCmd.CommandText = $"DELETE from {tableName} WHERE Id = @id";
                tableCmd.Parameters.AddWithValue("@id", recordId);

                int rowCount = tableCmd.ExecuteNonQuery();

                if (rowCount == 0)
                {
                    Console.WriteLine($"\n\nRecord with Id {recordId} doesn't exist. \n\n");
                    connection.Close();
                    Delete(tableName);
                    return;
                }

                connection.Close();
            }

            Console.WriteLine($"\n\nRecord with Id {recordId} was deleted. \n\n");
        }

        internal static void Update(string tableName, string unit)
        {
            Console.Clear();
            GetAllRecords(tableName);

            var recordId = GetNumberInput("\n\nPlease type Id of the record would like to update. Type 0 to return to menu.\n\n");

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                var checkCmd = connection.CreateCommand();
                checkCmd.CommandText = $"SELECT EXISTS(SELECT 1 FROM {tableName} WHERE Id = @id)";
                checkCmd.Parameters.AddWithValue("@id", recordId);
                int checkQuery = Convert.ToInt32(checkCmd.ExecuteScalar());

                if (checkQuery == 0)
                {
                    Console.WriteLine($"\n\nRecord with Id {recordId} doesn't exist.\n\n");
                    connection.Close();
                    Update(tableName, unit);
                    return;
                }

                string date = GetDateInput();
                int value = GetNumberInput($"\n\nPlease insert number of {unit}\n\n");

                var tableCmd = connection.CreateCommand();
                string columnName = tableName == "drinking_water" ? "quantity" : "hours";
                tableCmd.CommandText = $"UPDATE {tableName} SET date = @date, {columnName} = @value WHERE Id = @id";
                tableCmd.Parameters.AddWithValue("@date", date);
                tableCmd.Parameters.AddWithValue("@value", value);
                tableCmd.Parameters.AddWithValue("@id", recordId);

                tableCmd.ExecuteNonQuery();
                connection.Close();
            }

            Console.WriteLine("\n\nRecord updated successfully!\n\n");
        }

        internal static string GetDateInput()
        {
            Console.WriteLine("\n\nPlease insert the date: (Format: dd-mm-yy). Type 0 to return to menu.\n\n");

            string dateInput = Console.ReadLine() ?? "";

            if (dateInput == "0") return null;

            while (!DateTime.TryParseExact(dateInput, "dd-MM-yy", new CultureInfo("en-US"), DateTimeStyles.None, out _))
            {
                Console.WriteLine("\n\nInvalid date. (Format: dd-mm-yy). Type 0 to return to menu or try again:\n\n");
                dateInput = Console.ReadLine() ?? "";
                if (dateInput == "0") return null;
            }

            return dateInput;
        }

        internal static int GetNumberInput(string message)
        {
            Console.WriteLine(message);

            string numberInput = Console.ReadLine() ?? "";

            if (numberInput == "0") return 0;

            while (!Int32.TryParse(numberInput, out _) || Convert.ToInt32(numberInput) < 0)
            {
                Console.WriteLine("\n\nInvalid number. Try again.\n\n");
                numberInput = Console.ReadLine() ?? "";
                if (numberInput == "0") return 0;
            }

            int finalInput = Convert.ToInt32(numberInput);
            return finalInput;
        }
    }

    public class HabitRecord
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public int Value { get; set; }
    }
}