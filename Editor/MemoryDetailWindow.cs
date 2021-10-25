using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using AnalyticsInfo = AdbMemoryProfiler.AnalyticsInfo;
public class MemoryDetailWindow : EditorWindow
{
    static AnalyticsInfo analyticsInfo = null;
    static string currentName = "";
    public int maxFrameCount = 100;

    AnimationCurve androidPssCurve;
    AnimationCurve unknownCurve;
    AnimationCurve totalAllocatedCurve;
    AnimationCurve textureMemoryCurve;
    AnimationCurve meshMemoryCurve;
    AnimationCurve monoCurve;

    Vector2 scrollPos;
    public static void InitWindow(AnalyticsInfo info)
    {
        analyticsInfo = info;

        var window = GetWindow<MemoryDetailWindow>();
        window.Init();
        window.titleContent = new GUIContent("MemoryDetailWindow");
        window.Show();
    }
    private void Init()
    {
        currentName = analyticsInfo.name;

        var totalFrameInfo = analyticsInfo.totalFrameInfo;

        androidPssCurve = new AnimationCurve();
        unknownCurve = new AnimationCurve();
        totalAllocatedCurve = new AnimationCurve();
        textureMemoryCurve = new AnimationCurve();
        meshMemoryCurve = new AnimationCurve();
        monoCurve = new AnimationCurve();

        for (int i = 0; i < totalFrameInfo.Count; i++)
        {
            var frameInfo = totalFrameInfo[i];

            androidPssCurve.AddKey(i, frameInfo.totalSize);
            unknownCurve.AddKey(i, frameInfo.unknownSize);
            totalAllocatedCurve.AddKey(i, frameInfo.totalAllocated);
            textureMemoryCurve.AddKey(i, frameInfo.textureMemory);
            meshMemoryCurve.AddKey(i, frameInfo.meshMemory);
            monoCurve.AddKey(i, frameInfo.monoMemory);
        }

        
    }
    private void OnGUI()
    {
        if(analyticsInfo == null)
        {
            Close();
            return;
        }

         int currentCount = maxFrameCount;

        EditorGUILayout.BeginHorizontal();
        currentName = EditorGUILayout.TextField("CurrentName", currentName);
        int.TryParse(EditorGUILayout.TextField("MaxShowCount", maxFrameCount.ToString()), out maxFrameCount);
        EditorGUILayout.EndHorizontal();

        //EditorGUILayout.BeginHorizontal();


        var totalFrameInfo = analyticsInfo.totalFrameInfo;
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        for (int i = totalFrameInfo.Count - 1; i >= 0; i--)
        {
            currentCount--;
            if(currentCount <= 0)
            {
                EditorGUILayout.LabelField(i + " FrameInfo was hidden.");
                break;
                
            }

            var frame = totalFrameInfo[i];

            EditorGUILayout.BeginHorizontal();

            // EditorGUILayout.LabelField(string.Format("unknownSize : {0:N1} Mb, PSS : {1:N1} Mb.", frame.unknownSize,frame.totalSize));

            EditorGUILayout.LabelField(frame.FullToString());
            

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.CurveField("androidPssCurve", androidPssCurve);
        EditorGUILayout.CurveField("unknownCurve", unknownCurve);
        EditorGUILayout.CurveField("totalAllocatedCurve", totalAllocatedCurve);
        EditorGUILayout.CurveField("textureMemoryCurve", textureMemoryCurve);
        EditorGUILayout.CurveField("meshMemoryCurve", meshMemoryCurve);
        EditorGUILayout.CurveField("monoCurve", monoCurve);

        EditorGUILayout.EndScrollView();
        
    }
}
