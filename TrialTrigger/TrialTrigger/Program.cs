using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Diagnostics;

namespace TrialTrigger
{
    class Config
    {
        public string GroupName { get; set; }
        public string PlayerPath { get; set; }
        public string PlayerFolder { get; set; }
        public string ClientQty { get; set; }
        public string TestTime { get; set; }
        public string RemainedClientQty { get; set; }
        public string EnableList { get; set; }
        public string RenderingTime { get; set; }
        public string RenderingPort { get; set; }
        public string UDBPort { get; set; }
        public string ServerIP { get; set; }
    }

    class MainClass
    {
        private static string[] PackCameras(int[] dataArray, bool[] dataFlagArray, int packQty)
        {
            string[] packs = new string[packQty];
            int[] packSums = new int[packQty];

            int[] indexArray = new int[dataArray.Length];
            for (int i = 0; i < dataArray.Length; i ++)
            {
                indexArray[i] = i;
            }
            Array.Sort(dataArray, indexArray);

            for (int i = 0; i < packQty; i ++)
            {
                packs[i] = "";
                packSums[i] = 0;
            }

            for (int i = dataArray.Length - 1; i >= 0; i --)
            {
                int slotIndex = Array.IndexOf(packSums, packSums.Min());
                if (dataFlagArray[indexArray[i]] == true)
                {
                    packSums[slotIndex] += dataArray[i];
                    if (packs[slotIndex] == "")
                    {
                        packs[slotIndex] += indexArray[i].ToString();
                    }
                    else
                    {
                        packs[slotIndex] += ":" + indexArray[i].ToString();
                    }
                }
            }
            for (int i = 0; i < packs.Length; i ++)
            {
                if (packs[i] == "")
                {
                    packs[i] = "0";
                }
            }

            return packs;
        }

        private static string AddLocalHead(Config config)
        {
            string path = config.PlayerPath;
            string commandData = "start ";
            commandData += path + " ";
            return commandData;
        }
        private static string AddHead(Config config, string session)
        {
            string group = config.GroupName;
            string path = config.PlayerPath;
            string commandData = ".\\client.exe submit ";
            commandData += "/session " + session + " ";
            commandData += "/group " + group + " ";
            commandData += "/command cmd.exe \"/C\" \"start ";
            commandData += path + " ";
            return commandData;
        }
        private static string AddClient(Config config, int index, bool isTest, string enableList = "0")
        {
            string IP = config.ServerIP;
            string renderingPort = config.RenderingPort;
            string camera = isTest ? index.ToString() : enableList;
            string commandData = "-client " + index.ToString() + " ";
            commandData += IP + ":" + renderingPort + " *:* ";
            commandData += "-enablelist " + camera + " ";
            return commandData;
        }
        private static string AddServer(Config config, bool isTest)
        {
            string clientQty = isTest ? config.ClientQty : config.RemainedClientQty;
            string IP = config.ServerIP;
            string renderingPort = config.RenderingPort;
            string commandData = "-server " + clientQty + " ";
            commandData += IP + ":" + renderingPort + " *:* ";
            return commandData;
        }
        private static string AddMisc(Config config, bool isTest)
        {
            string time = isTest ? config.TestTime : config.RenderingTime;
            string UDBPort = config.UDBPort;
            string test = isTest ? "1" : "0";
            string commandData = "-test " + test + " ";
            commandData += "-time " + time + " ";
            commandData += "-port " + UDBPort + "";
            return commandData;
        }
        private static string AddServerCmd(Config config, bool isTest)
        {
            return (AddLocalHead(config) + AddServer(config, isTest) + AddMisc(config, isTest));
        }
        private static string AddClientCmd(Config config, string session, int index, bool isTest, string enableList = "0")
        {
            return (AddHead(config, session) + AddClient(config, index, isTest, enableList) + AddMisc(config, isTest) + "\"");
        }

