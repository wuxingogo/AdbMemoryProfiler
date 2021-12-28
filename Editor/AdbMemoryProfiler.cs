﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.Text.RegularExpressions;
using UnityEngine.Profiling;
using UnityEditorInternal;
using UnityEditor.Profiling;
using System.IO;
using UnityEngine.Profiling.Memory.Experimental;
using System;

namespace AdbProfiler
{


    public class AdbMemoryProfiler : EditorWindow
    {

        public enum CaptureMode
        {
            PerSecound,
            PerFrame,
        }
        [MenuItem("Window/Analysis/ADB Memory Dump")]
        private static void ShowWindow()
        {
            var window = GetWindow<AdbMemoryProfiler>();
            window.titleContent = new GUIContent("AdbMemoryProfiler");
            AdbMemoryProfiler.Init(window);
            window.Show();
        }
        public static void Init(AdbMemoryProfiler window)
        {
            window.OnCreate();
        }
        public const float CAPTURE_DELAY = 1.0f;
        public static int lastCaptureFrameIndex = -1;
        private float currentTime = 0;
        public CaptureMode captureMode = CaptureMode.PerSecound;
        private float lastUpdateTime = 0;
        public bool isRunning = false;
        public static string packageName
        {
            get
            {
                if (string.IsNullOrEmpty(_packageName))
                {
                    _packageName = PlayerSettings.applicationIdentifier;
                }
                return _packageName;
            }
            set
            {
                _packageName = value;
            }
        }
        private static string _packageName = "";
        public string platformName = "";
        public int maxFrameCount = 100;
        public int frameCaptureCount
        {
            get
            {
                cacheFrameCount = EditorPrefs.GetInt("AdbMemoryProfiler_frameCaptureCount", 5);
                return cacheFrameCount;
            }
            set
            {
                cacheFrameCount = value;
                EditorPrefs.SetInt("AdbMemoryProfiler_frameCaptureCount", value);
            }
        }
        private int cacheFrameCount = 5;

        private ProfilerArea currentArea = ProfilerArea.Memory;

        public DirectoryInfo textureFolder
        {
            get
            {
                if (_textureFolder == null)
                {
                    _textureFolder = new DirectoryInfo(Application.dataPath + "/../screenshot/");
                }
                if (_textureFolder.Exists == false)
                {
                    _textureFolder.Create();
                }
                return _textureFolder;
            }
        }
        private DirectoryInfo _textureFolder = null;
        private GUIStyle hoverStyle
        {
            get
            {
                if (_hoverStyle == null)
                {
                    _hoverStyle = new GUIStyle()
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontStyle = FontStyle.Bold,

                        normal = new GUIStyleState()
                        {
                            textColor = Color.white
                        },
                        hover = new GUIStyleState()
                        {
                            background = Texture2D.whiteTexture
                        },
                        // active = new GUIStyleState()
                        // {
                        //     background = Texture2D.whiteTexture
                        // }
                    };
                }
                return _hoverStyle;
            }
        }

        public void OnCreate()
        {
            cacheFrameCount = frameCaptureCount;
        }
        private GUIStyle _hoverStyle;

