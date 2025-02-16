using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

class Program
{
    static void Main()
    {
        Console.WriteLine("================================");
        Console.WriteLine(" Welcome to the Network Scanner ");
        Console.WriteLine("================================\n");

        // Added: Get CPU cores and set parallel tasks
        int coreCount = Environment.ProcessorCount;
        Console.WriteLine($"\nYour system has {coreCount} CPU cores.");
        Console.Write($"\nEnter the number of parallel tasks to use (recommended: {coreCount * 2}): ");
        string? parallelInput = Console.ReadLine();
        int parallelTasks;

        if (string.IsNullOrEmpty(parallelInput))
        {
            parallelTasks = coreCount * 2;
        }
        else if (!int.TryParse(parallelInput, out parallelTasks) || parallelTasks <= 0)
        {
            Console.WriteLine("Invalid input. Using default value.");
            parallelTasks = coreCount * 2;
        }

        Console.Write("Enter the IP range to scan (e.g., 192.168.1.1-192.168.1.255): ");
        string input = Console.ReadLine() ?? string.Empty;

        var parts = input.Split('-');
        if (parts.Length != 2)
        {
            Console.WriteLine("Invalid input. Please enter a valid range.");
            return;
        }

        IPAddress startIp, endIp;
        if (!IPAddress.TryParse(parts[0], out startIp) || !IPAddress.TryParse(parts[1], out endIp))
        {
            Console.WriteLine("Invalid IP address. Please try again.");
            return;
        }

        Console.WriteLine("\nScanning the network...");
        ScanNetwork(startIp, endIp, parallelTasks);  // Pass parallelTasks to the method
    }

    public static void ScanNetwork(IPAddress startIp, IPAddress endIp, int parallelTasks)
    {
        List<string> activeHosts = new List<string>();
        uint start = BitConverter.ToUInt32(startIp.GetAddressBytes().Reverse().ToArray(), 0);
        uint end = BitConverter.ToUInt32(endIp.GetAddressBytes().Reverse().ToArray(), 0);

        Console.WriteLine("Starting parallel scan...");

        // Added: ParallelOptions to limit parallel tasks
        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = parallelTasks };

        Parallel.For((long)start, (long)end + 1, parallelOptions, ip =>
        {
            byte[] addressBytes = BitConverter.GetBytes((uint)ip).Reverse().ToArray();
            IPAddress currentIp = new IPAddress(addressBytes);

            Console.WriteLine($"Scanning: {currentIp}");

            using (Ping ping = new Ping())
            {
                try
                {
                    PingReply reply = ping.Send(currentIp, 1000);
                    if (reply.Status == IPStatus.Success)
                    {
                        lock (activeHosts)
                        {
                            Console.WriteLine($"Host found: {currentIp}");
                            activeHosts.Add(currentIp.ToString());
                        }
                    }
                }
                catch (PingException ex)
                {
                    Console.WriteLine($"Error scanning {currentIp}: {ex.Message}");
                }
            }
        });

        Console.WriteLine("Finished scanning all IPs...");
        Thread.Sleep(2000);
        Console.Clear();
        Console.WriteLine("=== Network Scan Complete ===");

        if (activeHosts.Count > 0)
        {
            Console.WriteLine("Active Hosts:");
            foreach (var host in activeHosts)
            {
                Console.WriteLine(host);
            }
        }
        else
        {
            Console.WriteLine("No active hosts found.");
        }

        Console.WriteLine("\nPress Enter to exit...");
        Console.ReadLine();
    }
}
