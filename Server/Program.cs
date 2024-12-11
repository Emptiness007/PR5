using Server.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Server
{
    public class Program
    {
        static IPAddress ServerIpAddress;
        static int ServerPort;
        static int MaxClient;
        static int Duration;

        static List<Client> AllClients = new List<Client>();
        private static AppDbContext data = new AppDbContext();
        static void Main(string[] args)
        {
            OnSetings();
            Thread tListener = new Thread(ConnectServer);
            tListener.Start();

            Thread tDisconnect = new Thread(CheckDisconnectClient);
            tDisconnect.Start();
            while (true)
            {
                SetCommand();
            }
        }
        static void CheckDisconnectClient()
        {
            while (true)
            {
                for (int iClient = 0; iClient < AllClients.Count; iClient++)
                {
                    int ClientDuration = (int)DateTime.Now.Subtract(AllClients[iClient].DateConnect).TotalSeconds;

                    if(ClientDuration > Duration)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Client: {AllClients[iClient].Token} disconnect from server due to timeout");
                        AllClients.RemoveAt(iClient);
                        Console.WriteLine($"Client: {AllClients.Count}");
                    }
                }
                Thread.Sleep(1000);
            }
        }

        static void ConnectServer()
        {
            IPEndPoint endPoint = new IPEndPoint(ServerIpAddress, ServerPort);
            Socket SocketListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            SocketListener.Bind(endPoint);
            SocketListener.Listen(10);

            while (true)
            {
                Socket Handler = SocketListener.Accept();

                byte[] Bytes = new byte[10485760];
                int ByteRec = Handler.Receive(Bytes);
                string Message = Encoding.UTF8.GetString(Bytes, 0, ByteRec);
                string Response = SetCommandClient(Message);
                Handler.Send(Encoding.UTF8.GetBytes(Response));
            }

        }
        static string SetCommandClient(string Command)
        {
            if (Command.Contains("/token"))
            {
                if (AllClients.Count < MaxClient)
                {
                    string username = Command.Split(" ")[1];
                    string password = Command.Split(" ")[2];
                    Client client = data.Clients.FirstOrDefault(x => x.Username == username && x.Password == password);
                    if (client == null)
                    {
                        return "/disconnect";
                    }
                    client.CreateToken();
                    client.DateConnect = DateTime.Now;
                    data.SaveChanges();
                    if (client.IsBlacklisted)
                    {
                        return "User banned";
                    }
                    AllClients.Add(client);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"New client connection: " + client.Token);
                    Console.ForegroundColor = ConsoleColor.Red;

                    return client.Token;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"There is not enough space on the license server");
                    return "/limit";
                }
            }
            else
            {
                Client client = AllClients.Find(x => x.Token == Command);
                if (client != null)
                    return "/connect";
                else
                {
                    data.Clients.FirstOrDefault(x => x.Token == Command).Token = String.Empty;
                    data.SaveChanges();
                    return "/disconnect";
                }
            }
        }

        static void SetCommand()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            string Command = Console.ReadLine();
            if (Command == "/config")
            {
                File.Delete(Directory.GetCurrentDirectory() + "/.config");
                OnSetings();
            }
            else if (Command.Contains("/disconnect"))
            {
                DisconnectServer(Command);
            }
            else if (Command == "/status")
            {
                GetStatus();
            }
            else if (Command == "/help")
            {
                Help();
            }
            else if (Command.StartsWith("/ban"))
            {
                string[] commandParts = Command.Split(" ");
                if (commandParts.Length < 2)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Specify the username to add to the blacklist");
                    return;
                }
                BanClient(commandParts[1]);
            }
        }

        private static void BanClient(string username)
        {
            try
            {
                Client client = data.Clients.First(x => x.Username == username);
                client.IsBlacklisted = !client.IsBlacklisted;
                data.SaveChanges();
                if (client.IsBlacklisted)
                {
                    DisconnectServer(client.Token);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
            }
        }

        static void DisconnectServer(string command)
        {
            try
            {
                string Token = command.Replace("/disconnect ", "");
                Classes.Client DiscconnectClient = AllClients.Find(x => x.Token == Token);
                AllClients.Remove(DiscconnectClient);

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Client: {Token} disconnect from server");
            }
            catch (Exception exp)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: " + exp.Message);
            }
            
        }

        static void Help()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("Commands to the server: ");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("/config");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(" - set initial settings");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("/disconnect");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(" - disconnect users from server");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("/status");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(" - show list users");
        }

        static void OnSetings()
        {
            string Path = Directory.GetCurrentDirectory() + "/.config";
            string IpAddress = "";
            if (File.Exists(Path))
            {
                StreamReader streamReader = new StreamReader(Path);
                IpAddress = streamReader.ReadLine();
                ServerIpAddress = IPAddress.Parse(IpAddress);
                ServerPort = int.Parse(streamReader.ReadLine());
                MaxClient = int.Parse(streamReader.ReadLine());
                Duration = int.Parse(streamReader.ReadLine());
                streamReader.Close();
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Server address: ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(IpAddress);

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Server port: ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(ServerPort.ToString());

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Max count clients: ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(MaxClient.ToString());

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Token lifetime: ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(Duration.ToString());
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Please provide the IP address if the license server: ");
                Console.ForegroundColor = ConsoleColor.Green;
                IpAddress = Console.ReadLine();
                ServerIpAddress = IPAddress.Parse(IpAddress);

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Please specify the license server port: ");
                Console.ForegroundColor = ConsoleColor.Green;
                ServerPort = int.Parse(Console.ReadLine());

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Please indicate the largest number of clients: ");
                Console.ForegroundColor = ConsoleColor.Green;
                MaxClient = int.Parse(Console.ReadLine());

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Specify the token lifetime: ");
                Console.ForegroundColor = ConsoleColor.Green;
                Duration = int.Parse(Console.ReadLine());

                StreamWriter streamWriter = new StreamWriter(Path);
                streamWriter.WriteLine(IpAddress);
                streamWriter.WriteLine(ServerPort.ToString());
                streamWriter.WriteLine(MaxClient.ToString());
                streamWriter.WriteLine(Duration.ToString());
                streamWriter.Close();
            }
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("To change, write the command: ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("/config");
        }

        static void GetStatus()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Count clients: {AllClients.Count}");
            foreach (Client Client in AllClients)
            {
                int Duration = (int)DateTime.Now.Subtract(Client.DateConnect).TotalSeconds;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"Client: {Client.Token}, time connection: {Client.DateConnect.ToString("HH:mm:ss dd.MM")}, " + $" duration: {Duration}");
            }
        }
    }
}