        public class FrameInfo
        {
            public class ProfilerData
            {
                public bool isSizeOutput = false;
            }
            public static Dictionary<string, ProfilerData> PROFILER_SETTING = new Dictionary<string, ProfilerData>()
            {
                {"Total Allocated", new ProfilerData(){isSizeOutput = true}},
                {"Texture Memory", new ProfilerData(){isSizeOutput = true}},
                {"Mesh Memory", new ProfilerData(){isSizeOutput = true}},
                {"Total GC Allocated", new ProfilerData(){isSizeOutput = true}},
                {"GC Allocated", new ProfilerData(){isSizeOutput = true}},
                {"Mono", new ProfilerData(){isSizeOutput = true}},
                {"Gfx", new ProfilerData(){isSizeOutput = true}},
                {"PSS", new ProfilerData(){isSizeOutput = true}},
                {"Unknown", new ProfilerData(){isSizeOutput = true}},
            };
            public int frameIndex;
            public double unknownSize
            {
                get
                {
                    return _unknown;
                }
                set
                {
                    _unknown = value * 1024;
                    RecordProperty("Unknown", _unknown);
                }
            }
            private double _unknown;
            public double PssSize
            {
                get
                {
                    return _pss;
                }
                set
                {
                    _pss = value * 1024;
                    RecordProperty("PSS", _pss);
                }
            }
            private double _pss;
            [Header("Memory Area")]
            public double totalAllocated;
            public double textureMemory;
            public double meshMemory;
            public double totalGCAllocated;
            public double gcAllocated;
            public double heap;
            public Dictionary<string, long> frameInfo = new Dictionary<string, long>();
            public string monoStr
            {
                set
                {
                    monoMemory = CaclulateMemory(value);
                    RecordProperty("Mono", monoMemory);

                }
            }
            public double monoMemory;
            public string gfxDriverStr
            {
                set
                {
                    gfxMemory = CaclulateMemory(value);
                    RecordProperty("Gfx", gfxMemory);
                }
            }
            public double gfxMemory;

            const string delimiter = ",";
            const string newline = "\n";

            public string screenShotPath = string.Empty;
            public double CaclulateMemory(string field)
            {
                if (string.IsNullOrEmpty(field))
                    return 0;
                var array = field.Split(' ');
                if (array.Length < 1)
                {
                    return 0;
                }
                double result = 0;
                if (double.TryParse(array[0], out result))
                {
                    switch (array[1])
                    {
                        case "B":
                            return result;
                        case "KB":
                            return result * 1024;
                        case "MB":
                            return result * 1024 * 1024;
                        case "GB":
                            return result * 1024 * 1024 * 1024;
                    }
                }
                return 0;
            }
            public override string ToString()
            {

                string result = "";

                foreach (var info in frameInfo)
                {
                    var key = info.Key;
                    bool isSizeOutput = false;
                    if (PROFILER_SETTING.ContainsKey(key))
                    {
                        isSizeOutput = PROFILER_SETTING[key].isSizeOutput;
                    }

                    if (isSizeOutput)
                    {
                        result += string.Format("{0}:{1},", info.Key, SizeToString(info.Value));
                    }
                    else
                    {
                        result += string.Format("{0}:{1},", info.Key, info.Value);
                    }
                }
                // if(result.Length > 1)
                // result = result.Substring(0, result.Length - 1);
                return result;
                // return string.Format("Android PSS {5}, Unknow {6}, totalAllocated : {0}, textureMemory : {1}, meshMemory : {2}, totalGCAllocated : {3}, gcAllocated : {4}., heap : {7}, Mono: {8}, GfxDriver : {9}.", v1, v2, v3, v4, v5, vPSS, vUnknown, v6, v7, v8);
            }
            public string ToExcelString()
            {
                string result = "";

                foreach (var info in frameInfo)
                {
                    var key = info.Key;
                    bool isSizeOutput = false;
                    if (PROFILER_SETTING.ContainsKey(key))
                    {
                        isSizeOutput = PROFILER_SETTING[key].isSizeOutput;
                    }

                    if (isSizeOutput)
                    {
                        result += string.Format("{0},", SizeToString(info.Value, true));
                    }
                    else
                    {
                        result += string.Format("{0},", info.Value);
                    }
                }
                return result;
            }
            public string FullExcelString()
            {
                string result = "";

                foreach (var info in frameInfo)
                {
                    var key = info.Key;
                    bool isSizeOutput = false;
                    if (PROFILER_SETTING.ContainsKey(key))
                    {
                        isSizeOutput = PROFILER_SETTING[key].isSizeOutput;
                    }

                    if (isSizeOutput)
                    {
                        result += string.Format("{0},", SizeToString(info.Value, true));
                    }
                    else
                    {
                        result += string.Format("{0},", info.Value);
                    }
                }
                return result;
            }

