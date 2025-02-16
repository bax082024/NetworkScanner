using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

class Program
{
    static List<string> sessionData = new List<string>();


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

        int coreCount = Environment.ProcessorCount;
        Console.WriteLine($"\nYour system has {coreCount} CPU cores.");
        int parallelTasks = GetParallelTasks(coreCount);

        Console.Write("Enter the IP range to scan (e.g., 192.168.1.1-192.168.1.255): ");
        string input = Console.ReadLine() ?? string.Empty;

        var parts = input.Split('-');
        if (parts.Length != 2 || !IPAddress.TryParse(parts[0], out IPAddress startIp) || !IPAddress.TryParse(parts[1], out IPAddress endIp))
        {
            Console.WriteLine("Invalid input. Please enter a valid range.");
            return;
        }

        Console.Write("Enter the number of samples to estimate network speed (e.g., 5): ");
        int sampleCount = int.Parse(Console.ReadLine() ?? "5");

        int timeout = CalculateOptimalTimeout(startIp, endIp, sampleCount);

        Console.WriteLine("\nScanning the network...");
        List<string> activeHosts = ScanNetwork(startIp, endIp, timeout, parallelTasks);

        ShowMenu(activeHosts, timeout, parallelTasks, startIp, endIp);

    }


    static void ShowMenu(List<string> activeHosts, int timeout, int parallelTasks, IPAddress startIp, IPAddress endIp)

    {
        while (true)
        {
            Console.WriteLine("\n================ Menu ================");
            Console.WriteLine("Choose an option:");
            Console.WriteLine("1. Perform Network Scan");
            Console.WriteLine("2. Perform Port Scan");
            Console.WriteLine("3. Save Report to File");
            Console.WriteLine("4. Exit");

            string choice = Console.ReadLine() ?? string.Empty;

            switch (choice)
            {
                case "1":
                    activeHosts = ScanNetwork(startIp, endIp, timeout, parallelTasks);
                    break;

                case "2":
                    if (activeHosts.Count == 0)
                    {
                        Console.WriteLine("\nNo active hosts found. Please perform a network scan first.");
                    }
                    else
                    {
                        PerformPortScan(activeHosts, timeout, parallelTasks);
                    }
                    break;

                case "3":
                    if (sessionData.Count == 0)
                    {
                        Console.WriteLine("\nNo data to save. Please perform a scan first.");
                    }
                    else
                    {
                        SaveToFile(sessionData); 
                    }
                    break;

                case "4":
                    Console.WriteLine("Exiting program...");
                    return;

                default:
                    Console.WriteLine("Invalid choice. Please select 1, 2, 3, or 4.");
                    break;
            }
        }
    }

    static void PerformPortScan(List<string> activeHosts, int timeout, int parallelTasks)
    {
        Console.Write("\nEnter the port range to scan (e.g., 20-1024): ");
        string rangeInput = Console.ReadLine() ?? string.Empty;
        var parts = rangeInput.Split('-');

        if (parts.Length != 2
            || !int.TryParse(parts[0], out int startPort)
            || !int.TryParse(parts[1], out int endPort)
            || startPort < 1 || endPort > 65535
            || startPort > endPort)
        {
            Console.WriteLine("Invalid port range. Please enter a valid range (1-65535) with start port <= end port.");
            return;
        }

        Console.WriteLine("\nStarting port scan...");

        List<string> portScanResults = new List<string>();

        Parallel.ForEach(activeHosts, new ParallelOptions { MaxDegreeOfParallelism = parallelTasks }, host =>
        {
            Console.WriteLine($"\nScanning ports on {host}...");
            for (int port = startPort; port <= endPort; port++)
            {
                using (TcpClient tcpClient = new TcpClient())
                {
                    try
                    {
                        tcpClient.ConnectAsync(host, port).Wait(timeout);
                        if (tcpClient.Connected)
                        {
                            string result = $"[OPEN] {host}:{port}";
                            Console.WriteLine(result);
                            lock (portScanResults)
                            {
                                portScanResults.Add(result);
                            }
                            tcpClient.Close();
                        }
                    }
                    catch
                    {
                        // Port closed
                    }
                }
            }
        });


        Console.WriteLine("\nPort scan completed.");
        activeHosts.AddRange(portScanResults);
    }

    static void SaveToFile(List<string> sessionData)
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"NetworkScanReport_{timestamp}.txt";

        try
        {
            File.WriteAllLines(fileName, sessionData);
            Console.WriteLine($"\nReport saved successfully as {fileName} in the current directory.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nFailed to save the report: {ex.Message}");
        }
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


    public static List<string> ScanNetwork(IPAddress startIp, IPAddress endIp, int timeout, int parallelTasks)
    {
        List<string> activeHosts = new List<string>();
        uint start = BitConverter.ToUInt32(startIp.GetAddressBytes().Reverse().ToArray(), 0);
        uint end = BitConverter.ToUInt32(endIp.GetAddressBytes().Reverse().ToArray(), 0);

        Console.WriteLine("Starting parallel scan...");

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

        sessionData.AddRange(activeHosts);

        

        return activeHosts;
    }


    static int GetParallelTasks(int coreCount)
    {
        Console.Write($"\nEnter the number of parallel tasks to use (recommended: {coreCount * 2}): ");
        string? parallelInput = Console.ReadLine();
        if (string.IsNullOrEmpty(parallelInput) || !int.TryParse(parallelInput, out int parallelTasks) || parallelTasks <= 0)
        {
            Console.WriteLine("Invalid input. Using default value.");
            return coreCount * 2;
        }
        return parallelTasks;
    }

}
