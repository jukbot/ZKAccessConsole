using System.Diagnostics;
using System.Media;
using System.Runtime.InteropServices;
using PullSDK_core;
class Program
{
    // Import system libraries
    [DllImport("kernel32.dll")]
    public static extern bool Beep(int freq, int duration);

    // Allow door numbers for each group
    static Dictionary<int, int[]> allowedDoors = new Dictionary<int, int[]>
    {
        { 1, new[] { 1, 2 } },
        { 2, new[] { 1, 2, 3, 4 } },
        { 3, new[] { 1, 2, 3, 4, 5 } }
    };

    static void Main(string[] args)
    {
        string connectionStr = $"protocol=RS485,port=COM4,baudrate=38400bps,deviceid=1,timeout=50000,passwd={null}";
        Debug.WriteLine("Starting connection to device...");
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"Starting connection to {connectionStr}");
        AccessPanel device = new AccessPanel();
        // string parameters = $"protocol=TCP,ipaddress={ip},port={port},timeout={timeout},passwd={(key == 0 ? "" : key.ToString())}";

        bool deviceStatus = device.ConnectDevice(connectionStr);
        if (!deviceStatus)
        {
            Console.WriteLine("\a");
            Console.ForegroundColor = ConsoleColor.Red;
            PrintErrorCode(device, "Failed to connect to device");
            return;
        }

        SystemSounds.Beep.Play();
        PrintSuccess("Connected to device successfully.");
        Console.SetError(Console.Out);
        Console.BackgroundColor = ConsoleColor.Magenta;
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("Welcome to better ZkAccessConsole.\n");
        Console.ResetColor();
        Welcome(device);

