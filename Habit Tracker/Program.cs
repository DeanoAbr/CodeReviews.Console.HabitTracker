using System.Globalization;
using Microsoft.Data.Sqlite;

namespace habit_tracker
{
    class Program
    {
        private const string ConnectionString = @"Data Source=habit-tracker.db";
        private static readonly CultureInfo DateCulture = new("en-US");

        static void Main(string[] args)
        {
            InitializeDatabase();
            GetUserInput();
        }

        private static void InitializeDatabase()
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using var pragmaCmd = connection.CreateCommand();
            pragmaCmd.CommandText = "PRAGMA foreign_keys = ON";
            pragmaCmd.ExecuteNonQuery();

            using var tableCmd = connection.CreateCommand();
            tableCmd.CommandText =
                @"CREATE TABLE IF NOT EXISTS habits (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL UNIQUE,
                    Unit TEXT NOT NULL
                )";
            tableCmd.ExecuteNonQuery();

            tableCmd.CommandText =
                @"CREATE TABLE IF NOT EXISTS records (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    HabitId INTEGER NOT NULL,
                    Date TEXT NOT NULL,
                    Value INTEGER NOT NULL,
                    FOREIGN KEY(HabitId) REFERENCES habits(Id)
                )";
            tableCmd.ExecuteNonQuery();

            SeedDefaultHabits(connection);
            MigrateOldHabitTables(connection);
        }

        private static void SeedDefaultHabits(SqliteConnection connection)
        {
            InsertHabit(connection, "Water Drinking", "millilitres");
            InsertHabit(connection, "Coding Hours", "hours");
        }

        private static void InsertHabit(SqliteConnection connection, string name, string unit)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT OR IGNORE INTO habits (Name, Unit) VALUES (@name, @unit)";
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@unit", unit);
            cmd.ExecuteNonQuery();
        }

        private static void MigrateOldHabitTables(SqliteConnection connection)
        {
            MigrateOldTable(connection, "drinking_water", "Quantity", "Water Drinking");
            MigrateOldTable(connection, "coding_hours", "Hours", "Coding Hours");
        }

        private static void MigrateOldTable(SqliteConnection connection, string oldTableName, string oldValueColumn, string habitName)
        {
            if (!TableExists(connection, oldTableName)) return;

            Habit habit = GetHabitByName(connection, habitName);

            using var cmd = connection.CreateCommand();
            cmd.CommandText =
                $@"INSERT INTO records (HabitId, Date, Value)
                   SELECT @habitId, Date, {oldValueColumn}
                   FROM {oldTableName}
                   WHERE NOT EXISTS (
                       SELECT 1
                       FROM records
                       WHERE HabitId = @habitId
                         AND Date = {oldTableName}.Date
                         AND Value = {oldTableName}.{oldValueColumn}
                   )";
            cmd.Parameters.AddWithValue("@habitId", habit.Id);
            cmd.ExecuteNonQuery();
        }

        private static bool TableExists(SqliteConnection connection, string tableName)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = @tableName";
            cmd.Parameters.AddWithValue("@tableName", tableName);

            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        static void GetUserInput()
        {
            bool closeApp = false;
            while (!closeApp)
            {
                Console.Clear();
                List<Habit> habits = GetAllHabits();

                Console.WriteLine("\n\nMAIN MENU");
                Console.WriteLine("\nWhat would you like to track?");
                Console.WriteLine("\nType 0 to Close Application.");

                foreach (Habit habit in habits)
                {
                    Console.WriteLine($"Type {habit.Id} for {habit.Name} Tracker.");
                }

                Console.WriteLine("------------------------------------------\n");

                int command = GetNumberInput("");

                if (command == 0)
                {
                    Console.WriteLine("\nGoodbye!\n");
                    closeApp = true;
                    continue;
                }

                Habit? selectedHabit = habits.FirstOrDefault(habit => habit.Id == command);

                if (selectedHabit == null)
                {
                    Console.WriteLine("\nInvalid Command. Please choose one of the menu options.\n");
                    PressAnyKeyToContinue();
                    continue;
                }

                HabitTrackingMenu(selectedHabit);
            }
        }

        private static void HabitTrackingMenu(Habit habit)
        {
            bool goBack = false;
            while (!goBack)
            {
                Console.Clear();
                Console.WriteLine($"\n\n{habit.Name.ToUpper()} TRACKER");
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
                        GetAllRecords(habit);
                        PressAnyKeyToContinue();
                        break;
                    case "2":
                        Insert(habit);
                        PressAnyKeyToContinue();
                        break;
                    case "3":
                        Delete(habit);
                        PressAnyKeyToContinue();
                        break;
                    case "4":
                        Update(habit);
                        PressAnyKeyToContinue();
                        break;
                    default:
                        Console.WriteLine("\nInvalid Command. Please type a number from 0 to 4.\n");
                        PressAnyKeyToContinue();
                        break;
                }
            }
        }

        private static List<Habit> GetAllHabits()
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, Unit FROM habits ORDER BY Id";

            using SqliteDataReader reader = cmd.ExecuteReader();
            List<Habit> habits = new();

            while (reader.Read())
            {
                habits.Add(new Habit
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Unit = reader.GetString(2)
                });
            }

            return habits;
        }

        private static Habit GetHabitByName(SqliteConnection connection, string habitName)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, Unit FROM habits WHERE Name = @name";
            cmd.Parameters.AddWithValue("@name", habitName);

            using SqliteDataReader reader = cmd.ExecuteReader();
            reader.Read();

            return new Habit
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Unit = reader.GetString(2)
            };
        }

        private static void GetAllRecords(Habit habit)
        {
            Console.Clear();
            List<HabitRecord> records = GetRecordsForHabit(habit.Id);

            Console.WriteLine("------------------------------------------\n");

            if (records.Count == 0)
            {
                Console.WriteLine("No rows found");
            }

            foreach (HabitRecord record in records)
            {
                Console.WriteLine($"{record.Id} - {record.Date:dd-MMM-yyyy} - {habit.Unit}: {record.Value}");
            }

            Console.WriteLine("------------------------------------------\n");
        }

        private static List<HabitRecord> GetRecordsForHabit(int habitId)
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText =
                @"SELECT Id, Date, Value
                  FROM records
                  WHERE HabitId = @habitId
                  ORDER BY Date";
            cmd.Parameters.AddWithValue("@habitId", habitId);

            using SqliteDataReader reader = cmd.ExecuteReader();
            List<HabitRecord> records = new();

            while (reader.Read())
            {
                string dateString = reader.GetString(1);

                if (TryParseDate(dateString, out DateTime parsedDate))
                {
                    records.Add(new HabitRecord
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

            return records;
        }

        private static void Insert(Habit habit)
        {
            string? date = GetDateInput();
            if (date == null) return;

            int value = GetNumberInput($"\n\nPlease insert number of {habit.Unit}\n\n");
            if (value == 0) return;

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO records (HabitId, Date, Value) VALUES (@habitId, @date, @value)";
            cmd.Parameters.AddWithValue("@habitId", habit.Id);
            cmd.Parameters.AddWithValue("@date", date);
            cmd.Parameters.AddWithValue("@value", value);

            cmd.ExecuteNonQuery();

            Console.WriteLine("\n\nRecord inserted successfully!\n\n");
        }

        private static void Delete(Habit habit)
        {
            Console.Clear();
            GetAllRecords(habit);

            int recordId = GetNumberInput("\n\nPlease type the Id of the record you want to delete or type 0 to go back\n\n");
            if (recordId == 0) return;

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM records WHERE Id = @id AND HabitId = @habitId";
            cmd.Parameters.AddWithValue("@id", recordId);
            cmd.Parameters.AddWithValue("@habitId", habit.Id);

            int rowCount = cmd.ExecuteNonQuery();

            if (rowCount == 0)
            {
                Console.WriteLine($"\n\nRecord with Id {recordId} doesn't exist for this habit.\n\n");
                return;
            }

            Console.WriteLine($"\n\nRecord with Id {recordId} was deleted.\n\n");
        }

        internal static void Update(Habit habit)
        {
            Console.Clear();
            GetAllRecords(habit);

            int recordId = GetNumberInput("\n\nPlease type Id of the record you would like to update. Type 0 to return to menu.\n\n");
            if (recordId == 0) return;

            if (!RecordExists(recordId, habit.Id))
            {
                Console.WriteLine($"\n\nRecord with Id {recordId} doesn't exist for this habit.\n\n");
                return;
            }

            string? date = GetDateInput();
            if (date == null) return;

            int value = GetNumberInput($"\n\nPlease insert number of {habit.Unit}\n\n");
            if (value == 0) return;

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "UPDATE records SET Date = @date, Value = @value WHERE Id = @id AND HabitId = @habitId";
            cmd.Parameters.AddWithValue("@date", date);
            cmd.Parameters.AddWithValue("@value", value);
            cmd.Parameters.AddWithValue("@id", recordId);
            cmd.Parameters.AddWithValue("@habitId", habit.Id);

            cmd.ExecuteNonQuery();

            Console.WriteLine("\n\nRecord updated successfully!\n\n");
        }

        private static bool RecordExists(int recordId, int habitId)
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT EXISTS(SELECT 1 FROM records WHERE Id = @id AND HabitId = @habitId)";
            cmd.Parameters.AddWithValue("@id", recordId);
            cmd.Parameters.AddWithValue("@habitId", habitId);

            return Convert.ToInt32(cmd.ExecuteScalar()) == 1;
        }

        internal static string? GetDateInput()
        {
            Console.WriteLine("\n\nPlease insert the date: (Format: dd-mm-yy). Type 0 to return to menu.\n\n");

            string dateInput = Console.ReadLine() ?? "";

            if (dateInput == "0") return null;

            while (!TryParseDate(dateInput, out _))
            {
                Console.WriteLine("\n\nInvalid date. (Format: dd-mm-yy). Type 0 to return to menu or try again:\n\n");
                dateInput = Console.ReadLine() ?? "";
                if (dateInput == "0") return null;
            }

            return dateInput;
        }

        internal static int GetNumberInput(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                Console.WriteLine(message);
            }

            string numberInput = Console.ReadLine() ?? "";

            if (numberInput == "0") return 0;

            while (!int.TryParse(numberInput, out int parsedInput) || parsedInput < 0)
            {
                Console.WriteLine("\n\nInvalid number. Try again.\n\n");
                numberInput = Console.ReadLine() ?? "";
                if (numberInput == "0") return 0;
            }

            return Convert.ToInt32(numberInput);
        }

        private static bool TryParseDate(string dateInput, out DateTime parsedDate)
        {
            return DateTime.TryParseExact(dateInput, "dd-MM-yy", DateCulture, DateTimeStyles.None, out parsedDate)
                || DateTime.TryParse(dateInput, out parsedDate);
        }

        private static void PressAnyKeyToContinue()
        {
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
    }

    public class Habit
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Unit { get; set; } = "";
    }

    public class HabitRecord
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public int Value { get; set; }
    }
}
