using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static bool quiet = false;
    static double timeout = 1000; // one second timeout

    static List<string> GetIPs(string input)
    {
        var ips = new List<string>();
        if (Regex.IsMatch(input, @"^\d{1,3}(\.\d{1,3}){3}$"))
        {
            ips.Add(input);
        }
        else if (Regex.IsMatch(input, @"^(\d{1,3}(\.\d{1,3}){3})-(\d{1,3}(\.\d{1,3}){3})$"))
        {
            var parts = input.Split('-');
            var start = IPAddress.Parse(parts[0]).GetAddressBytes();
            var end = IPAddress.Parse(parts[1]).GetAddressBytes();
            for (uint i = ToUInt(start); i <= ToUInt(end); i++)
            {
                ips.Add(new IPAddress(BitConverter.GetBytes(i).Reverse().ToArray()).ToString());
            }
        }
        else if (Regex.IsMatch(input, @"^\d{1,3}(\.\d{1,3}){3}/\d{1,2}$"))
        {
            var parts = input.Split('/');
            var ip = IPAddress.Parse(parts[0]).GetAddressBytes();
            int cidr = int.Parse(parts[1]);
            uint mask = cidr == 0 ? 0 : 0xffffffff << (32 - cidr);
            uint ipVal = ToUInt(ip);
            uint network = ipVal & mask;
            uint broadcast = network + (uint)(Math.Pow(2, 32 - cidr) - 1);
            for (uint i = network + 1; i < broadcast; i++)
            {
                ips.Add(new IPAddress(BitConverter.GetBytes(i).Reverse().ToArray()).ToString());
            }
        }
        else
        {
            Console.WriteLine("Invalid format. Please provide a valid IP, IP range or CIDR subnet.");
            Environment.Exit(1);
        }
        return ips;
    }

    static uint ToUInt(byte[] bytes)
    {
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        return BitConverter.ToUInt32(bytes, 0);
    }

    static async Task FetchAndSaveAsync(string ip, int port)
    {


        var protocols = new[] { "http", "https" };
        foreach (var proto in protocols)
        {
            var url = $"{proto}://{ip}:{port}/";
            using (var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (m, c, h, e) => true })
            using (var client = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(500) })
            {
                try
                {
                    var response = await client.GetAsync(url);
                    var content = await response.Content.ReadAsStringAsync();
                    Directory.CreateDirectory(ip);
                    File.WriteAllText(Path.Combine(ip, $"index-{port}.html"), content);

                    Console.WriteLine("[+] Found page: " + url);

                    return;
                }
                catch
                {
                    continue;
                }
            }
        }
    }

    static void Main(string[] args)
    {
        MainAsync(args).GetAwaiter().GetResult();
    }

    static async Task MainAsync(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("[!] Please provide an argument: IP, IP range or subnet (e.g., 192.168.0.1, 192.168.0.1-192.168.0.10 or 192.168.0.0/24).");
            return;
        }

        string input = args[0];
        int threadCount = Environment.ProcessorCount;
        var ports = new[] { 80, 443, 8000, 8080, 8081 };

        foreach (var arg in args)
        {
            if (arg.StartsWith("--threads="))
            {
                if (int.TryParse(arg.Split('=')[1], out int val) && val > 0) threadCount = val;
            }
            else if (arg.StartsWith("--ports="))
            {
                string rawPorts = arg.Split('=')[1];
                string[] parsedPorts = rawPorts.Split(',');
                List<int> finalPorts = new List<int>();

                for (int i = 0; i < parsedPorts.Length; i++)
                {
                    try
                    {
                        finalPorts.Add(int.Parse(parsedPorts[i].Trim()));
                    }
                    catch
                    {
                        Console.WriteLine("[!] Invalid port " + parsedPorts[i] + ".");
                        return;
                    }
                }

                if (finalPorts.Count > 0)
                {
                    ports = finalPorts.ToArray();
                }
            }
            else if (arg == "--quiet")
            {
                quiet = true;
            }
            else if (arg.StartsWith("--timeout="))
            {
                try
                {
                    timeout = double.Parse(arg.Split('=')[1]);
                }
                catch (Exception)
                {
                    Console.WriteLine("[!] Invalid timeout. Use an integer that can be used as timeout milliseconds");
                    return;
                }
            }
        }

        var ipList = GetIPs(input);

        if (!quiet)
        {
            Console.WriteLine("[+] Using " + threadCount.ToString() + " threads");
            Console.WriteLine("[+] Using " + timeout.ToString() + " timeout");
            Console.WriteLine("[+] Scanning on ports: " + string.Join(", ", ports) + "");
        }

        var throttler = new SemaphoreSlim(threadCount);
        var tasks = new List<Task>();

        foreach (var ip in ipList)
        {
            foreach (var port in ports)
            {
                await throttler.WaitAsync();
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await FetchAndSaveAsync(ip, port);
                    }
                    finally
                    {
                        throttler.Release();
                    }
                }));
            }
        }

        await Task.WhenAll(tasks);
    }
}