            public string ExcelHeadString()
            {
                //Name	PSS	Unknown	Total Allocated	Texture Memory	Mesh Memory	Material Count	Object Count	Total GC Allocated	GC Allocated	Mono	Gfx
                string result = "";
                foreach (var info in frameInfo)
                {
                    var key = info.Key;
                    result += string.Format("{0}{1}", key, delimiter);
                }
                result += newline;
                return result;
            }
            public string ExcelSummaryHeadString()
            {
                //Name	PSS	Unknown	Total Allocated	Texture Memory	Mesh Memory	Material Count	Object Count	Total GC Allocated	GC Allocated	Mono	Gfx
                string result = "";
                foreach (var info in frameInfo)
                {
                    var key = info.Key;
                    result += string.Format("{0}Ave{1}", key, delimiter);
                }
                foreach (var info in frameInfo)
                {
                    var key = info.Key;
                    result += string.Format("{0}Max{1}", key, delimiter);
                }
                result += newline;
                return result;
            }

            
            public string FullToString()
            {
                return string.Format("[{0}]-{1}", frameIndex, ToString());
            }
            public string SizeToString(double sizeB, bool replaceDelimiter = false)
            {
                var result = string.Empty;
                if (sizeB > 1024 * 1024 * 1024)
                {
                    result = string.Format("{0:N1} GB", sizeB / 1024.0 / 1024.0f / 1024.0f);
                }
                if (sizeB > 1024 * 1024)
                {
                    result = string.Format("{0:N1} MB", sizeB / 1024.0 / 1024.0f);
                }
                else if (sizeB > 1024)
                {
                    result = string.Format("{0:N1} KB", sizeB / 1024);
                }
                else
                {
                    result = string.Format("{0:N1} B", sizeB);
                }

                if (replaceDelimiter)
                {
                    result = result.Replace(",", "");
                }
                return result;
            }
            public double Kb2Mb(double size)
            {
                return size * 1.0f / 1024;
            }
            public double B2Mb(double size)
            {
                return size * 1.0f / 1024 / 1024;
            }
            public FrameInfo Clone()
            {
                var newFrameInfo = new FrameInfo();
                newFrameInfo.frameIndex = frameIndex;
                newFrameInfo.unknownSize = unknownSize;
                newFrameInfo.PssSize = PssSize;
                newFrameInfo.totalAllocated = totalAllocated;
                newFrameInfo.textureMemory = textureMemory;
                newFrameInfo.meshMemory = meshMemory;
                newFrameInfo.totalGCAllocated = totalGCAllocated;
                newFrameInfo.gcAllocated = gcAllocated;
                newFrameInfo.heap = heap;
                newFrameInfo.monoMemory = monoMemory;
                newFrameInfo.gfxMemory = gfxMemory;
                newFrameInfo.screenShotPath = screenShotPath;
                return newFrameInfo;
            }

            public void RecordProperty(string propertyName, double f)
            {
                frameInfo[propertyName] = (long)f;
            }

            public void RecordProperty(string propertyName, string size)
            {

            }
        }
        public class AnalyticsInfo
        {
            public string name;
            public List<FrameInfo> totalFrameInfo = null;
            public FrameInfo maxFrameInfo = null;
            public FrameInfo averageFrameInfo = null;

            string delimiter = ",";
            string newline = "\n";
            public string SummaryContent()
            {
                var head = "";
                head += name + delimiter;
                head += averageFrameInfo.FullExcelString();
                head += maxFrameInfo.FullExcelString();
                head += newline;
                return head;
            }

