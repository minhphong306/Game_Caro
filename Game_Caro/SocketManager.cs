﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Game_Caro {
    public class SocketManager {
        #region Client
        Socket client;
        public bool ConnectServer(string serverIP) {
            IPEndPoint iep = new IPEndPoint(IPAddress.Parse(serverIP), PORT);
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            isServer = false;
            try {
                client.Connect(iep);
                return true;
            }
            catch {
                return false;
            }
        }
        #endregion

        #region server
        Socket server;
        public void CreateServer()
        {
            isServer = true;
            IPEndPoint iep = new IPEndPoint(IPAddress.Parse(IP), PORT);
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            server.Bind(iep);
            server.Listen(10);
            client = server.Accept();
            //Thread acceptClient = new Thread(() => {
            //    if (client != null)
            //    {
            //        SocketData firstConnect = new SocketData();
            //        firstConnect.Command = (int) SocketCommand.SEND_NAME;
            //        firstConnect.Message = "I'm server";
            //        byte[] bytes = SerializeData(firstConnect);
            //        SendData(client, bytes);
            //    }
            //});
            //acceptClient.IsBackground = true;
            //acceptClient.Start();
        }
        #endregion

        #region Both
        public string IP = "127.0.0.1";
        public int PORT = 9999;
        public const int BUFFER = 1024;
        public bool isServer = true;

        public bool Send(object data) {
            byte[] sendData = SerializeData(data);
            return SendData(client, sendData);
        }

        public object Receive() {
            byte[] receiveData = new byte[BUFFER];
            ReceiveData(client, receiveData);

            return DeserializeData(receiveData);
        }

        private bool SendData(Socket target, byte[] data) {
            return target.Send(data) > 0;
        }


        private bool ReceiveData(Socket target, byte[] data) {
            return target.Receive(data) > 0;
        }
        /// <summary>
        /// Nén đối tượng thành mảng byte[]
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public byte[] SerializeData(Object o) {
            MemoryStream ms = new MemoryStream();
            BinaryFormatter bf1 = new BinaryFormatter();
            bf1.Serialize(ms, o);
            return ms.ToArray();
        }

        /// <summary>
        /// Giải nén mảng byte[] thành đối tượng object
        /// </summary>
        /// <param name="theByteArray"></param>
        /// <returns></returns>
        public object DeserializeData(byte[] theByteArray) {
            MemoryStream ms = new MemoryStream(theByteArray);
            BinaryFormatter bf1 = new BinaryFormatter();
            ms.Position = 0;
            return bf1.Deserialize(ms);
        }

        /// <summary>
        /// Lấy ra IP V4 của card mạng đang dùng
        /// </summary>
        /// <param name="_type"></param>
        /// <returns></returns>
        public string GetLocalIPv4(NetworkInterfaceType _type) {
            string output = "";
            foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces()) {
                if (item.NetworkInterfaceType == _type && item.OperationalStatus == OperationalStatus.Up) {
                    foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses) {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork) {
                            output = ip.Address.ToString();
                        }
                    }
                }
            }
            return output;
        }

        #endregion
    }
}
