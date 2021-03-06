﻿using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Node.Connections;
using Node.Connections.Tcp;
using Node.Data;
using Node.Encryption;
using Node.Routing;
using Node.Serialization;

namespace Node
{
    internal class EntryPoint
    {
        private const string AddressFormat = "10.23.{0}.3";
        private const string AddressRegex = @"^10\.23\.\d+\.3$";
        //private const string AddressFormat = "172.16.16.1{0:00}";
        //private const string AddressRegex = @"^172\.16\.16\.1\d+$";

        private static void Main(string[] args)
        {
            var config = new StaticConfig
            {
                DesiredConnections = 3,
                MaxConnections = 20,
                ConnectCooldown = TimeSpan.FromMilliseconds(100),
                DisconnectCooldown = TimeSpan.FromMilliseconds(100),
                MapUpdateCooldown = TimeSpan.FromMilliseconds(50),
                KeySendCooldown = TimeSpan.FromSeconds(5),
                ConnectingSocketMaxTTL = TimeSpan.FromMilliseconds(50),
                ConnectingSocketsToConnectionsMultiplier = 5,
                PreconfiguredNodes = Enumerable.Range(1, 30).Select(i => new TcpAddress(new IPEndPoint(IPAddress.Parse(string.Format(AddressFormat, i)), 16900)) as IAddress).ToList(),
                LocalAddress = GetLocalAddress(16900),
                LongNames = true
            };
            var node = CreateNode(config, config, "storage");
            var consoleServer = new ConsoleServer(new IPEndPoint(IPAddress.Any, 16901), node);
            consoleServer.Start();
            node.Start();
        }

        private static TcpAddress GetLocalAddress(int port)
        {
            foreach (var @interface in NetworkInterface.GetAllNetworkInterfaces())
            {
                //Console.WriteLine(@interface.Name);
                var info = @interface.GetIPProperties().UnicastAddresses
                    .Where(i => i.Address.AddressFamily == AddressFamily.InterNetwork)
                    .FirstOrDefault(i => Regex.IsMatch(i.Address.ToString(), AddressRegex));
                if (info == null)
                    continue;
                return new TcpAddress(new IPEndPoint(info.Address, port));
            }
            throw new Exception("Could not find interface 10.23.*.3 to listen on!");
        }

        private static BluemeshNode CreateNode(IConnectionConfig connectionConfig, IRoutingConfig routingConfig, string storagePath)
        {
            var encryptionManager = new EncryptionManager(((TcpAddress)connectionConfig.LocalAddress).Endpoint, connectionConfig.KeySendCooldown);
            var connectionManager = new TcpConnectionManager(connectionConfig, routingConfig, encryptionManager);
            var routingManager = new RoutingManager(connectionManager, routingConfig);
            var dataManager = new DataManager(LoadStorage(storagePath) ?? new DataStorage(), storagePath, routingManager, encryptionManager);
            return new BluemeshNode(routingManager, connectionManager, dataManager, encryptionManager, routingConfig.DoLogMap);
        }

        private static IDataStorage LoadStorage(string path)
        {
            try
            {
                using (var stream = File.OpenRead(path))
                {
                    var deserializer = new StreamDeserializer(stream);
                    return DataStorage.Deserialize(deserializer);
                }
            }
            catch
            {
                Console.WriteLine("!! Cannot load data storage from '{0}'", path);
            }
            return null;
        }
    }
}
