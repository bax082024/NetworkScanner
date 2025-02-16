using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

class Program
{
    public static IEnumerable<IPAddress> GenerateIPRange(IPAddress startIp, IPAddress endIp)
    {
        uint start = BitConverter.ToUInt32(startIp.GetAddressBytes().Reverse().ToArray(), 0);
        uint end = BitConverter.ToUInt32(endIp.GetAddressBytes().Reverse().ToArray(), 0);

        for (uint ip = start; ip <= end; ip++)
        {
            byte[] addressBytes = BitConverter.GetBytes(ip).Reverse().ToArray();
            yield return new IPAddress(addressBytes);
        }
    }


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

        Console.Write("Enter the number of samples to estimate network speed (e.g., 5): ");
        int sampleCount = int.Parse(Console.ReadLine() ?? "5");

        int timeout = CalculateOptimalTimeout(startIp, endIp, sampleCount);

        Console.WriteLine("\nScanning the network...");
        ScanNetwork(startIp, endIp, timeout, parallelTasks);  // Pass parallelTasks to the method
    }

    public static int CalculateOptimalTimeout(IPAddress startIp, IPAddress endIp, int sampleCount)
    {
        List<long> responseTimes = new List<long>();
        var ipAddresses = GenerateIPRange(startIp, endIp).Take(sampleCount).ToList();

        Console.WriteLine("Sampling network response times...");

        foreach (var ip in ipAddresses)
        {
            using (Ping ping = new Ping())
            {
                try
                {
                    var start = DateTime.Now;
                    PingReply reply = ping.Send(ip, 1000);
                    var end = DateTime.Now;

                    if (reply.Status == IPStatus.Success)
                    {
                        long timeTaken = (end - start).Milliseconds;
                        responseTimes.Add(timeTaken);
                        Console.WriteLine($"Ping {ip}: {timeTaken}ms");
                    }
                    else
                    {
                        Console.WriteLine($"No response from {ip}");
                    }
                }
                catch
                {
                    Console.WriteLine($"Error pinging {ip}");
                }
            }
        }

        if (responseTimes.Count == 0)
        {
            Console.WriteLine("No successful pings during sampling. Using default timeout (1000ms).");
            return 1000;
        }

        double average = responseTimes.Average();
        int optimalTimeout = Math.Min(1000, Math.Max(100, (int)average + 50));

        Console.WriteLine($"\nCalculated optimal timeout: {optimalTimeout}ms\n");
        return optimalTimeout;
    }


    public static void ScanNetwork(IPAddress startIp, IPAddress endIp, int timeout, int parallelTasks)


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
                    PingReply reply = ping.Send(currentIp, timeout);
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