            public string ToDetailString()
            {
                var head = name + newline;
                for (int i = 0; i < totalFrameInfo.Count; i++)
                {
                    var frameInfo = totalFrameInfo[i];
                    head += i + delimiter;
                    head += frameInfo.FullExcelString();
                    head += newline;
                }
                return head;
            }
            public string ExcelHeadString()
            {
                if (totalFrameInfo.Count > 0)
                {
                    return totalFrameInfo[0].ExcelHeadString();
                }
                return "";
            }
            public string SummaryHeadString()
            {
                if (totalFrameInfo.Count > 0)
                {
                    return totalFrameInfo[0].ExcelSummaryHeadString();
                }
                return "";
            }
        }
        public List<AnalyticsInfo> TotalAnalyticsInfo = new List<AnalyticsInfo>();
        public List<FrameInfo> totalFrameInfo = new List<FrameInfo>();
        public string subName = "NewLabel";
        Vector2 scrollPos;
        private void OnGUI()
        {
            var maxInfo = GetMaxInfo();
            var averageInfo = GetAverageInfo();
            EditorGUILayout.BeginHorizontal();
            if (Button("StartSample"))
            {
                BeginSample();
            }
            if (Button("EndSample"))
            {
                EndSample();
            }
            if (Button("MemoryTakeSample"))
            {
                MemoryTakeSample();
            }
            if (Button("Pause"))
            {
                isRunning = !isRunning;
            }
            if (Button("Clear"))
            {
                totalFrameInfo.Clear();
                TotalAnalyticsInfo.Clear();
            }
            if (Button("Capture"))
            {
                cacheFrameCount = 1;
            }
            if (Button("NewSubInfo"))
            {
                var analyticsInfo = new AnalyticsInfo();
                var subName = this.subName;

                analyticsInfo.totalFrameInfo = new List<FrameInfo>(totalFrameInfo);
                analyticsInfo.maxFrameInfo = maxInfo;
                analyticsInfo.averageFrameInfo = averageInfo;
                analyticsInfo.name = subName;
                TotalAnalyticsInfo.Add(analyticsInfo);

                totalFrameInfo.Clear();

                this.subName = "NewLabel";
            }
            if (Button("ExportCSV"))
            {
                Export();
            }
            if (Button("OpenCSV"))
            {
                Open();
            }
            if (Button("TakeSnapShot"))
            {
                TakeSnapshot();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(string.Format("Platform : {0}", platformName));
            packageName = EditorGUILayout.TextField("PackageName", string.Format("{0}", packageName));
            captureMode = (CaptureMode)EditorGUILayout.EnumPopup(captureMode);
            currentArea = (ProfilerArea)EditorGUILayout.EnumPopup(currentArea);
            if (Button("RestartGame"))
            {
                DoCmd("adb shell am force-stop " + packageName);
                DoCmd(string.Format("adb shell monkey -p {0} -c android.intent.category.LAUNCHER 1", packageName));
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(string.Format("Max           : {0}", maxInfo.ToString()));
            EditorGUILayout.LabelField(string.Format("Averange      : {0}", averageInfo.ToString()));


            int currentCount = maxFrameCount;

            EditorGUILayout.BeginHorizontal();
            subName = EditorGUILayout.TextField("CurrentSubName", subName);
            int.TryParse(EditorGUILayout.TextField("MaxShowCount", maxFrameCount.ToString()), out maxFrameCount);


            cacheFrameCount = EditorGUILayout.IntField("CaptureCount", cacheFrameCount);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            //GUILayout.FlexibleSpace();
            for (int i = TotalAnalyticsInfo.Count - 1; i >= 0; i--)
            {
                var totalFrame = TotalAnalyticsInfo[i];
                if (GUILayout.Button(totalFrame.name, EditorStyles.miniButtonLeft, GUILayout.Width(85)))
                {
                    // MemoryDetailWindow.InitWindow(totalFrame);
                }

            }
            EditorGUILayout.EndHorizontal();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);


            for (int i = totalFrameInfo.Count - 1; i >= 0; i--)
            {
                currentCount--;
                if (currentCount <= 0)
                {
                    EditorGUILayout.LabelField(i + " FrameInfo was hidden.");
                    break;

                }

                var frame = totalFrameInfo[i];

                EditorGUILayout.BeginHorizontal();

                // EditorGUILayout.LabelField(string.Format("unknownSize : {0:N1} Mb, PSS : {1:N1} Mb.", frame.unknownSize,frame.totalSize));

                EditorGUILayout.LabelField(frame.FullToString(), EditorStyles.miniButtonLeft, GUILayout.Width(1350));
                if (string.IsNullOrEmpty(frame.screenShotPath) == false)
                {
                    if (Button("Texture"))
                    {
                        var path = textureFolder + frame.screenShotPath;
                        UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(path, 1);
                        // EditorUtility.RevealInFinder(path);
                    }
                }

                EditorGUILayout.EndHorizontal();
            }
            for (int i = TotalAnalyticsInfo.Count - 1; i >= 0; i--)
            {
                var totalFrame = TotalAnalyticsInfo[i];
                EditorGUILayout.BeginHorizontal();
                totalFrame.name = EditorGUILayout.TextField(totalFrame.name);
                if (Button("Delete"))
                {
                    TotalAnalyticsInfo.RemoveAt(i);
                    continue;
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.LabelField(string.Format("FrameCount           : {0}", totalFrame.totalFrameInfo.Count.ToString()));
                EditorGUILayout.LabelField(string.Format("Max           : {0}", totalFrame.maxFrameInfo.ToString()));
                EditorGUILayout.LabelField(string.Format("Averange      : {0}", totalFrame.averageFrameInfo.ToString()));
            }

            EditorGUILayout.EndScrollView();
        }

        public void Export()
        {
            string delimiter = ",";
            string newline = "\n";
            // var head = string.Format("Name{0}Android PSS Avg{0}UnknowAvg{0}totalAllocatedAvg{0}textureMemoryAvg{0}meshMemoryAvg{0}totalGCAllocatedAvg{0}gcAllocatedAvg{0}heapAvg{0}MonoAvg{0}GfxDriverAvg{0}Android PSS Max{0}UnknowMax{0}totalAllocatedMax{0}textureMemoryMax{0}meshMemoryMax{0}totalGCAllocatedMax{0}gcAllocatedMax{0}heapMax{0}MonoMax{0}GfxDriverMax{1}", delimiter, newline);
            var head = "";
            var sourceHead = "Name" + delimiter;
            var summaryHead = string.Copy(sourceHead);
            if (TotalAnalyticsInfo.Count > 0)
            {
                sourceHead += TotalAnalyticsInfo[0].ExcelHeadString();
                summaryHead += TotalAnalyticsInfo[0].SummaryHeadString();
            }
            head += summaryHead;
            foreach (var item in TotalAnalyticsInfo)
            {
                var totalFrame = item;

                head += totalFrame.SummaryContent();
            }

            head += newline;
            head += newline;

            // head += string.Format("Name{0}Android PSS{0}Unknow{0}totalAllocated{0}textureMemory{0}meshMemory{0}totalGCAllocated{0}gcAllocated{0}heap{0}Mono{0}GfxDriver{0}ScreenShot{1}", delimiter, newline);
            head += sourceHead;
            foreach (var item in TotalAnalyticsInfo)
            {
                var totalFrame = item;

                head += totalFrame.ToDetailString();
            }
            var folder = textureFolder.FullName;
            var path = EditorUtility.SaveFilePanel("Save Excel", folder, "FrameData.csv", "csv");
            if (path.Length != 0)
                File.WriteAllText(path, head);
        }
        public void Open()
        {
            var folder = textureFolder.FullName;
            var path = EditorUtility.OpenFilePanel("Save Excel", folder, "csv");

            totalFrameInfo.Clear();
            TotalAnalyticsInfo.Clear();

        }
        public FrameInfo GetMaxInfo()
        {
            // double unknownSizeMax = 0;
            // double totalSizeMax = 0;
            // double totalAllocatedMax = 0;
            // double textureMemoryMax = 0;
            // double meshMemoryMax = 0;
            // double totalGCAllocateMax = 0;
            // double gcAllocatedMax = 0;
            // double heapMax = 0;
            // double monoMax = 0;
            // double gfxMax = 0;

            var maxFrame = new FrameInfo();


            for (int i = 0; i < totalFrameInfo.Count; i++)
            {
                var frame = totalFrameInfo[i];

                // double unknownSize = frame.unknownSize;
                // double totalSize = frame.PssSize;
                // double totalAllocated = frame.totalAllocated;
                // double textureMemory = frame.textureMemory;
                // double meshMemory = frame.meshMemory;;
                // double totalGCAllocated = frame.totalGCAllocated;
                // double gcAllocated = frame.gcAllocated;
                // double heap = frame.heap;
                // double monoMemory = frame.monoMemory;
                // double gfxMemory = frame.gfxMemory;

                // unknownSizeMax = Math.Max(unknownSize, unknownSizeMax);
                // totalSizeMax = Math.Max(totalSize, totalSizeMax);
                // totalAllocatedMax = Math.Max(totalAllocated, totalAllocatedMax);
                // textureMemoryMax = Math.Max(textureMemory, textureMemoryMax);
                // meshMemoryMax = Math.Max(meshMemory, meshMemoryMax);
                // totalGCAllocateMax = Math.Max(totalGCAllocated, totalGCAllocateMax);
                // gcAllocatedMax = Math.Max(gcAllocated, gcAllocatedMax);
                // heapMax = Math.Max(heap, heapMax);
                // monoMax = Math.Max(monoMemory, monoMax);
                // gfxMax = Math.Max(gfxMemory, gfxMax);

                foreach (var frameInfo in frame.frameInfo)
                {
                    if (maxFrame.frameInfo.ContainsKey(frameInfo.Key) == false)
                    {
                        maxFrame.frameInfo.Add(frameInfo.Key, -1);
                    }
                    var v = maxFrame.frameInfo[frameInfo.Key];
                    maxFrame.frameInfo[frameInfo.Key] = Math.Max(v, frameInfo.Value);
                }
            }
            // maxFrame.unknownSize = unknownSizeMax;
            // maxFrame.PssSize = totalSizeMax;
            // maxFrame.totalAllocated = totalAllocatedMax;
            // maxFrame.textureMemory = textureMemoryMax;
            // maxFrame.meshMemory = meshMemoryMax;
            // maxFrame.totalGCAllocated = totalGCAllocateMax;
            // maxFrame.gcAllocated = gcAllocatedMax;
            // maxFrame.heap = heapMax;
            // maxFrame.monoMemory = monoMax;
            // maxFrame.gfxMemory = gfxMax;
            return maxFrame;
        }
        public FrameInfo GetAverageInfo()
        {
            
            

            double count = totalFrameInfo.Count;
            Dictionary<string, long> totalValue = new Dictionary<string, long>();
            if(count > 0)
            {
                var firstFrame = totalFrameInfo[0];
                var frameInfo = firstFrame.frameInfo;
                foreach (var item in frameInfo)
                {
                    totalValue[item.Key] = item.Value;
                }

                for (int i = 1; i < count; i++)
                {
                    var frame = totalFrameInfo[i];

                    var keyPair = frame.frameInfo;
                    foreach (var item in keyPair)
                    {
                        if(totalValue.ContainsKey(item.Key))
                        {
                            var sourceValue = totalValue[item.Key];
                            long v = (sourceValue + item.Value) / 2;
                            totalValue[item.Key] = v;
                        }
                        else{
                            totalValue[item.Key] = item.Value;
                        }
                        
                    }
                }
                var maxFrame = new FrameInfo();
                maxFrame.frameInfo = totalValue;
                // foreach (var item in totalValue)
                // {
                //     maxFrame.frameInfo[item.Key] = (long)(item.Value / count);
                // }
                return maxFrame;
            }
            return new FrameInfo();
        }
        public float Kb2Mb(float size)
        {
            return size * 1.0f / 1024;
        }
        public float B2Mb(float size)
        {
            return size * 1.0f / 1024 / 1024;
        }

        public void UpdateInfo()
        {
            if (isRunning)
            {
                if (captureMode == CaptureMode.PerSecound)
                {
                    if (currentTime > CAPTURE_DELAY)
                    {
                        lastUpdateTime = Time.realtimeSinceStartup;
                        Capture();
                    }

                    currentTime = Time.realtimeSinceStartup - lastUpdateTime;

                }
                else if (captureMode == CaptureMode.PerFrame)
                {
                    if (lastCaptureFrameIndex != ProfilerDriver.lastFrameIndex)
                    {
                        Capture();
                    }
                }



                this.Repaint();
            }
        }

        public void Capture()
        {
            var output = DoCmd("adb shell dumpsys meminfo " + packageName);
            //Log(output);
            string[] allLines = output.Split('\n');
            outPut = GetOutPut(allLines, "Unknown", "TOTAL");
            foreach (var item in outPut)
            {
                //Log(item.Key + " : " + item.Value);
            }

            var frameInfo = new FrameInfo();

            if (outPut.ContainsKey("Unknown") && outPut.ContainsKey("TOTAL"))
            {

                var totalSize = int.Parse(outPut["TOTAL"]);
                var unknownSize = int.Parse(outPut["Unknown"]);
                frameInfo.PssSize = totalSize;
                frameInfo.unknownSize = unknownSize;
                frameInfo.frameIndex = totalFrameInfo.Count;
                totalFrameInfo.Add(frameInfo);
            }

            // using (var frameData = ProfilerDriver.GetHierarchyFrameDataView(0, 0, HierarchyFrameDataView.ViewModes.Default, HierarchyFrameDataView.columnGcMemory, false))
            // {
            //     int rootId = frameData.GetRootItemID();
            //     float fps = frameData.frameFps;
            //     UnityEngine.Debug.Log("rootID : " + rootId + ", fps : " + fps);
            // }

            string targetName = ProfilerDriver.GetConnectionIdentifier(ProfilerDriver.connectedProfiler);
            platformName = targetName;

            int[] availableProfilers = ProfilerDriver.GetAvailableProfilers();

            var first = ProfilerDriver.firstFrameIndex;
            lastCaptureFrameIndex = ProfilerDriver.lastFrameIndex;

            //Memory Area
            var statistics = ProfilerDriver.GetGraphStatisticsPropertiesForArea(currentArea);

            foreach (var propertyName in statistics)
            {
                var id = ProfilerDriver.GetStatisticsIdentifierForArea(currentArea, propertyName);
                var buffer = new float[1];
                ProfilerDriver.GetStatisticsValues(id, lastCaptureFrameIndex, 1, buffer, out var maxValue);

                //UnityEngine.Debug.Log("propertyName : " + propertyName + ", size : " + buffer[0]);
                //mb
                if (propertyName == "Total Allocated") frameInfo.totalAllocated = buffer[0];
                else if (propertyName == "Texture Memory") frameInfo.textureMemory = buffer[0];
                else if (propertyName == "Mesh Memory") frameInfo.meshMemory = buffer[0];
                //kb
                else if (propertyName == "Total GC Allocated") frameInfo.totalGCAllocated = buffer[0];
                else if (propertyName == "GC Allocated") frameInfo.gcAllocated = buffer[0];

                frameInfo.RecordProperty(propertyName, buffer[0]);
            }

            var allProperty = ProfilerDriver.GetAllStatisticsProperties();
            var overview = ProfilerDriver.miniMemoryOverview;

            var text = ProfilerDriver.GetOverviewText(currentArea, lastCaptureFrameIndex);
            // ProfilerDriver.RequestObjectMemoryInfo(m_GatherObjectReferences);

            frameInfo.heap = ProfilerDriver.usedHeapSize;
            var matchMono = "(?<=Mono:\\s)([0-9]*.[0-9]*\\s(MB|GB))(?=\\s\\s\\s)";
            var matchGfx = "(?<=GfxDriver:\\s)([0-9]*.[0-9]*\\s(MB|GB))(?=\\s\\s\\s)";
            frameInfo.monoStr = GetOverviewText(text, matchMono);
            frameInfo.gfxDriverStr = GetOverviewText(text, matchGfx);
            SaveToFrameInfo(frameInfo, cacheFrameCount);

            if (frameCaptureCount != cacheFrameCount)
            {
                frameCaptureCount = cacheFrameCount;
            }
        }
        void TakeSnapshot()
        {
            var fullPath = textureFolder.FullName;
            var fileName = System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".data";
            fileName = fileName.Replace("/", "_");

            UnityEngine.Profiling.Memory.Experimental.MemoryProfiler.TakeSnapshot(fullPath + fileName, null);
        }
        public void SaveToFrameInfo(FrameInfo info, int frameCount)
        {
            if (frameCount <= 0 || info.frameIndex % frameCount != 0)
                return;


            var fileName = System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".png";
            fileName = fileName.Replace("/", "_");
            info.screenShotPath = fileName;

            var command = string.Format("adb shell screencap -p /sdcard/screen.png | adb pull /sdcard/screen.png {0}{1} | adb shell rm /sdcard/screen.png", textureFolder, fileName);
            DoCmd(command);

        }
        public string DoCmd(string args)
        {

            Process cmd = new Process();
            cmd.StartInfo.FileName = "cmd.exe";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.Start();

            cmd.StandardInput.WriteLine(args);
            cmd.StandardInput.Flush();
            cmd.StandardInput.Close();
            cmd.WaitForExit();
            string output = cmd.StandardOutput.ReadToEnd();

            return output;
        }
        public static Dictionary<string, string> outPut = new Dictionary<string, string>();
        public Dictionary<string, string> GetOutPut(string[] allLines, params string[] infos)
        {
            outPut.Clear();
            for (int i = 0; i < allLines.Length; i++)
            {
                var s = allLines[i];

                var matches = Regex.Matches(s, "\\S\\w*");

                for (int j = 0; j < infos.Length; j++)
                {
                    var info = infos[j];

                    if (matches.Count > 1 && matches[0].Value == info)
                    {
                        if (outPut.ContainsKey(info) == false)
                            outPut.Add(info, matches[1].Value);
                    }
                }
            }
            return outPut;
        }
        public static string GetOverviewText(string overview, string match)
        {
            var c = Regex.Matches(overview, match);
            if (c.Count > 0)
            {
                return c[0].Value;
            }
            return null;
        }
        public void BeginSample()
        {
            totalFrameInfo.Clear();
            isRunning = true;

            DoCmd("adb forward tcp:34999 localabstract:Unity-" + packageName);
            ProfilerDriver.connectedProfiler = 1337;
            Log(ProfilerDriver.connectedProfiler.ToString());
            currentTime = 0;
            lastUpdateTime = Time.realtimeSinceStartup;
            EditorApplication.update += UpdateInfo;

            ProfilerDriver.enabled = true;

        }
        public void EndSample()
        {
            isRunning = false;
            currentTime = 0;
            EditorApplication.update -= UpdateInfo;

            ProfilerDriver.enabled = false;

        }
        public void MemoryTakeSample()
        {
            MemoryProfiler.TakeTempSnapshot(OnTakeSnapshotFinish);
        }
        public void OnTakeSnapshotFinish(string info, bool result)
        {
            Log($"info : {info}, result : {result}");
        }

        void OnDisable()
        {
            EndSample();
        }
        public void Log(string context)
        {
            UnityEngine.Debug.Log(context);
        }

        public bool Button(string name)
        {
            return GUILayout.Button(name);
        }
    }
}