        //while (true)
        //{
        //    // Read real-time logs
        //    var eventLog = device.GetEventLog();
        //    if (eventLog != null)
        //    {
        //        Console.WriteLine("Doors Status: " + eventLog.DoorsStatus);
        //        foreach (var e in eventLog.Events)
        //        {
        //            Console.WriteLine("Event: " + e);
        //        }
        //    }
        //    System.Threading.Thread.Sleep(1000); // Adjust the interval as needed
        //}
    }

    static void Welcome(AccessPanel device)
    {
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Please enter a command to continue:");
        Console.WriteLine("-----------------------------------------------------");
        Console.WriteLine("user - print all users\n" +
                        "event - read events\n" +
                        "open - open door\n" +
                        "close - close door\n" +
                        "add - add new user\n" +
                        "del - delete user\n" +
                        "log - read real-time logs\n" +
                        "clear - clear console\n" +
                        "reboot - reboot device\n" +
                        "quit - quit program");
        Console.WriteLine("-----------------------------------------------------");
        Console.ForegroundColor = ConsoleColor.White;
        string key = Console.ReadLine();

        if (!string.IsNullOrEmpty(key))
        {
            switch (key)
            {
                case "user":
                    PrintAllUsers(device);
                    break;
                case "open":
                    OpenDoor(device);
                    break;
                case "close":
                    CloseDoor(device);
                    break;
                case "reboot":
                    RebootDevice(device);
                    break;
                case "clear":
                    ResetConsole(device);
                    break;
                case "reset":
                    ResetConsole(device);
                    break;
                case "add":
                    AddUser(device);
                    break;
                case "del":
                    DeleteUser(device);
                    break;
                case "quit":
                    QuitProgram(device);
                    break;
                default:
                    Console.WriteLine("\a");
                    Console.Clear();
                    Console.WriteLine("Invalid command.");
                    Welcome(device);
                    break;
            }
        }

    }

    static void ResetConsole(AccessPanel device)
    {
        Console.Clear();
        Console.ResetColor();
        Welcome(device);
    }

    static void RebootDevice(AccessPanel device)
    {
        Console.WriteLine("Rebooting device...");
        bool result = device.Reboot();
        if (result)
        {
            PrintSuccess("Device rebooted successfully.");
            Beep(500, 500);
        }
        else
        {
            PrintErrorCode(device, "Failed to reboot device.");
        }
        FinishProgram(device);
    }

    static void PrintAllUsers(AccessPanel device)
    {
        PrintInProgress("Reading users...");
        List<PullSDK_core.User> users = device.ReadUsers();
        if (users == null)
        {
            PrintErrorCode(device, "Failed to read users");
            return;
        }

        foreach (var user in users)
        {
            Console.WriteLine(user.ToString());
        }

        FinishProgram(device);
    }

    static void OpenDoor(AccessPanel device)
    {
        Console.WriteLine("Enter the door number to open (1-5):");
        string input = Console.ReadLine();
        int doorNumber;

        if (int.TryParse(input, out doorNumber) && doorNumber >= 1 && doorNumber <= 5)
        {
            device.OpenDoor(doorNumber, 5); // Open the specified door for 5 seconds
            PrintSuccess($"Door {doorNumber} opened.");
            FinishProgram(device);
        }
        else
        {
            Console.WriteLine("Invalid door number. Please enter a number between 1 and 5.");
            OpenDoor(device);
        }
    }

    static void CloseDoor(AccessPanel device)
    {
        Console.WriteLine("Enter the door number to close (1-5):");
        string input = Console.ReadLine();
        int doorNumber;

        if (int.TryParse(input, out doorNumber) && doorNumber >= 1 && doorNumber <= 5)
        {
            device.CloseDoor(doorNumber);
            PrintSuccess($"Door {doorNumber} closed.");
            FinishProgram(device);
        }
        else
        {
            Console.WriteLine("Invalid door number. Please enter a number between 1 and 5.");
            CloseDoor(device);
        }
    }

    static void AddUser(AccessPanel device)
    {
        Console.WriteLine("Enter the user's name (max 30 characters):");
        string name = Console.ReadLine();
        if (name.Length > 30)
        {
            Console.WriteLine("Name is too long. Please enter a name with max 30 characters.");
            return;
        }

        Console.WriteLine("Enter the group number (1 for member, 2 for staff, 3 for admin):");
        string groupInput = Console.ReadLine();
        int groupNumber;

        if (!int.TryParse(groupInput, out groupNumber) || groupNumber < 1 || groupNumber > 3)
        {
            Console.WriteLine("Invalid group number. Please enter a number between 1 and 3.");
            return;
        }

        Console.WriteLine("Enter the pin number (max 10 digits):");
        string pinInput = Console.ReadLine();
        int pinNumber;
        if (!int.TryParse(pinInput, out pinNumber) || pinInput.Length > 10)
        {
            Console.WriteLine("Invalid pin number. Please enter a number with max 10 digits.");
            return;
        }

        Console.WriteLine("Enter the card number:");
        string cardNumber = Console.ReadLine();

        Console.WriteLine("The following user will be add to access control");
        Console.WriteLine("---------------------------------------------------");
        Console.WriteLine($"Name: {name}");
        Console.WriteLine($"Group ID: {groupNumber}");
        Console.WriteLine($"Pin: {pinNumber}");
        Console.WriteLine($"Card Number: {cardNumber}");
        Console.WriteLine($"Allowed Doors: {string.Join(", ", allowedDoors[groupNumber])}");
        Console.WriteLine("---------------------------------------------------\n");
        Console.WriteLine("1 - continue add user\n2 - reset and start over\n3 - cancel");
        string confirmInput = Console.ReadLine();
        switch (confirmInput)
        {
            case "1":
                string today = DateTime.Today.ToString("yyyyMMdd");
                string endDate = DateTime.Today.AddYears(20).ToString("yyyyMMdd");
                // Add user
                PullSDK_core.User u = new PullSDK_core.User(pinNumber.ToString(), name, cardNumber, null, today, endDate, allowedDoors[groupNumber]);
                Console.WriteLine(u.ToString());

                if (device.WriteUser(u))
                {
                    PrintSuccess("User added successfully.");
                    FinishProgram(device);
                }
                else
                {
                    PrintErrorCode(device, "Failed to add user.");
                }
                break;
            case "2":
                Console.Clear();
                AddUser(device);
                break;
            case "3":
                ResetConsole(device);
                Welcome(device);
                break;
            default:
                PrintError("Invalid command.");
                break;
        }
    }

    static void DeleteUser(AccessPanel device)
    {
        Console.WriteLine("1 - delete user by card no\n2 - to delete everythings\n3 - cancel");
        string confirmInput = Console.ReadLine();
        switch (confirmInput)
        {
            case "1":
                Console.WriteLine("Enter the user's card number to delete:");
                string cardNumber = Console.ReadLine();
                if (string.IsNullOrEmpty(cardNumber))
                {
                    PrintError("Invalid card number.");
                    return;
                }
                device.DeleteUserByCard(cardNumber);
                PrintSuccess("User deleted successfully.");
                FinishProgram(device);
                break;
            case "2":
                device.DeleteAllDataFromDevice();
                PrintSuccess("All users deleted successfully.");
                FinishProgram(device);
                break;
            case "3":
                ResetConsole(device);
                Welcome(device);
                break;
            default:
                PrintError("Invalid command.");
                break;
        }
    }

    static void QuitProgram(AccessPanel device)
    {
        device.Disconnect();
        Console.WriteLine("Disconnected from device.");
        Console.WriteLine("Quitting program...");
        Environment.Exit(0);
        return;
    }

    static void PrintErrorCode(AccessPanel device, string msg)
    {
        int error = device.GetLastError();
        Console.WriteLine("\a");
        PrintError($"{msg}, Error code: " + error);
        PrintError(device.GetLastDataErrorTable());
    }

    static void PrintInProgress(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"{msg}\n");
        Console.ResetColor();
    }

    static void PrintSuccess(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"{msg}\n");
        Console.ResetColor();
    }

    static void PrintError(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"{msg}\n");
        Console.ResetColor();
    }

    static void FinishProgram(AccessPanel device)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\nPress any key to continue...\n\n");
        Console.ResetColor();
        Console.ReadKey();
        ResetConsole(device);
    }

    static bool IsAuthorized(string cardNo)
    {
        // Implement your authorization logic here
        // For example, check against a list of authorized card numbers
        return cardNo == "authorized_card_number"; // Replace with actual logic
    }
}

