using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;

namespace AdbProfiler
{
    public class DeviceWindow : EditorWindow
    {
        public static void ShowWindow()
        {
            var window = GetWindow<DeviceWindow>();
            window.titleContent = new GUIContent("DeviceWindow");
            DeviceWindow.InitDevice();
            window.Show();
        }
        public class Device
        {
            public string name;
            public string Name
            {
                get{
                    return remark;
                }
                set{
                    name = value;
                    remark = EditorPrefs.GetString(name, name);
                }
            }
            public string descrption;
            public string remark;
        }
        public static Device CurrentDevice = null;
        public static List<Device> AllDevice = new List<Device>();
        public static bool InitDevice()
        {
            AllDevice.Clear();
            string output = AdbMemoryProfiler.DoCmd($"{AdbMemoryProfiler.AdbInstallPath} devices -l");
            string[] allLine = output.Split('\n');
            string regex = "^(.*?)device ";
            for (int i = 0; i < allLine.Length; i++)
            {
                var line = allLine[i];
                var c = Regex.Matches(line, regex);
                if (c.Count > 0)
                {
                    
                    string temp = c[0].Value;
                    var name = temp.Replace("device", "").Replace("\n", "").Replace(" ", "").Replace("\r", "");
                    string desc = line.Substring(c[0].Length);
                    if(string.IsNullOrEmpty(name) == false)
                    {
                        var newDevice = new Device();
                        newDevice.Name = name;
                        newDevice.descrption = desc;
                        AllDevice.Add(newDevice);
                        if(CurrentDevice == null)
                        {
                            CurrentDevice = newDevice;
                            GetWindow<AdbMemoryProfiler>().ShowNotification(new GUIContent($"Connect Device:{newDevice.name}"));
                        }
                    }
                }
                
            }
            return AllDevice.Count > 0;
        }
        public static int deviceCount
        {
            get{
                string output = AdbMemoryProfiler.DoCmd($"{AdbMemoryProfiler.AdbInstallPath} devices");
                return 1;
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("Refresh Device"))
            {
                DeviceWindow.InitDevice();
            }

            if(CurrentDevice != null)
                EditorGUILayout.LabelField("Current", CurrentDevice.Name);

            EditorGUILayout.EndHorizontal();

            

            //EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Select Device");
            for (int i = 0; i < AllDevice.Count; i++)
            {
                var device = AllDevice[i];
                EditorGUILayout.BeginHorizontal();
                if(GUILayout.Button(device.descrption))
                {
                    CurrentDevice = device;
                    GetWindow<AdbMemoryProfiler>().ShowNotification(new GUIContent($"Connect Device:{device.Name}"));
                }
                device.Name = EditorGUILayout.TextField("DeviceName", device.Name);
                EditorGUILayout.EndHorizontal();
            }
            //EditorGUILayout.EndHorizontal();
        }

        void OnLostFocus()
        {
            Close();
        }
    }
}
