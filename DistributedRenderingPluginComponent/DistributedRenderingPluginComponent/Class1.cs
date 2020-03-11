using System;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Diagnostics;
using UnityEngine;

namespace DistributedRenderingPluginComponent
{
    public class SynchronousSocketListener
    {
        public string data = null;

        public string StartListening(string serverIP, int port)
        {
            byte[] bytes = new Byte[1024];

            IPAddress ipAddress = IPAddress.Parse(serverIP);
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);

            Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            //try
            {
                listener.Bind(localEndPoint);
                listener.Listen(10);
                Socket handler = listener.Accept();
                data = null;
                while (true)
                {
                    int bytesRec = handler.Receive(bytes);
                    data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                    if (data.IndexOf(";") > -1)
                    {
                        byte[] msg = Encoding.ASCII.GetBytes(data);
                        handler.Send(msg);
                        break;
                    }
                }
                return data;
            }
            //catch (Exception e)
            //{
                //return null;
            //}
        }

        public string GetClientData(string serverIP, int port)
        {
            return StartListening(serverIP, port);
        }
    }

    public class SynchronousSocketClient
    {
        public string StartClient(string data, string IP, int port)
        {
            byte[] bytes = new byte[1024];
            while (true)
            {
                try
                {
                    IPAddress ipAddress = IPAddress.Parse(IP);
                    IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);
                    Socket sender = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    try
                    {
                        sender.Connect(remoteEP);
                        byte[] msg = Encoding.ASCII.GetBytes(data + ";");
                        int bytesSent = sender.Send(msg);
                        int bytesRec = sender.Receive(bytes);
                        string stringRec = Encoding.ASCII.GetString(bytes, 0, bytesRec);
                        if ((data + ";") == stringRec)
                        {
                            sender.Shutdown(SocketShutdown.Both);
                            sender.Close();
                            return data;
                        }
                    }
                    catch (ArgumentNullException ane)
                    {
                        //return null;
                    }
                    catch (SocketException se)
                    {
                        //return null;
                    }
                    catch (Exception e)
                    {
                        //return null;
                    }
                }
                catch (Exception e)
                {
                    //return null;
                }
            }
        }

        public string SendClientData(string data, string IP, int port)
        {
            return StartClient(data, IP, port);
        }
    }

    public class CRPComponent : MonoBehaviour
    {
        public Camera[] CameraList;
        private Stopwatch stopwatch;
        private bool test;
        private int time;
        private long accTime = 0;
        private int testCamera = -1;
        private int clientQty = 0;
        private bool init = false;

        private string server;
        private string client;
        private string serverIP;
        private string port;

        private static string GetArg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == name && args.Length > i + 1)
                {
                    return args[i + 1];
                }
            }
            return null;
        }

        private static string GetSecondArg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == name && args.Length > i + 2)
                {
                    return args[i + 2];
                }
            }
            return null;
        }

        // Start is called before the first frame update
        void Start()
        {
            server = GetArg("-server");
            client = GetArg("-client");
            port = GetArg("-port");

            if ((server != null) || (client != null))
            {
                test = Int32.Parse(GetArg("-test")) == 1;
                time = Int32.Parse(GetArg("-time"));
                for (int i = 0; i < CameraList.Length; i++)
                {
                    CameraList[i].enabled = false;
                }
                if (server != null)
                {
                    clientQty = Int32.Parse(server);
                    serverIP = GetSecondArg("-server").Split(new Char[] { ':' })[0];
                }
                else if (client != null)
                {
                    int number = -1;
                    string enableList = GetArg("-enablelist");
                    string[] words = enableList.Split(new Char[] { ':' });
                    for (int i = 0; i < words.Length; i++)
                    {
                        number = Int32.Parse(words[i]);
                        if (number < CameraList.Length)
                        {
                            CameraList[number].enabled = true;
                        }
                    }
                    if (test)
                    {
                        testCamera = number;
                        serverIP = GetSecondArg("-client").Split(new Char[] { ':' })[0];
                    }
                }
            }
            stopwatch = new Stopwatch();
        }

        // Update is called once per frame
        void Update()
        {
            if (!init)
            {
                init = true;
                StartCoroutine(WaitAndQuit(time));
            }
            stopwatch.Reset();
            stopwatch.Start();
            StartCoroutine(AddTime());
        }

        private IEnumerator AddTime()
        {
            yield return new WaitForEndOfFrame();
            accTime += stopwatch.ElapsedMilliseconds;
        }

        private void SaveCameraCost(int index, long cost)
        {
            if (server != null)
            {
                string fileName = Application.dataPath + "/config.txt";
                FileStream fileStream = File.Open(fileName, FileMode.Create, FileAccess.Write);
                StreamWriter streamWriter = new StreamWriter(fileStream);
                int clientRemained = clientQty;
                while (clientRemained > 0)
                {
                    SynchronousSocketListener synchronousSocketListener = new SynchronousSocketListener();
                    string returnData = synchronousSocketListener.GetClientData(serverIP, Int32.Parse(port));
                    if (returnData != null)
                    {
                        streamWriter.WriteLine(returnData);
                        clientRemained--;
                    }
                }
                streamWriter.Close();
                fileStream.Close();
            }
            else if (client != null)
            {
                string data = index.ToString() + ":" + cost.ToString();
                string fileName = Application.dataPath + "/config.txt";
                FileStream fileStream = File.Open(fileName, FileMode.Create, FileAccess.Write);
                StreamWriter streamWriter = new StreamWriter(fileStream);
                SynchronousSocketClient synchronousSocketClient = new SynchronousSocketClient();
                streamWriter.WriteLine(synchronousSocketClient.SendClientData(data, serverIP, Int32.Parse(port)));
                streamWriter.Close();
                fileStream.Close();
            }
        }

        private IEnumerator WaitAndQuit(int waitTime)
        {
            yield return new WaitForSeconds(waitTime);
            // Save camera costs
            if (test)
            {
                SaveCameraCost(testCamera, accTime);
            }
            // Quit the player
            Application.Quit();
        }
    }
}