        public static void Main(string[] args)
        {

            string configFileName = "config.json";
            string configString = File.ReadAllText(configFileName);
            Config config = JsonSerializer.Deserialize<Config>(configString);

            // Start Session 0

            string batchFilePath = "start.bat";
            FileStream fileStream;
            fileStream = new FileStream(batchFilePath, FileMode.Create, FileAccess.Write);
            StreamWriter streamWriter = new StreamWriter(fileStream);

            streamWriter.WriteLine(AddServerCmd(config, true));
            for (int i = 0; i < Int32.Parse(config.ClientQty); i ++)
            {
                streamWriter.WriteLine(AddClientCmd(config, "0", i, true));
            }

            // Wait Session 0

            streamWriter.WriteLine(".\\client.exe wait /session 0 /group " + config.GroupName);

            // Finish Writing

            streamWriter.Close();
            fileStream.Close();

            // Run bat file

            Process pro = new Process();
            pro.StartInfo.WorkingDirectory = Path.GetDirectoryName(batchFilePath);
            pro.StartInfo.FileName = Path.GetFileName(batchFilePath);
            pro.StartInfo.CreateNoWindow = false;
            pro.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            pro.Start();
            pro.WaitForExit();

            // Delete bat file

            if (File.Exists(batchFilePath))
            {
                File.Delete(batchFilePath);
            }

            // Collect data from tests

            string[] tmp = config.PlayerPath.Split(new Char[] { '\\' });
            string dataFolderName = tmp[tmp.Length - 1].Split(new Char[] { '.' })[0] + "_Data\\";
            string dataFileName = config.PlayerFolder + dataFolderName + "config.txt";
            string dataString = File.ReadAllText(dataFileName);
            string[] data = dataString.Split(new Char[] { ';' });
            
            string sampleFilePath = "sample.txt";
            FileStream fileStreamS;
            fileStreamS = new FileStream(sampleFilePath, FileMode.Create, FileAccess.Write);
            StreamWriter streamWriterS = new StreamWriter(fileStreamS);
            for (int i = 0; i < data.Length - 1; i++)
            {
                streamWriterS.WriteLine("Client " + data[i].Split(new Char[] { ':' })[0] + ", " + data[i].Split(new Char[] { ':' })[1] + ";");
            }
            streamWriterS.Close();
            fileStreamS.Close();

            // Start Session 1

            batchFilePath = "start.bat";
            fileStream = new FileStream(batchFilePath, FileMode.Create, FileAccess.Write);
            streamWriter = new StreamWriter(fileStream);

            int RemainedClientQty = Int32.Parse(config.RemainedClientQty);
            string[] PreparedEnableList;
            if (config.EnableList == "")
            {
                PreparedEnableList = new string[0];
            }
            else
            {
                PreparedEnableList = config.EnableList.Split(new Char[] { ';' });
            }
            streamWriter.WriteLine(AddServerCmd(config, false));

            if (RemainedClientQty <= PreparedEnableList.Length)
            {
                // Use config file to enable cameras
                for (int i = 0; i < RemainedClientQty - 1; i ++)
                {
                    streamWriter.WriteLine(AddClientCmd(config, "1", i, false, PreparedEnableList[i]));
                }
                // If the config file assigns too many agents, combine the remaining items
                string lastEnableList = "";
                for (int i = RemainedClientQty - 1; i < PreparedEnableList.Length; i ++)
                {
                    lastEnableList += PreparedEnableList[i] + (i == (PreparedEnableList.Length - 1) ? "" : ":");
                }
                streamWriter.WriteLine(AddClientCmd(config, "1", RemainedClientQty - 1, false, lastEnableList));
            }
            else
            {
                // Use config file to enable cameras
                for (int i = 0; i < PreparedEnableList.Length; i++)
                {
                    streamWriter.WriteLine(AddClientCmd(config, "1", i, false, PreparedEnableList[i]));
                }

                // Calculate the remaining cameras

                // Convert data string to int
                int[] dataArray = new int[data.Length - 1];
                for (int i = 0; i < dataArray.Length; i ++)
                {
                    dataArray[Int32.Parse(data[i].Split(new Char[] { ':' })[0])] = Int32.Parse(data[i].Split(new Char[] { ':' })[1]);
                }
                // Check whether a camera has been enabled
                bool[] dataFlagArray = new bool[data.Length - 1];
                for (int i = 0; i < dataFlagArray.Length; i ++)
                {
                    dataFlagArray[i] = true;
                }
                for (int i = 0; i < PreparedEnableList.Length; i ++)
                {
                    string[] lists = PreparedEnableList[i].Split(new Char[] { ':' });
                    for (int j = 0; j < lists.Length; j ++)
                    {
                        // If enabled, make cost 0
                        int no = Int32.Parse(lists[j]);
                        dataFlagArray[no] = false;
                        dataArray[no] = 0;
                    }
                }
                // Pack Cameras
                string[] CalculatedEnableList = PackCameras(dataArray, dataFlagArray, RemainedClientQty - PreparedEnableList.Length);
                for (int i = PreparedEnableList.Length; i < RemainedClientQty; i ++)
                {
                    streamWriter.WriteLine(AddClientCmd(config, "1", i, false, CalculatedEnableList[i - PreparedEnableList.Length]));
                }
            }

            // Wait Session 1

            streamWriter.WriteLine(".\\client.exe wait /session 1 /group " + config.GroupName);

            // Finish Writing

            streamWriter.Close();
            fileStream.Close();

            // Run bat file

            pro = new Process();
            pro.StartInfo.WorkingDirectory = Path.GetDirectoryName(batchFilePath);
            pro.StartInfo.FileName = Path.GetFileName(batchFilePath);
            pro.StartInfo.CreateNoWindow = false;
            pro.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            pro.Start();
            pro.WaitForExit();

            // Delete bat file

            if (File.Exists(batchFilePath))
            {
                //File.Delete(batchFilePath);
            }
        }
    }
}
