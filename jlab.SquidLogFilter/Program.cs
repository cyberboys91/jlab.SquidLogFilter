using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace SquidLogFilter
{
    class Program
    {
        static Regex userRegExp = new Regex(@"(\w+\.\w+)");
        static Dictionary<String, List<String>> users = new Dictionary<string, List<string>>()
            , ips = new Dictionary<string, List<string>>();
        static Dictionary<string, StreamWriter> writers = new Dictionary<string, StreamWriter>();

        static void Main(string[] args)
        {
            while (true)
            {
                Console.Write("Squid Log Folder Path: ");
                try
                {
                    DirectoryInfo info = new DirectoryInfo(Console.ReadLine());
                    foreach (FileInfo elem in info.GetFiles())
                        using (StreamReader reader = new StreamReader(elem.FullName))
                            Filter(reader);

                    using (StreamWriter userStatistics = new StreamWriter("user/statistics.txt"),
                           ipStatistics = new StreamWriter("ip/statistics.txt"))
                    {
                        //write trace for user (only ip)
                        foreach (var user in users)
                            using (StreamWriter writer = new StreamWriter("user/" + user.Key + "-ip.txt"))
                            {
                                foreach (var ip in user.Value)
                                    writer.WriteLine(ip);

                                userStatistics.WriteLine(String.Format("{0} : {1} ip", user.Key, user.Value.Count));
                                Console.WriteLine("Added all ip for user " + user.Key);
                            }

                        //write trace for ip (only user)
                        foreach (var ip in ips)
                            using (StreamWriter writer = new StreamWriter("ip/" + ip.Key + "-user.txt"))
                            {
                                foreach (var user in ip.Value)
                                    writer.WriteLine(user);

                                ipStatistics.WriteLine(String.Format("{0} : {1} users", ip.Key, ip.Value.Count));
                                Console.WriteLine("Added all user for ip " + ip.Key);
                            }
                    }

                    foreach (var writer in writers)
                        writer.Value.Close();

                    break;
                }
                catch
                {
                    Console.WriteLine("Invalid log folder path");
                }
            }
            Console.ReadLine();
        }

        private static void Filter(StreamReader reader)
        {
            string line = "";
            while ((line = reader.ReadLine()) != null)
            {
                try
                {
                    string[] split = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (split.Length > 3 && userRegExp.IsMatch(split[split.Length - 3]))
                    {
                        if (!Directory.Exists("user") || !Directory.Exists("ip"))
                        {
                            Directory.CreateDirectory("user");
                            Directory.CreateDirectory("ip");
                        }

                        string user = split[split.Length - 3], ip = split[2],
                            userAllFilePath = "user/" + user + "-all.txt",
                            ipAllFilePath = "ip/" + ip + "-all.txt",
                            userDateFilePath = "user/" + user + "-date.txt",
                            ipDateFilePath = "ip/" + ip + "-date.txt",
                            date = getDateForUnixMilliseconds(long.Parse(split[0].Remove(split[0].IndexOf('.'), 1))).ToString();

                        if (!writers.ContainsKey(userAllFilePath))
                            writers.Add(userAllFilePath, new StreamWriter(userAllFilePath));

                        if (!writers.ContainsKey(ipAllFilePath))
                            writers.Add(ipAllFilePath, new StreamWriter(ipAllFilePath));

                        if (!writers.ContainsKey(userDateFilePath))
                            writers.Add(userDateFilePath, new StreamWriter(userDateFilePath));

                        if (!writers.ContainsKey(ipDateFilePath))
                            writers.Add(ipDateFilePath, new StreamWriter(ipDateFilePath));

                        //write trace for user
                        StreamWriter writer = writers[userAllFilePath];
                        writer.WriteLine(line);

                        //write trace for user (only date and ip)
                        writer = writers[userDateFilePath];
                        writer.WriteLine(string.Format("{0} {1}", date, ip));
                        Console.WriteLine(string.Format("Added trace for date {0}", date));

                        //write trace for ip
                        writer = writers[ipAllFilePath];
                        writer.WriteLine(line);

                        //write for ip (only date and user)
                        writer = writers[ipDateFilePath];
                        writer.WriteLine(string.Format("{0} {1}", date, user));

                        if (users.ContainsKey(user) && !users[user].Contains(ip))
                            users[user].Add(ip);
                        else
                            users.Add(user, new List<string>() { ip });

                        if (ips.ContainsKey(ip) && !ips[ip].Contains(user))
                            ips[ip].Add(user);
                        else
                            ips.Add(ip, new List<string>() { user });
                    }
                }
                catch (Exception ignored)
                {

                }
            }
        }

        public static DateTime getDateForUnixMilliseconds(long unixMilliseconds)
        {
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddMilliseconds(unixMilliseconds).ToLocalTime();
            return dtDateTime;
        }
    }
}