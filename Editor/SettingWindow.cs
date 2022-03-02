using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;

namespace AdbProfiler
{
    public class SettingWindow : EditorWindow
    {
        public static void ShowWindow()
        {
            var window = GetWindow<SettingWindow>();
            window.titleContent = new GUIContent("SettingWindow");
            
            window.Show();
        }
        public static bool CaptureMemoryOverview
        {
            get{
                return EditorPrefs.GetBool("AdbMemoryProfiler_CaptureMemoryOverview", true);
                
            }
            set{
                EditorPrefs.SetBool("AdbMemoryProfiler_CaptureMemoryOverview", value);
            }
        }
        public static bool CaptureTemperature
        {
            get{
                return EditorPrefs.GetBool("AdbMemoryProfiler_CaptureTemperature", true);
                
            }
            set{
                EditorPrefs.SetBool("AdbMemoryProfiler_CaptureTemperature", value);
            }
        }
        public static bool CaptureGLMtrack
        {
            get{
                return EditorPrefs.GetBool("AdbMemoryProfiler_CaptureGLMtrack", true);
                
            }
            set{
                EditorPrefs.SetBool("AdbMemoryProfiler_CaptureGLMtrack", value);
            }
        }
        public static bool CaptureAndroidMemory
        {
            get{
                return EditorPrefs.GetBool("AdbMemoryProfiler_CaptureAndroidMemory", true);
                
            }
            set{
                EditorPrefs.SetBool("AdbMemoryProfiler_CaptureAndroidMemory", value);
            }
        }
        public static int GcRecordMinValue
        {
            get{
                return EditorPrefs.GetInt("AdbMemoryProfiler_GCRecordMinValue", 1024);
            }
            set{
                EditorPrefs.SetInt("AdbMemoryProfiler_GCRecordMinValue", value);
            }
        }

        public static bool CaptureCodeAnalytics
        {
            get{
                return EditorPrefs.GetBool("AdbMemoryProfiler_CaptureCodeAnalytics", true);
                
            }
            set{
                EditorPrefs.SetBool("AdbMemoryProfiler_CaptureCodeAnalytics", value);
            }
        }

        public static bool CaptureFPS
        {
            get{
                return EditorPrefs.GetBool("AdbMemoryProfiler_FPS", true);
                
            }
            set{
                EditorPrefs.SetBool("AdbMemoryProfiler_FPS", value);
            }
        }
        
        private void OnGUI()
        {
            // EditorGUILayout.BeginHorizontal();
            

            // EditorGUILayout.EndHorizontal();
            CaptureMemoryOverview = EditorGUILayout.Toggle("CaptureUnityOverview", CaptureMemoryOverview);
            CaptureAndroidMemory = EditorGUILayout.Toggle("CaptureAndroidMemory", CaptureAndroidMemory);
            CaptureTemperature = EditorGUILayout.Toggle("CaptureTemperature", CaptureTemperature);
            CaptureGLMtrack = EditorGUILayout.Toggle("CaptureGLMtrack", CaptureGLMtrack);
            CaptureCodeAnalytics = EditorGUILayout.Toggle("CaptureCodeAnalytics", CaptureCodeAnalytics);
            GcRecordMinValue = EditorGUILayout.IntField("GcRecordMinValue:(Size:B)", GcRecordMinValue);
            CaptureFPS = EditorGUILayout.Toggle("CaptureFPS", CaptureFPS);
           
        }
        void OnLostFocus()
        {
            Close();
        }
    }
}
