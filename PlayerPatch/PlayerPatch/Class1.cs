using System;
using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace PlayerPatch
{
    [Serializable]
    class Agent
    {
        public string conductorServerHost;
        public int conductorServerPort;
        public Agent(string host, int port)
        {
            conductorServerHost = host;
            conductorServerPort = port;
        }
    }
    public class Class1 : IPostprocessBuildWithReport
    {
        public int callbackOrder { get { return 0; } }
        public void OnPostprocessBuild(BuildReport report)
        {
            string packagePath = "Packages/distributed-rendering-plugin";
            string outputPath = Directory.GetParent(report.summary.outputPath).ToString();
            
            string agentJsonName = "/agent.json";
            if (File.Exists(packagePath + agentJsonName))
            {
                File.Copy(packagePath + agentJsonName, outputPath + agentJsonName);
            }

            string agentDmsName = "/agent.exe";
            if (File.Exists(packagePath + agentDmsName))
            {
                File.Copy(packagePath + agentDmsName, outputPath + agentDmsName);
            }
        }
    }
}
