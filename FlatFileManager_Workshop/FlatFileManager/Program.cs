using FlatFileManager;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Taller6
{
    internal class Program
    {
        private const string FilePersons = "Persons.txt";
        private const string FileUsers = "Users.txt";
        private const string FileLog = "log.txt";

        private static string activeUser = "";

        private static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            InitializeFiles();

            if (!Login()) return;

            int option;
            do
            {
                ShowMenu();
                option = ReadOption();

                switch (option)
                {
                    case 1: ShowPersons(); break;
                    case 2: AddPerson(); break;
                    case 3: EditPerson(); break;
                    case 4: DeletePerson(); break;
                    case 5: SaveChanges(); break;
                    case 6: ShowReport(); break;
                    case 0: Console.WriteLine("\nGoodbye!"); break;
                    default: Console.WriteLine("\nInvalid option."); break;
                }
            } while (option != 0);
        }

        // ════════════════════════════════════════════════════════════════════════
        // INITIALIZE
        // ════════════════════════════════════════════════════════════════════════

        private static void InitializeFiles()
        {
            if (!File.Exists(FileUsers))
            {
                File.WriteAllLines(FileUsers, new[]
                {
                    "jzuluaga,P@ssw0rd123!,true",
                    "mbedoya,S0yS3gur02025*,true"
                });
            }

            if (!File.Exists(FilePersons))
            {
                File.WriteAllLines(FilePersons, new[]
                {
                    "1|Maria|Bedoya|322 311 4015|Medellín|15000.00",
                    "2|Juan|Zuluaga|322 311 4620|Medellín|8200.00",
                    "3|Brad|Pit|322 450 4545|Miami|14000000.00"
                });
            }

            if (!File.Exists(FileLog))
                File.WriteAllText(FileLog, "");
        }

        // ════════════════════════════════════════════════════════════════════════
        // LOGIN
        // ════════════════════════════════════════════════════════════════════════

        private static bool Login()
        {
            Console.Clear();
            Console.WriteLine("╔══════════════════════════════════════════╗");
            Console.WriteLine("║           MANAGEMENT SYSTEM - LOGIN      ║");
            Console.WriteLine("╚══════════════════════════════════════════╝");

            List<User> users = LoadUsers();
            int attempts = 0;

            while (attempts < 3)
            {
                Console.Write("\nUsername: ");
                string userName = Console.ReadLine()?.Trim() ?? "";
                Console.Write("Password: ");
                string password = ReadPassword();

                User u = users.FirstOrDefault(x =>
                    x.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase));

                if (u == null)
                {
                    attempts++;
                    Console.WriteLine($"\n✗ User not found. Attempts: {attempts}/3");
                    WriteLog("SYSTEM", $"Failed login - user '{userName}' does not exist.");
                    continue;
                }

                if (!u.IsActive)
                {
                    Console.WriteLine("\n✗ User is blocked. Contact administrator.");
                    WriteLog("SYSTEM", $"Blocked user tried to login: '{userName}'.");
                    return false;
                }

                if (u.Password == password)
                {
                    u.Attempts = 0;
                    SaveUsers(users);
                    activeUser = u.UserName;
                    Console.WriteLine($"\n✓ Welcome, {activeUser}!\n");
                    WriteLog(activeUser, "Successful login.");
                    return true;
                }
                else
                {
                    u.Attempts++;
                    attempts++;
                    Console.WriteLine($"\n✗ Wrong password. Remaining attempts: {3 - attempts}");
                    WriteLog("SYSTEM", $"Wrong password for '{userName}'. Attempt {attempts}/3.");

                    if (attempts >= 3)
                    {
                        u.IsActive = false;
                        SaveUsers(users);
                        Console.WriteLine($"\n✗ User '{userName}' has been blocked.");
                        WriteLog("SYSTEM", $"User '{userName}' blocked after 3 failed attempts.");
                        return false;
                    }
                }
            }
            return false;
        }

        private static string ReadPassword()
        {
            string pass = "";
            ConsoleKeyInfo key;
            do
            {
                key = Console.ReadKey(true);
                if (key.Key != ConsoleKey.Enter && key.Key != ConsoleKey.Backspace)
                {
                    pass += key.KeyChar;
                    Console.Write("*");
                }
                else if (key.Key == ConsoleKey.Backspace && pass.Length > 0)
                {
                    pass = pass[..^1];
                    Console.Write("\b \b");
                }
            } while (key.Key != ConsoleKey.Enter);
            Console.WriteLine();
            return pass;
        }

        // ════════════════════════════════════════════════════════════════════════
        // MENU
        // ════════════════════════════════════════════════════════════════════════

        private static void ShowMenu()
        {
            Console.WriteLine("\n==========================================");
            Console.WriteLine("1. Show persons");
            Console.WriteLine("2. Add person");
            Console.WriteLine("3. Edit person");
            Console.WriteLine("4. Delete person");
            Console.WriteLine("5. Save changes");
            Console.WriteLine("6. Report by city");
            Console.WriteLine("0. Exit");
            Console.WriteLine("==========================================");
            Console.Write("Choose an option: ");
        }

        private static int ReadOption()
        {
            if (int.TryParse(Console.ReadLine(), out int op)) return op;
            return -1;
        }

        // ════════════════════════════════════════════════════════════════════════
        // SHOW PERSONS
        // ════════════════════════════════════════════════════════════════════════

        private static void ShowPersons()
        {
            List<Person> persons = LoadPersons();
            Console.WriteLine("\n==========================================");
            if (persons.Count == 0)
                Console.WriteLine("No persons registered.");
            else
                foreach (var p in persons)
                {
                    Console.WriteLine($"{p.Id,-6}{p.FirstName} {p.LastName}");
                    Console.WriteLine($"      Phone:   {p.Phone}");
                    Console.WriteLine($"      City:    {p.City}");
                    Console.WriteLine($"      Balance: {p.Balance:C}\n");
                }
            Console.WriteLine("==========================================");
            WriteLog(activeUser, "Viewed persons list.");
        }

        // ════════════════════════════════════════════════════════════════════════
        // ADD PERSON
        // ════════════════════════════════════════════════════════════════════════

        private static void AddPerson()
        {
            List<Person> persons = LoadPersons();
            Console.WriteLine("\n── ADD PERSON ───────────────────────────");

            int id;
            while (true)
            {
                Console.Write("ID (unique number): ");
                string input = Console.ReadLine()?.Trim() ?? "";
                if (!int.TryParse(input, out id) || id <= 0)
                { Console.WriteLine("✗ ID must be a positive number."); continue; }
                if (persons.Any(p => p.Id == id))
                { Console.WriteLine("✗ ID already exists. Enter a different one."); continue; }
                break;
            }

            string firstName;
            while (true)
            {
                Console.Write("First Name: ");
                firstName = Console.ReadLine()?.Trim() ?? "";
                if (!ValidateName(firstName))
                { Console.WriteLine("✗ Invalid first name (letters only, min 2 characters)."); continue; }
                break;
            }

            string lastName;
            while (true)
            {
                Console.Write("Last Name: ");
                lastName = Console.ReadLine()?.Trim() ?? "";
                if (!ValidateName(lastName))
                { Console.WriteLine("✗ Invalid last name (letters only, min 2 characters)."); continue; }
                break;
            }

            string phone;
            while (true)
            {
                Console.Write("Phone (e.g. 322 311 4015): ");
                phone = Console.ReadLine()?.Trim() ?? "";
                if (!ValidatePhone(phone))
                { Console.WriteLine("✗ Invalid phone. Use 10 digits (e.g. 322 311 4015)."); continue; }
                break;
            }

            Console.Write("City: ");
            string city = Console.ReadLine()?.Trim() ?? "Unknown";
            if (string.IsNullOrWhiteSpace(city)) city = "Unknown";

            decimal balance;
            while (true)
            {
                Console.Write("Balance (positive number): ");
                string input = Console.ReadLine()?.Trim() ?? "";
                if (!decimal.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out balance) || balance < 0)
                { Console.WriteLine("✗ Balance must be a positive number."); continue; }
                break;
            }

            persons.Add(new Person { Id = id, FirstName = firstName, LastName = lastName, Phone = phone, City = city, Balance = balance });
            SavePersons(persons);
            Console.WriteLine($"\n✓ Person '{firstName} {lastName}' added successfully.");
            WriteLog(activeUser, $"Added person ID={id}: {firstName} {lastName}, City={city}, Balance={balance:F2}.");
        }

        // ════════════════════════════════════════════════════════════════════════
        // EDIT PERSON
        // ════════════════════════════════════════════════════════════════════════

        private static void EditPerson()
        {
            List<Person> persons = LoadPersons();
            Console.WriteLine("\n── EDIT PERSON ──────────────────────────");
            Console.Write("Enter ID to edit: ");
            if (!int.TryParse(Console.ReadLine(), out int id))
            { Console.WriteLine("✗ Invalid ID."); return; }

            Person p = persons.FirstOrDefault(x => x.Id == id);
            if (p == null) { Console.WriteLine("✗ Person not found."); return; }

            Console.WriteLine($"\nEditing: {p.FirstName} {p.LastName} (press ENTER to keep current value)\n");

            while (true)
            {
                Console.Write($"First Name [{p.FirstName}]: ");
                string val = Console.ReadLine()?.Trim() ?? "";
                if (val == "") break;
                if (!ValidateName(val)) { Console.WriteLine("✗ Invalid name."); continue; }
                p.FirstName = val; break;
            }

            while (true)
            {
                Console.Write($"Last Name [{p.LastName}]: ");
                string val = Console.ReadLine()?.Trim() ?? "";
                if (val == "") break;
                if (!ValidateName(val)) { Console.WriteLine("✗ Invalid name."); continue; }
                p.LastName = val; break;
            }

            while (true)
            {
                Console.Write($"Phone [{p.Phone}]: ");
                string val = Console.ReadLine()?.Trim() ?? "";
                if (val == "") break;
                if (!ValidatePhone(val)) { Console.WriteLine("✗ Invalid phone."); continue; }
                p.Phone = val; break;
            }

            Console.Write($"City [{p.City}]: ");
            string city = Console.ReadLine()?.Trim() ?? "";
            if (!string.IsNullOrWhiteSpace(city)) p.City = city;

            while (true)
            {
                Console.Write($"Balance [{p.Balance:F2}]: ");
                string val = Console.ReadLine()?.Trim() ?? "";
                if (val == "") break;
                if (!decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal balance) || balance < 0)
                { Console.WriteLine("✗ Invalid balance."); continue; }
                p.Balance = balance; break;
            }

            SavePersons(persons);
            Console.WriteLine($"\n✓ Person ID={id} updated successfully.");
            WriteLog(activeUser, $"Edited person ID={id}: {p.FirstName} {p.LastName}.");
        }

        // ════════════════════════════════════════════════════════════════════════
        // DELETE PERSON
        // ════════════════════════════════════════════════════════════════════════

        private static void DeletePerson()
        {
            List<Person> persons = LoadPersons();
            Console.WriteLine("\n── DELETE PERSON ────────────────────────");
            Console.Write("Enter ID to delete: ");
            if (!int.TryParse(Console.ReadLine(), out int id))
            { Console.WriteLine("✗ Invalid ID."); return; }

            Person p = persons.FirstOrDefault(x => x.Id == id);
            if (p == null) { Console.WriteLine("✗ Person not found."); return; }

            Console.WriteLine($"\nPerson found:");
            Console.WriteLine($"  ID:      {p.Id}");
            Console.WriteLine($"  Name:    {p.FirstName} {p.LastName}");
            Console.WriteLine($"  Phone:   {p.Phone}");
            Console.WriteLine($"  City:    {p.City}");
            Console.WriteLine($"  Balance: {p.Balance:C}");
            Console.Write("\nAre you sure you want to delete? (Y/N): ");
            string confirm = Console.ReadLine()?.Trim().ToUpper() ?? "N";

            if (confirm == "Y")
            {
                persons.Remove(p);
                SavePersons(persons);
                Console.WriteLine($"\n✓ Person ID={id} deleted successfully.");
                WriteLog(activeUser, $"Deleted person ID={id}: {p.FirstName} {p.LastName}.");
            }
            else
            {
                Console.WriteLine("\nOperation cancelled.");
                WriteLog(activeUser, $"Cancelled deletion of person ID={id}.");
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // SAVE CHANGES
        // ════════════════════════════════════════════════════════════════════════

        private static void SaveChanges()
        {
            Console.WriteLine("\n✓ All changes have been saved.");
            WriteLog(activeUser, "Executed 'Save changes'.");
        }

        // ════════════════════════════════════════════════════════════════════════
        // REPORT BY CITY
        // ════════════════════════════════════════════════════════════════════════

        private static void ShowReport()
        {
            List<Person> persons = LoadPersons();
            Console.WriteLine("\n==========================================");
            Console.WriteLine("           REPORT BY CITY");
            Console.WriteLine("==========================================");

            if (persons.Count == 0)
            { Console.WriteLine("No data to display."); return; }

            var byCity = persons.GroupBy(p => p.City).OrderBy(g => g.Key);
            decimal grandTotal = 0;

            foreach (var group in byCity)
            {
                Console.WriteLine($"\nCity: {group.Key}");
                Console.WriteLine($"{"ID",-6}{"First Name",-16}{"Last Name",-16}{"Balance",14}");
                Console.WriteLine($"{"─",-6}{"─",-16}{"─",-16}{"─",14}");

                decimal subtotal = 0;
                foreach (var p in group.OrderBy(x => x.Id))
                {
                    Console.WriteLine($"{p.Id,-6}{p.FirstName,-16}{p.LastName,-16}{p.Balance,14:N2}");
                    subtotal += p.Balance;
                }

                Console.WriteLine($"{"",38}{"═",14}");
                Console.WriteLine($"Total {group.Key,-32}{subtotal,14:N2}");
                grandTotal += subtotal;
            }

            Console.WriteLine($"\n{"",38}{"═",14}");
            Console.WriteLine($"{"GRAND TOTAL:",-38}{grandTotal,14:N2}");
            Console.WriteLine("\n==========================================");
            WriteLog(activeUser, $"Viewed city report. Grand total: {grandTotal:N2}.");
        }

        // ════════════════════════════════════════════════════════════════════════
        // VALIDATIONS
        // ════════════════════════════════════════════════════════════════════════

        private static bool ValidateName(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Trim().Length < 2) return false;
            return Regex.IsMatch(value.Trim(), @"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s]+$");
        }

        static bool ValidatePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return false;
            string digits = Regex.Replace(phone, @"\s", "");
            return Regex.IsMatch(digits, @"^\d{10}$");
        }

        // ════════════════════════════════════════════════════════════════════════
        // PERSISTENCE — PERSONS
        // ════════════════════════════════════════════════════════════════════════

        private static List<Person> LoadPersons()
        {
            var list = new List<Person>();
            if (!File.Exists(FilePersons)) return list;

            foreach (string line in File.ReadAllLines(FilePersons))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] fields = line.Split('|');
                if (fields.Length < 6) continue;

                if (!int.TryParse(fields[0].Trim(), out int id)) continue;
                if (!decimal.TryParse(fields[5].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal balance)) balance = 0;

                list.Add(new Person
                {
                    Id = id,
                    FirstName = fields[1].Trim(),
                    LastName = fields[2].Trim(),
                    Phone = fields[3].Trim(),
                    City = fields[4].Trim(),
                    Balance = balance
                });
            }
            return list;
        }

        private static void SavePersons(List<Person> persons)
        {
            var lines = persons.Select(p =>
                $"{p.Id}|{p.FirstName}|{p.LastName}|{p.Phone}|{p.City}|{p.Balance.ToString("F2", CultureInfo.InvariantCulture)}");
            File.WriteAllLines(FilePersons, lines);
        }

        // ════════════════════════════════════════════════════════════════════════
        // PERSISTENCE — USERS
        // ════════════════════════════════════════════════════════════════════════

        private static List<User> LoadUsers()
        {
            var list = new List<User>();
            if (!File.Exists(FileUsers)) return list;

            foreach (string line in File.ReadAllLines(FileUsers))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] fields = line.Split(',');
                if (fields.Length < 3) continue;

                list.Add(new User
                {
                    UserName = fields[0].Trim(),
                    Password = fields[1].Trim(),
                    IsActive = fields[2].Trim().Equals("true", StringComparison.OrdinalIgnoreCase)
                });
            }
            return list;
        }

        private static void SaveUsers(List<User> users)
        {
            var lines = users.Select(u =>
                $"{u.UserName},{u.Password},{u.IsActive.ToString().ToLower()}");
            File.WriteAllLines(FileUsers, lines);
        }

        // ════════════════════════════════════════════════════════════════════════
        // LOG
        // ════════════════════════════════════════════════════════════════════════

        static void WriteLog(string user, string operation)
        {
            string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] User: {user} | {operation}";
            File.AppendAllText(FileLog, entry + Environment.NewLine);
        }
    }
}