using System.IO;
using UnityEngine;
using UnityEditor;
using DistributedRenderingPluginComponent;

namespace DistributedRenderingPlugin
{
    public class CRP : MonoBehaviour
    {
        [MenuItem("Distributed Rendering/Add Controller", false)]
        static void CreateCustomGameObject(MenuCommand menuCommand)
        {
            GameObject test = GameObject.Find("<DR Controller>");
            if (test != null)
            {
                Debug.Log("Controller Exists!");
            }
            else
            {
                GameObject go = new GameObject("<DR Controller>");

                GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);

                Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);

                go.AddComponent<CRPComponent>();

                Selection.activeObject = go;
            }
        }
        [MenuItem("Distributed Rendering/Prepare Agent", false)]
        static void PrepareAgent(MenuCommand menuCommand)
        {
            string assetEditorPath = "Assets/Editor";
            string packagePath = "Packages/distributed-rendering-plugin";
            string patchName = "/PlayerPatch.dll";
            System.IO.Directory.CreateDirectory(assetEditorPath);
            if (File.Exists(packagePath + patchName))
            {
                if (File.Exists(assetEditorPath + patchName))
                {
                    Debug.Log("Agent Exists!");
                }
                else
                {
                    File.Copy(packagePath + patchName, assetEditorPath + patchName);
                }
            }
        }
    }
}
