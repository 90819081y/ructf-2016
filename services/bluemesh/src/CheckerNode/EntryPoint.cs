﻿using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Node;
using Node.Connections;
using Node.Connections.Tcp;
using Node.Data;
using Node.Encryption;
using Node.Routing;
using Node.Serialization;

namespace CheckerNode
{
    internal class EntryPoint
    {
        private const string AddressFormat = "10.23.{0}.3";
        
        private static void Main(string[] args)
        {
            var config = new StaticConfig
            {
                DesiredConnections = 5,
                MaxConnections = 22,
                ConnectCooldown = TimeSpan.FromMilliseconds(100),
                DisconnectCooldown = TimeSpan.FromMilliseconds(100),
                MapUpdateCooldown = TimeSpan.FromMilliseconds(50),
                KeySendCooldown = TimeSpan.FromSeconds(10),
                ConnectingSocketMaxTTL = TimeSpan.FromMilliseconds(50),
                ConnectingSocketsToConnectionsMultiplier = 5,
                PreconfiguredNodes = Enumerable.Range(1, 22).Select(i => new TcpAddress(new IPEndPoint(IPAddress.Parse(string.Format(AddressFormat, i)), 16800)) as IAddress).ToList(),
                LocalAddress = GetLocalAddress(16800),
                LongNames = true,
                DoLogMap = true
            };
            var node = CreateNode(config, config, "storage");
            var consoleServer = new ConsoleServer(new IPEndPoint(IPAddress.Any, 16801), node);
            var extraServer = new ExtraConsoleServer(new IPEndPoint(IPAddress.Any, 16802), node);
            consoleServer.Start();
            extraServer.Start();
            node.Start();
        }

        private static TcpAddress GetLocalAddress(int port)
        {
            return (TcpAddress)new TcpUtility().ParseAddress("10.23.0.14:" + port);
        }

        private static CheckerNode CreateNode(IConnectionConfig connectionConfig, IRoutingConfig routingConfig, string storagePath)
        {
            var encryptionManager = new EncryptionManager(((TcpAddress)connectionConfig.LocalAddress).Endpoint, connectionConfig.KeySendCooldown);
            var connectionManager = new TcpConnectionManager(connectionConfig, routingConfig, encryptionManager);
            var routingManager = new RoutingManager(connectionManager, routingConfig);
            var dataManager = new DataManager(LoadStorage(storagePath) ?? new DataStorage(), storagePath, routingManager, encryptionManager);
            return new CheckerNode(routingManager, connectionManager, dataManager, encryptionManager, routingConfig.DoLogMap);
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