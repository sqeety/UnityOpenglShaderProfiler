using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using Debug = System.Diagnostics.Debug;

public class OpenglShaderProfileWindow : EditorWindow
{
    private static readonly Type ShaderUtilType = typeof(ShaderUtil);
    private static OpenglShaderProfileWindow activeWindow;
    private bool cursor0Move;
    private Rect cursorChangeRect0;
    private string filePath;
    private float firstX;
    private readonly List<ShaderInfo> infos = new List<ShaderInfo>();
    private float pastTime;
    [SerializeField] private Vector2 scrollPos;
    private float secondX;
    [SerializeField] private Shader shader;
    private bool shaderComplete;
    private int showShaderInfo;
    private bool start;
    private string tempPath;
    private float windowWidth;

    [MenuItem("Window/Sqeety/ShaderProfile")]
    private static void CreateWindow()
    {
        OpenglShaderProfileWindow window = GetWindow<OpenglShaderProfileWindow>();
        window.Show();
        activeWindow = window;
    }

    public void DealShader()
    {
        infos.Clear();
        string content = File.ReadAllText(filePath);
        //Debug.Log(File.ReadAllText(filePath));
        Regex regex =
            new Regex("((Keywords)|(No keywords))(.|\n)*?-- Fragment shader");
        MatchCollection ma = regex.Matches(content);
        for (int i = 0; i < ma.Count; i++)
        {
            string text = ma[i].ToString();
            ShaderInfo si = new ShaderInfo();
            int startIndex = text.IndexOf(": ", StringComparison.Ordinal);
            int endNum = text.IndexOf("\n", StringComparison.Ordinal);
            if (startIndex != -1 && startIndex < endNum)
            {
                startIndex += 2;
                string result = text.Substring(startIndex, endNum - startIndex);
                string[] allKeys = result.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries);
                si.keyWords = allKeys[0];
                for (int j = 1; j < allKeys.Length; j++)
                    si.keyWords += "\n" + allKeys[j];
                //si.keyWords = si.keyWords.Replace(" ", "\n");
            }
            else
            {
                si.keyWords = "NoKeyWords";
            }
            startIndex = text.IndexOf("#ifdef VERTEX", StringComparison.Ordinal) + 13;
            endNum = text.IndexOf("#endif", StringComparison.Ordinal);
            si.vert = text.Substring(startIndex, endNum - startIndex);

            startIndex = text.IndexOf("#ifdef FRAGMENT", StringComparison.Ordinal) + 16;
            endNum = text.LastIndexOf("#endif", StringComparison.Ordinal);
            si.frag = text.Substring(startIndex, endNum - startIndex);
            if (!si.frag.Contains("float;\n"))
            {
                string first = si.frag.Substring(0, si.frag.IndexOf('\n') + 1);
                startIndex = si.frag.IndexOf("precision", StringComparison.Ordinal);
                string third = si.frag.Substring(startIndex);
                si.frag = first + "precision highp float;\n" + third;
            }
            //Debug.Log(si.keyWords);
            //Debug.Log(si.vert);
            //Debug.Log(si.frag);
            infos.Add(si);
        }
        ProfileShader();
        showShaderInfo = 1;
    }

    public static void DrawHorizontalLine(Color color, Vector2 start, Vector2 end, float lineWidth = 1.0f)
    {
        if (Event.current.type != EventType.Repaint)
            return;
        Color defaultColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(start.x, start.y, end.x - start.x, lineWidth), EditorGUIUtility.whiteTexture);
        GUI.color = defaultColor;
    }

    public static void DrawRect(Color color, Rect rect, float lineWidth)
    {
        if (Event.current.type != EventType.Repaint)
            return;
        Color color1 = GUI.color;
        GUI.color *= color;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, lineWidth), EditorGUIUtility.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - lineWidth, rect.width, lineWidth), EditorGUIUtility.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y + 1f, lineWidth, rect.height - 2f * lineWidth),
            EditorGUIUtility.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMax - lineWidth, rect.y + 1f, lineWidth, rect.height - 2f * lineWidth),
            EditorGUIUtility.whiteTexture);
        GUI.color = color1;
    }

    public static void DrawVerticalLine(Color color, Vector2 start, Vector2 end, float lineWidth = 1.0f)
    {
        if (Event.current.type != EventType.Repaint)
            return;
        Color defaultColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(start.x, start.y, lineWidth, end.y - start.y), EditorGUIUtility.whiteTexture);
        GUI.color = defaultColor;
    }

    private void OnDestroy()
    {
        activeWindow = null;
    }

    private void OnEnable()
    {
        windowWidth = position.width;
        showShaderInfo = 0;
        titleContent = new GUIContent("Shader3.0", EditorGUIUtility.FindTexture("Shader Icon"));
        tempPath = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/')) + "/Temp/";
        scrollPos = Vector2.zero;
        cursorChangeRect0 = new Rect();
        firstX = 140;
    }

    private void OnGUI()
    {
        if (Math.Abs(windowWidth - position.width) > 0.01f)
            OnWidthChanged();
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        shader = EditorGUILayout.ObjectField("SelectShader", shader, typeof(Shader), false) as Shader;
        if (EditorGUI.EndChangeCheck())
            shaderComplete = false;
        EditorGUILayout.EndHorizontal();

        if (shaderComplete)
        {
        }

        if (shader && !start && !shaderComplete)
        {
            Debug.Assert(shader != null, "shader != null");
            showShaderInfo = 0;
            filePath = tempPath + "Compiled-" + shader.name.Replace('/', '-') + ".shader";
            if (File.Exists(filePath))
            {
                DealShader();
                shaderComplete = true;
                start = false;
                EditorGUILayout.EndScrollView();
                return;
            }
            OpenCompiledShader(shader, 3, 736973, false);
            start = true;
            pastTime = 0;
            shaderComplete = false;
        }
        Event curEvent = Event.current;
        GUILayout.Label("   ");
        Rect startRect = GUILayoutUtility.GetLastRect();
        if (showShaderInfo == 0)
        {
            EditorGUILayout.EndScrollView();
            return;
        }
        if (showShaderInfo == 1)
            if (curEvent.type == EventType.layout)
            {
                showShaderInfo = 2;
            }
            else
            {
                EditorGUILayout.EndScrollView();
                return;
            }

        EditorGUI.indentLevel++;
        List<Rect> keyRects = new List<Rect>();
        for (int i = 0; i < infos.Count; i++)
        {
            //UnityEngine.Debug.Log(EditorStyles.label.CalcSize(new GUIContent(infos[i].keyWords)).x);
            EditorGUILayout.LabelField(infos[i].keyWords,
                GUILayout.Height(EditorStyles.label.CalcSize(new GUIContent(infos[i].keyWords)).y));
            //if (infos[i].opened)
            //{
            //    EditorGUI.indentLevel++;
            //    EditorGUILayout.LabelField("vertProfile", infos[i].vertResult);
            //    EditorGUILayout.LabelField("fragProfile", infos[i].fragResult);
            //    EditorGUI.indentLevel--;
            //}
            Rect keyRect = GUILayoutUtility.GetLastRect();
            keyRects.Add(keyRect);
            DrawHorizontalLine(Color.black, new Vector2(keyRect.x, keyRect.y),
                new Vector2(keyRect.x + keyRect.width, keyRect.y));
        }
        Rect endRect = GUILayoutUtility.GetLastRect();
        Rect outRect = new Rect(startRect.x, startRect.y, startRect.width, endRect.y + endRect.height - startRect.y);
        EditorGUI.indentLevel--;
        DrawVerticalLine(Color.black, new Vector2(firstX, startRect.y),
            new Vector2(firstX, outRect.y + outRect.height));
        cursorChangeRect0.x = firstX;
        cursorChangeRect0.y = startRect.y;
        cursorChangeRect0.width = 5;
        cursorChangeRect0.height = outRect.height;
        if (Event.current.type == EventType.Repaint)
            EditorGUIUtility.AddCursorRect(cursorChangeRect0, MouseCursor.ResizeHorizontal);
        if (curEvent.type == EventType.mouseDown && cursorChangeRect0.Contains(curEvent.mousePosition))
            cursor0Move = true;
        if (curEvent.type == EventType.MouseUp)
            cursor0Move = false;
        if (cursor0Move)
        {
            firstX = curEvent.mousePosition.x - cursorChangeRect0.width / 2.0f;
            if (firstX < 140)
                firstX = 140;
            else if (firstX + 200 > position.width)
                firstX = position.width - 200;
            Repaint();
        }
        secondX = (outRect.width - firstX) / 2 + firstX;
        DrawVerticalLine(Color.black, new Vector2(secondX, startRect.y),
            new Vector2(secondX, outRect.y + outRect.height));
        DrawRect(Color.black, outRect, 1);
        EditorGUI.LabelField(new Rect(startRect.x + 16, startRect.y, 140, startRect.height), "KeyWords");
        float oneCenter = firstX / 2 + secondX / 2;
        EditorGUI.LabelField(
            new Rect(oneCenter - EditorStyles.label.CalcSize(new GUIContent("vert")).x / 2, startRect.y,
                secondX - firstX, startRect.height), "vert");
        float twoCenter = secondX / 2 + outRect.width / 2 + outRect.x / 2;
        EditorGUI.LabelField(
            new Rect(twoCenter - EditorStyles.label.CalcSize(new GUIContent("frag")).x / 2, startRect.y,
                outRect.width + outRect.x - secondX, startRect.height), "frag");
        for (int i = 0; i < infos.Count; i++)
        {
            EditorGUI.LabelField(
                new Rect(oneCenter - EditorStyles.label.CalcSize(new GUIContent(infos[i].vertResult)).x / 2,
                    keyRects[i].y + keyRects[i].height / 2 - 8,
                    secondX - firstX, startRect.height), infos[i].vertResult);

            EditorGUI.LabelField(
                new Rect(twoCenter - EditorStyles.label.CalcSize(new GUIContent(infos[i].fragResult)).x / 2,
                    keyRects[i].y + keyRects[i].height / 2 - 8,
                    secondX - firstX, startRect.height), infos[i].fragResult);
        }

        Color defaultColor = GUI.color;
        GUI.color = Color.green;
        EditorGUILayout.LabelField("0:Average 1:branch instructions  \n2:other instructions" +
                                   "3:Worst  \n4:branch instructions 5:other instructions", GUILayout.Height(46));
        GUI.color = defaultColor;
        EditorGUILayout.EndScrollView();
    }

    private void OnWidthChanged()
    {
        //cursorChangeRect0.height = position.height;
    }

    [OnOpenAsset(-1)]
    public static bool OpenAsset(int instanceID, int line)
    {
        Type t = EditorUtility.InstanceIDToObject(instanceID).GetType();
        if (t == typeof(Shader))
            if (activeWindow)
            {
                activeWindow.shader = EditorUtility.InstanceIDToObject(instanceID) as Shader;
                activeWindow.Repaint();
            }
        return false;
    }

    private static void OpenCompiledShader(Shader s, int mode, int customPlatformsMask, bool includeAllVariants)
    {
        ShaderUtilType.InvokeMember("OpenCompiledShader",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null,
            new object[] {s, mode, customPlatformsMask, includeAllVariants});
    }

    private void ProfileShader()
    {
        string exePath = Application.dataPath + "/Editor/SharderProfile/PowerVR/GLSLESCompiler_Series6.exe";
        string vertPath = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/')) + "/Temp/vert.txt";
        string vertProfilePath = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/')) +
                                 "/Temp/vertProfile.txt";
        string vertProfileOutPath = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/')) +
                                    "/Temp/vertProfile.disasm";
        if (File.Exists(vertPath))
        {
            FileStream file = File.Create(vertPath);
            file.Close();
        }
        if (File.Exists(vertProfilePath))
        {
            FileStream file = File.Create(vertProfilePath);
            file.Close();
        }
        string fragPath = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/')) + "/Temp/frag.txt";
        if (File.Exists(fragPath))
        {
            FileStream file = File.Create(fragPath);
            file.Close();
        }
        string fragProfilePath = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/')) +
                                 "/Temp/fragProfile.txt";
        string fragProfileOutPath = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/')) +
                                    "/Temp/fragProfile.disasm";
        if (File.Exists(fragProfilePath))
        {
            FileStream file = File.Create(fragProfilePath);
            file.Close();
        }
        for (int i = 0; i < infos.Count; i++)
        {
            ShaderInfo info = infos[i];
            File.WriteAllBytes(vertPath, Encoding.Default.GetBytes(info.vert));
            Process myprocess = new Process();
            ProcessStartInfo startInfo =
                new ProcessStartInfo(exePath, vertPath + " " + vertProfilePath + " -v -profile -nowarn");
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.CreateNoWindow = true;
            myprocess.StartInfo = startInfo;
            myprocess.StartInfo.UseShellExecute = false;
            myprocess.Start();
            Thread.Sleep(30);
            while (string.IsNullOrEmpty(info.vertResult))
                try
                {
                    info.vertResult = File.ReadAllLines(vertProfileOutPath)[4];
                }
                catch (Exception)
                {
                    Thread.Sleep(30);
                }

            File.WriteAllBytes(fragPath, Encoding.Default.GetBytes(info.frag));
            myprocess = new Process();
            startInfo = new ProcessStartInfo(exePath, fragPath + " " + fragProfilePath + " -f -profile -nowarn");
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.CreateNoWindow = true;
            myprocess.StartInfo = startInfo;
            myprocess.StartInfo.UseShellExecute = false;
            myprocess.Start();
            Thread.Sleep(30);
            while (string.IsNullOrEmpty(info.fragResult))
                try
                {
                    info.fragResult = File.ReadAllLines(fragProfileOutPath)[4];
                }
                catch (Exception)
                {
                    Thread.Sleep(30);
                }
        }
    }

    private void Update()
    {
        if (start)
        {
            pastTime += Time.deltaTime;
            if (pastTime > 3.0f)
            {
                if (File.Exists(filePath))
                {
                    DealShader();
                    start = false;
                    shaderComplete = true;
                }
                pastTime -= 1.0f;
            }
        }
    }

    public class ShaderInfo
    {
        public string frag;
        public string fragResult;
        public string keyWords;
        public bool opened = true;
        public string vert;
        public string vertResult;
    }
}