﻿using HabitLoggerConsole.Models;
using Microsoft.Data.Sqlite;
using HabitLoggerConsole.HabitUpdates;
using System.Linq;

namespace HabitLoggerConsole;

internal class HabitCommands
{
    private static char exitChar = 'E';
    internal static void InsertHabit()
    {
        bool exitFunction = false;
        string name = "";
        string nameOfTheTrackingVariable = "";
        MeasurementType measurementType = 0;

        Console.WriteLine("Choose a name for the habit you want to start tracking.");
        Program.InsertExitPrompt(exitChar);

        while (true)
        {
            exitFunction = Program.AssingNameInput(ref name, "Your name must not be empty. Please, try inserting the habit's name again: ", exitChar: exitChar, excludeSymbols: true);
            if (exitFunction)
            {
                return;
            }
            if (IsTableNameDuplicate(name))
            {
                continue;
            }
            break;
        }
        Console.Clear();

        Console.WriteLine("Choose how you would like to measure your habit from the units listed below. Type in an index number.\n");
        Console.WriteLine($"{new string('-', Console.BufferWidth)}");
        var sortedMeasurements = MeasurementUnits.DisplayMeasurements();
        Console.WriteLine();
        Console.WriteLine($"{new string('-', Console.BufferWidth)}");
        Console.WriteLine();
        Program.InsertExitPrompt(exitChar);

        int measurementTypeLength = sortedMeasurements.Length;
        int userInput = 0;
        exitFunction = Program.AssignSelectionInput(ref userInput, 1, measurementTypeLength, skipSelection: exitChar);
        if (exitFunction) 
        { 
            return; 
        }
        measurementType = sortedMeasurements[userInput - 1];
        string stringMeasurementType = Enum.GetName(typeof(MeasurementType), measurementType);

        Console.Clear();
        Console.WriteLine("Please type in a name for what you are going to be tracking.");
        Program.InsertExitPrompt(exitChar);

        exitFunction = Program.AssingNameInput(ref nameOfTheTrackingVariable, "Your name must not be empty. Please, try inserting variable's name again: ", exitChar: exitChar, excludeSymbols: true);

        using (var connection = new SqliteConnection(Program.connectionString))
        {
            connection.Open();

            var tableCmd = connection.CreateCommand();

            tableCmd.CommandText = $"CREATE TABLE IF NOT EXISTS '{name}' (Id INTEGER PRIMARY KEY AUTOINCREMENT, Date TEXT, '{nameOfTheTrackingVariable}' TEXT, '{measurementType}' TEXT)";

            tableCmd.ExecuteNonQuery();

            connection.Close();
        }
    }
    internal static void ViewAllHabits()
    {
        Console.WriteLine("You are currently viewing all habits you have previously started.\n");
        Console.WriteLine($"{new string('-', Console.BufferWidth)}");
        Console.WriteLine();

        using (var connection = new SqliteConnection(Program.connectionString))
        {
            connection.Open();

            var tableCmd = connection.CreateCommand();

            tableCmd.CommandText = $"SELECT name FROM sqlite_master WHERE type = 'table'";

            SqliteDataReader reader = tableCmd.ExecuteReader();

            if (!reader.HasRows)
            {
                Console.Clear();
                Console.WriteLine("You are currently not tracking any habits. Please choose an option to track a new habit to view your list.");
                Console.Write("Press any key to return to the back menu: ");
                Console.ReadKey();
            }
            else
            {
                int paddingLength = 25;
                while (reader.Read())
                {
                    if(reader.GetString(0).Length > paddingLength)
                        paddingLength = reader.GetString(0).Length;
                }
                if (paddingLength > 20)
                    paddingLength += 5;

                reader.Close();
                reader = tableCmd.ExecuteReader();

                Console.WriteLine($"{"Number of entries: ".PadRight(paddingLength)}Habit name:");
                while (reader.Read())
                {
                    if (reader.GetString(0) == "sqlite_sequence")
                    {
                        continue;
                    }

                    var tableCmdRecordNumber = connection.CreateCommand();

                    tableCmdRecordNumber.CommandText = $"SELECT COUNT(*) FROM '{reader.GetString(0)}'";

                    var numberOfRecords = tableCmdRecordNumber.ExecuteScalar();

                    string displayName = TableNameToDisplayableFormat(reader.GetString(0));

                    Console.WriteLine($"{numberOfRecords.ToString().PadRight(paddingLength)}{displayName}");
                }
            }
            connection.Close();
        }
        Console.WriteLine();
        Console.WriteLine($"{new string('-', Console.BufferWidth)}");
        Console.Write($"\nPlease press any key to return to the main menu: ");
        Console.ReadKey();
    }
    internal static void UpdateHabit()
    {
        while (true)
        {
            using (var connection = new SqliteConnection(Program.connectionString))
            {
                connection.Open();

                var command = connection.CreateCommand();

                command.CommandText = $"SELECT name FROM sqlite_master WHERE type = 'table'";

                SqliteDataReader reader = command.ExecuteReader();

                if (!reader.HasRows)
                {
                    Console.WriteLine("You have no habits you can update! Please press any key to return to the main menu: ");
                    Console.ReadKey();
                    connection.Close();
                    return;
                }    

                Console.WriteLine("You are currently updating one of your habits.");
                Console.WriteLine("Below is the full list of the ones you have started to track: \n");
                Console.WriteLine($"{new string('-', Console.BufferWidth)}");
                Console.WriteLine();

                int counter = 0;
                List<string> habitNames = new List<string>();

                while (reader.Read())
                {
                    if (reader.GetString(0) == "sqlite_sequence")
                        continue;

                    counter ++;
                    habitNames.Add(reader.GetString(0));

                    string displayableName = TableNameToDisplayableFormat(reader.GetString(0));
                    Console.WriteLine($"{counter}) {displayableName}");
                }

                Console.WriteLine();
                Console.WriteLine($"{new string('-', Console.BufferWidth)}");
                Console.WriteLine();
                Console.WriteLine("Please select the index number of the habit you'd like to update.");
                Program.InsertExitPrompt(exitChar);

                int selectionInput = 0;
                bool shouldExit = Program.AssignSelectionInput(ref selectionInput, 1, counter, skipSelection: exitChar);
                if (shouldExit)
                {
                    connection.Close();
                    return;
                }

                string updatingHabit = habitNames[selectionInput - 1];
                shouldExit = HabitUpdate.RunHabitUpdateMenu(updatingHabit, connection, exitChar);
                if (shouldExit)
                {
                    connection.Close();
                    return;
                }
                Console.Clear();
            }
        }
    }
    internal static void DeleteHabit()
    {
        while (true)
        {
            using (var connection = new SqliteConnection(Program.connectionString))
            {
                connection.Open();

                var command = connection.CreateCommand();

                command.CommandText = $"SELECT name FROM sqlite_master WHERE type = 'table'";

                SqliteDataReader reader = command.ExecuteReader();

                if (!reader.HasRows)
                {
                    Console.WriteLine("You have no habits to delete. Please press any key to return to the main menu: ");
                    Console.ReadKey();
                    connection.Close();
                    return;
                }

                Console.WriteLine("You are currently on a deletion menu.");
                Console.WriteLine("Below is the full list of the ones you have started to track: \n");
                Console.WriteLine($"{new string('-', Console.BufferWidth)}");
                Console.WriteLine();

                int counter = 0;
                List<string> habitNames = new List<string>();

                while (reader.Read())
                {
                    if (reader.GetString(0) == "sqlite_sequence")
                        continue;

                    counter++;
                    habitNames.Add(reader.GetString(0));

                    string displayableName = TableNameToDisplayableFormat(reader.GetString(0));
                    Console.WriteLine($"{counter}) {displayableName}");
                }

                reader.Close();

                Console.WriteLine();
                Console.WriteLine($"{new string('-', Console.BufferWidth)}");
                Console.WriteLine();
                Console.WriteLine("Please select the index number of the habit you'd like to delete permanently.");
                Program.InsertExitPrompt(exitChar);

                int selectionInput = 0;
                bool shouldExit = Program.AssignSelectionInput(ref selectionInput, 1, counter, skipSelection: exitChar);
                if (shouldExit)
                {
                    connection.Close();
                    return;
                }

                string deletionHabit = habitNames[selectionInput - 1];

                Console.Clear();
                Console.WriteLine($"You are now deleting {deletionHabit} habit and its all records.");
                Console.Write($"Are you sure you want to continue?: Y - Yes/N - No. Type in your option: ");

                while (true)
                {
                    string answer = Console.ReadLine().ToString().ToLower();
                    if (answer == "y")
                    {
                        command.CommandText = $"DROP TABLE '{deletionHabit}'";

                        command.ExecuteNonQuery();

                        Console.Clear();
                        break;
                    }
                    else if (answer == "n")
                    {
                        Console.Clear();
                        break;
                    }
                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                    Console.Write($"{new string(' ', Console.BufferWidth)}");
                    Console.SetCursorPosition(0, Console.CursorTop);
                    Console.Write($"Please select \"Y\" to delete the habit table or \"N\" to go back to the previous menu. Your option: ");
                }
            }
        }
    }

    internal static bool IsTableNameDuplicate(string name)
    {
        using (var connection = new SqliteConnection(Program.connectionString))
        {
            connection.Open();

            var tableCmd = connection.CreateCommand();

            tableCmd.CommandText = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{name}'";

            SqliteDataReader reader = tableCmd.ExecuteReader();
            if (reader.HasRows)
            {
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                Console.Write($"{new string(' ', Console.BufferWidth)}");
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write("A habit with this name already exists. Please choose another habit to track: ");
                connection.Close();
                return true;
            }
            connection.Close();
            return false;
        }
    }

    internal static string TableNameToDisplayableFormat(string tableName)
    {
        tableName = tableName.Replace("_", " ");
        tableName = tableName[0].ToString().ToUpper() + tableName.Substring(1);
        return tableName;
    } 
}
