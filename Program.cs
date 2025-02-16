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
        ScanNetwork(startIp, endIp);

    }

    static void ScanNetwork(IPAddress startIp, IPAddress endIp)
    {
        long start = BitConverter.ToInt32(startIp.GetAddressBytes().Reverse().ToArray(), 0);
        long end = BitConverter.ToInt32(endIp.GetAddressBytes().Reverse().ToArray(), 0);

        for (long i = start; i <= end; i++)
        {
            var ipBytes = BitConverter.GetBytes(i).Reverse().ToArray();
            var ip = new IPAddress(ipBytes);

            if (PingHost(ip.ToString()))
            {
                Console.WriteLine($"[+] Active IP Found: {ip}");
            }
        }
    }

    static bool PingHost(string ip)
    {
        try
        {
            Ping ping = new Ping();
            PingReply reply = ping.Send(ip, 1000); // 1000ms timeout
            return reply.Status == IPStatus.Success;
        }
        catch
        {
            return false;
        }
    }




}