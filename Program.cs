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
        { 2, new[] { 1, 2, 3 } },
        { 3, new[] { 1, 2, 3, 4 } }
    };

    static bool isRunning = true;
    static string connectionStr = $"protocol=RS485,port=COM4,baudrate=38400bps,deviceid=1,timeout=5000,passwd={null}";
    // static string connectionStr = $"protocol=TCP,port=4370,ipaddress=192.168.1.201,timeout=5000,passwd={null}";
    static void Main(string[] args)
    {
        PrintInProgress($"Start connecting to {connectionStr}");
        AccessPanel device = new AccessPanel();

        bool deviceStatus = device.ConnectDevice(connectionStr);
        Console.Clear();
        if (!deviceStatus)
        {
            Console.WriteLine("\a");
            Console.ForegroundColor = ConsoleColor.Red;
            PrintErrorCode(device, "Failed to connect to device");
        }

        PrintSuccess("Connected to device successfully.");
        SystemSounds.Exclamation.Play();

        CheckIsConnected(device);

        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"Connected: {device.IsConnected()}");
        Console.WriteLine($"Type: {device.GetType().ToString()}");
        Console.WriteLine($"Firmware version: {device.DetectedFirmwareVersion}");
        Console.WriteLine($"Connected doors: {device.GetDoorCount()}");
        Console.WriteLine($"Serial: {device.GetSerialNumber()}");
        Console.WriteLine("-----------------------------------------------------\n");
        SetTimezone(device);
        Welcome(device);

        Console.CancelKeyPress += delegate (object? sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true; // prevent the program from exiting immediately
            Program.isRunning = false;
        };
    }

    static void CheckIsConnected(AccessPanel device)
    {
        if (!device.IsConnected() || device.GetDoorCount() == -1)
        {
            PrintErrorCode(device, "Device is not connect");
            return;
        }
    }
    static void Welcome(AccessPanel device)
    {
        CheckIsConnected(device);
        Console.ResetColor();
        Console.BackgroundColor = ConsoleColor.Magenta;
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("Welcome to better ZkAccess Console v1.0");
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\nPlease enter a command to continue:");
        Console.WriteLine("-----------------------------------------------------");
        Console.WriteLine("user - get all users\n" +
                        "open - open door\n" +
                        "close - close door\n" +
                        "add - add new user\n" +
                        "del - delete user\n" +
                        "door - get user doors\n" +
                        "set - set user access\n" +
                        "event - read event log\n" +
                        "log - read realtime logs\n" +
                        "reboot - reboot device\n" +
                        "clear - clear console\n" +
                        "quit - quit program");
        Console.WriteLine("-----------------------------------------------------");
        Console.ForegroundColor = ConsoleColor.White;
        string key = Console.ReadLine();

        if (!string.IsNullOrEmpty(key))
        {
            switch (key)
            {
                case "user":
                    PrintUsers(device);
                    break;
                case "open":
                    OpenDoor(device);
                    break;
                case "close":
                    CloseDoor(device);
                    break;
                case "add":
                    AddUser(device);
                    break;
                case "del":
                    DeleteUser(device);
                    break;
                case "set":
                    SetUserAccess(device);
                    break;
                case "door":
                    PrintUserDoor(device);
                    break;
                case "event":
                    PrintEventLog(device);
                    break;
                case "log":
                    PrintRealtimeLog(device);
                    break;
                case "reboot":
                    RebootDevice(device);
                    break;
                case "clear":
                    ResetConsole(device);
                    break;
                case "exit":
                    QuitProgram(device);
                    break;
                case "quit":
                    QuitProgram(device);
                    break;
                default:
                    Console.WriteLine("\a");
                    Console.Clear();
                    PrintError("Invalid command.");
                    Welcome(device);
                    break;
            }
        }

    }

    static void SetTimezone(AccessPanel device)
    {
        int[] defaultTZ = new int[] {
            2359, 0, 0, // Friday is the first day because i said so to spite you
            2359, 0, 0,
            2359, 0, 0,
            2359, 0, 0,
            2359, 0, 0,
            2359, 0, 0,
            2359, 0, 0
        };
        if (!device.WriteTimezone(1, defaultTZ))
        {
            PrintErrorCode(device, "Failed to set timezone");
            return; // Why won't you work you P.O.S?
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
        PrintInProgress("Rebooting device...");
        device.Reboot();
        PrintSuccess("Device rebooted successfully.");
        ReconnectDevice(device);
    }

    static void ReconnectDevice(AccessPanel device)
    {
        PrintInProgress($"Starting connection to {connectionStr}");
        device.ConnectDevice(connectionStr);
        PrintSuccess("Connected to device successfully.");
        Beep(500, 500);
        ResetConsole(device);
    }

    static void PrintUserDoor(AccessPanel device)
    {
        Console.Clear();
        Console.WriteLine("Enter user pin to get allowed doors:");
        int pinNumber;
        string pinInput = Console.ReadLine();
        if (!int.TryParse(pinInput, out pinNumber) || pinInput.Length > 10 || string.IsNullOrWhiteSpace(pinInput))
        {
            Console.Clear();
            PrintError("Invalid pin number. Please input a number with max 10 digits.");
            SetUserAccess(device);
        }
        else if (pinNumber < 0)
        {
            Console.Clear();
            PrintError("Pin number cannot be negative. Please enter a number with max 10 digits.");
            SetUserAccess(device);
        }
        // -----------------------------------------------------------------------------------------
        PrintInProgress("Reading user doors...");
        int doors = device.ReadDoors(pinInput, 1);

        if (doors < 0)
        {
            PrintErrorCode(device, "Cannot get user doors");
        }

        Console.WriteLine($"User pin {pinInput} has access to doors: {doors}");
        FinishProgram(device);
    }

    static void PrintEventLog(AccessPanel device)
    {
        PrintInProgress("Reading events...");
        var events = device.GetEventLog();
        if (events == null)
        {
            PrintErrorCode(device, "Failed to read events");
            FinishProgram(device);
        }

        if (events.Events.Length == 0)
        {
            Console.WriteLine("No events found.");
            FinishProgram(device);
        }
        else
        {
            Console.WriteLine($"Last event: {events.Events[events.Events.Length - 1]}");
            FinishProgram(device);
        }
    }

    static void PrintRealtimeLog(AccessPanel device)
    {
        Console.Clear();
        Program.isRunning = true;
        PrintInProgress("Reading real-time logs... (press 'q' to exit)");
        while (Program.isRunning)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Q)
                {
                    Program.isRunning = false;
                    ResetConsole(device);
                    return;
                }
            }

            var e = device.GetEventLog();
            if (e == null)
            {
                PrintErrorCode(device, "Failed to read real-time logs");
                FinishProgram(device);
                return;
            }

            foreach (var eventLog in e.Events)
            {
                if (eventLog.EventType == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"User card no {eventLog.Card} with pin {eventLog.Pin} accessed door no {eventLog.Door} at {eventLog.Time}");
                    Console.WriteLine($"Event type: {eventLog.EventType} - {eventLog.ToString()}\n");
                }
                else if (eventLog.EventType == 27)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine($"User card no {eventLog.Card} with pin {eventLog.Pin} try to access door no {eventLog.Door} at {eventLog.Time}");
                    Console.WriteLine($"Event type: {eventLog.EventType} - {eventLog.ToString()}\n");
                }
                else if (eventLog.EventType == 8)
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine($"Event type: {eventLog.EventType} - {eventLog.ToString()}\n");
                }
                else if (eventLog.EventType != 20)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine($"Event type: {eventLog.EventType} - {eventLog.ToString()}\n");
                }
            }

            System.Threading.Thread.Sleep(1000);  // Wait for a short time before getting the next log
        }
    }

    static void PrintUsers(AccessPanel device)
    {
        PrintInProgress("Reading users...");
        List<PullSDK_core.User> users = device.ReadUsers();
        if (users == null)
        {
            PrintErrorCode(device, "Failed to read users");
            FinishProgram(device);
            return;
        }

        if (users.Count == 0)
        {
            Console.WriteLine("No users found.");
        }

        foreach (var user in users)
        {
            Console.WriteLine(user.ToString());
        }

        FinishProgram(device);
    }

    static void OpenDoor(AccessPanel device)
    {
        Console.WriteLine("Enter the door number to open (1-4):");
        string input = Console.ReadLine();
        int doorNumber;

        if (int.TryParse(input, out doorNumber) && doorNumber >= 1 && doorNumber <= 4)
        {
            PrintInProgress($"Opening door {doorNumber}...");
            device.OpenDoor(doorNumber, 5); // Open the specified door for 5 seconds
            PrintSuccess($"Door {doorNumber} opened.");
            FinishProgram(device);
        }
        else
        {
            PrintError("Invalid door number. Please enter a number between 1 and 4.");
            FinishProgram(device);
        }
    }

    static void CloseDoor(AccessPanel device)
    {
        Console.WriteLine("Enter the door number to close (1-4):");
        string input = Console.ReadLine();
        int doorNumber;

        if (int.TryParse(input, out doorNumber) && doorNumber >= 1 && doorNumber <= 4)
        {
            PrintInProgress($"Closing door {doorNumber}...");
            device.CloseDoor(doorNumber);
            PrintSuccess($"Door {doorNumber} closed.");
            FinishProgram(device);
        }
        else
        {
            PrintError("Invalid door number. Please enter a number between 1 and 4.");
            CloseDoor(device);
        }
    }

    static void AddUser(AccessPanel device)
    {
        // -----------------------------------------------------------------------------------------
        Console.Clear();
        string name;
        do
        {
            Console.WriteLine("Enter user's name (max 30 characters):");
            name = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(name) || name.Length > 30)
            {
                Console.Clear();
                PrintError("Invalid input. Please enter a name with max 30 characters and it must not be empty.");
            }
        } while (string.IsNullOrWhiteSpace(name) || name.Length > 30);

        // -----------------------------------------------------------------------------------------
        Console.Clear();
        Console.WriteLine("Enter a group number:\n1 - member\n2 - staff\n3 - admin\nLeave blank to set same as admin");
        string groupInput = Console.ReadLine();
        int groupNumber;

        if (string.IsNullOrWhiteSpace(groupInput))
        {
            groupNumber = 3;
        }
        else if (!int.TryParse(groupInput, out groupNumber) || groupNumber < 1 || groupNumber > 3)
        {
            Console.Clear();
            PrintError("Invalid group number. Please enter a number between 1 and 3.");
        }

        // -----------------------------------------------------------------------------------------
        Console.Clear();
        int pinNumber;
        string pinInput;
        do
        {
            Console.WriteLine("Enter a pin number (max 10 digits):");
            pinInput = Console.ReadLine();
            if (!int.TryParse(pinInput, out pinNumber) || pinInput.Length > 10 || string.IsNullOrWhiteSpace(pinInput))
            {
                Console.Clear();
                PrintError("Invalid pin number. Please input a number with max 10 digits.");
            }
            else if (pinNumber < 0)
            {
                Console.Clear();
                PrintError("Pin number cannot be negative. Please enter a number with max 10 digits.");
            }
        } while (!int.TryParse(pinInput, out pinNumber) || pinInput.Length > 10 || string.IsNullOrWhiteSpace(pinInput) || pinNumber < 0);

        // -----------------------------------------------------------------------------------------
        Console.Clear();
        string cardNumber;
        do
        {
            Console.WriteLine("Enter a card number (Support only alphanumeric):");
            cardNumber = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(cardNumber) || cardNumber.Length > 30 || !IsAlphanumeric(cardNumber))
            {
                Console.Clear();
                PrintError("Invalid input. Please enter a card number with max 30 alphanumeric characters and it must not be empty.");
            }
        } while (string.IsNullOrWhiteSpace(cardNumber) || cardNumber.Length > 30 || !IsAlphanumeric(cardNumber));

        // -----------------------------------------------------------------------------------------
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("The following user data will be add to access control");
        Console.WriteLine("---------------------------------------------------");
        Console.WriteLine($"Name: {name}");
        Console.WriteLine($"Group ID: {groupNumber}");
        Console.WriteLine($"Pin: {pinNumber}");
        Console.WriteLine($"Card Number: {cardNumber}");
        Console.WriteLine($"Allowed Doors: {string.Join(", ", allowedDoors[groupNumber])}");
        Console.WriteLine("---------------------------------------------------");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Are you sure that it is correct?");
        Console.WriteLine("1 - confirm\n2 - start over\n3 - cancel");
        Console.ResetColor();
        string confirmInput = Console.ReadLine();
        switch (confirmInput)
        {
            case "1":
                string today = DateTime.Today.ToString("yyyyMMdd");
                string endDate = DateTime.Today.AddYears(20).ToString("yyyyMMdd");
                PullSDK_core.User u = new PullSDK_core.User(pinNumber.ToString(), name, cardNumber, null, today, endDate, allowedDoors[groupNumber]);
                PrintInProgress("Adding user...");
                if (device.WriteUser(u))
                {
                    Console.Clear();
                    Console.WriteLine(u.ToString());
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
        Console.Clear();
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
                    DeleteUser(device);
                }
                PrintInProgress("Deleting user...");
                device.DeleteUserByCard(cardNumber);
                PrintSuccess("User deleted successfully.");
                FinishProgram(device);
                break;
            case "2":
                PrintInProgress("Deleting all users...");
                device.DeleteAllDataFromDevice();
                PrintSuccess("All users deleted successfully.");
                FinishProgram(device);
                break;
            case "3":
                ResetConsole(device);
                break;
            default:
                PrintError("Invalid command.");
                DeleteUser(device);
                break;
        }
    }

    static void SetUserAccess(AccessPanel device)
    {
        Console.Clear();
        Console.WriteLine("Enter user pin to set access:");
        int pinNumber;
        string pinInput = Console.ReadLine();
        if (!int.TryParse(pinInput, out pinNumber) || pinInput.Length > 10 || string.IsNullOrWhiteSpace(pinInput))
        {
            Console.Clear();
            PrintError("Invalid pin number. Please input a number with max 10 digits.");
            SetUserAccess(device);
        }
        else if (pinNumber < 0)
        {
            Console.Clear();
            PrintError("Pin number cannot be negative. Please enter a number with max 10 digits.");
            SetUserAccess(device);
        }
        // -----------------------------------------------------------------------------------------
        Console.Clear();
        int groupNumber;
        do
        {
            Console.WriteLine("Enter a group number:\n1 - member\n2 - staff\n3 - admin\nLeave blank to set same as admin");
            string groupInput = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(groupInput))
            {
                groupNumber = 3;
                break;
            }
            else if (!int.TryParse(groupInput, out groupNumber) || groupNumber < 1 || groupNumber > 3)
            {
                Console.Clear();
                PrintError("Invalid group number. Please enter a number between 1 and 3.");
            }
            else
            {
                break;
            }
        } while (true);

        // -----------------------------------------------------------------------------------------
        PrintInProgress("Setting door access...");
        device.SetUserDoors(pinInput, 1, allowedDoors[groupNumber]);
        PrintSuccess("User access set successfully.");
        FinishProgram(device);
    }

    static void QuitProgram(AccessPanel device)
    {
        PrintInProgress("Disconnecting from device...");
        device.Disconnect();
        PrintInProgress("Goodbye!");
        Environment.Exit(0);
    }

    static void PrintErrorCode(AccessPanel device, string msg)
    {
        int error = device.GetLastError();
        Console.WriteLine("\a");
        PrintError($"{msg}, Error code: " + error.ToString());
        PrintError(device.GetLastDataErrorTable());
    }

    static void PrintInProgress(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"{msg}");
        Console.ResetColor();
    }

    static void PrintSuccess(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"{msg}");
        Console.ResetColor();
    }

    static void PrintError(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"{msg}");
        Console.ResetColor();
    }

    static void FinishProgram(AccessPanel device)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("------------------------------------------\nPress any key to continue...");
        Console.ResetColor();
        Console.ReadKey();
        ResetConsole(device);
    }

    private static bool IsAlphanumeric(string str)
    {
        for (int i = 0; i < str.Length; i++)
        {
            if (!char.IsLetterOrDigit(str[i]))
            {
                return false;
            }
        }
        return true;
    }
}

