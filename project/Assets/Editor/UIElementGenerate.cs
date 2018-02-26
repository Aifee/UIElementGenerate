using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class UIElementGenerate : EditorWindow
{
    private class UIElementTreeItem
    {
        public GameObject gameObject;
        public Transform transform;
        public Transform parent;
        public List<Transform> childs;
        public int Depth;
        public string Name;
        public bool IsOpen;
        public bool IsGenerate;

        /// <summary>
        /// 组件简称
        /// </summary>
        private static string[] elementAbbreviation = new string[] { "img", "btn", "Tex", "Tog", "sli", "scr", "dro", "input" };

        private static Dictionary<string, Type> abbTypes = new Dictionary<string, Type>();

        /// <summary>
        /// 组件全称
        /// </summary>
        private static string[] elementFullname = new string[] { "text", "image", "rawimage", "button", "toggle", "slider", "scrollbar", "dropdown", "inputfield", "scrollview" };

        private static Dictionary<string, Type> fullTypes = new Dictionary<string, Type>();

        public string ToString(bool gt)
        {
            Transform trans = transform;
            string childPath = Name;
            while (trans.parent != null && trans.parent != UIElementGenerate.OptionView)
            {
                childPath = trans.parent.name + "/" + childPath;
                trans = trans.parent;
            }
            if (gt)
            {
                return string.Format("self.{0} = self:GetChildObj(\"{1}\");", Name, childPath);
            }
            MonoBehaviour[] behaviours = gameObject.GetComponents<MonoBehaviour>();
            if (behaviours.Length > 0)
            {
                Type type = GetType(Name);
                if (type != default(Type))
                {
                    for (int i = 0; i < behaviours.Length; i++)
                    {
                        if (behaviours[i].GetType() == type)
                        {
                            string componentStr = type.ToString();
                            return string.Format("self.{0} = self:GetChildCompByObj(\"{1}\",\"{2}\");", Name, childPath, componentStr);
                        }
                    }
                }
            }
            return string.Format("self.{0} = self:GetChildObj(\"{1}\");", Name, childPath);
        }

        private static Type GetType(string elementName)
        {
            string newName = elementName.ToLower();
            if (abbTypes == null || abbTypes.Count <= 0)
            {
                abbTypes = new Dictionary<string, Type>();
                abbTypes.Add("tex", typeof(Text));
                abbTypes.Add("img", typeof(Image));
                abbTypes.Add("rawimg", typeof(RawImage));
                abbTypes.Add("btn", typeof(Button));
                abbTypes.Add("tog", typeof(Toggle));
                abbTypes.Add("sli", typeof(Slider));
                abbTypes.Add("scb", typeof(Scrollbar));
                abbTypes.Add("dro", typeof(Dropdown));
                abbTypes.Add("input", typeof(InputField));
                abbTypes.Add("scr", typeof(ScrollRect));
                abbTypes.Add("grid", typeof(GridLayoutGroup));
                abbTypes.Add("hor", typeof(HorizontalLayoutGroup));
                abbTypes.Add("ver", typeof(VerticalLayoutGroup));
            }
            foreach (KeyValuePair<string, Type> kvp in abbTypes)
            {
                if (newName.Contains(kvp.Key))
                {
                    return kvp.Value;
                }
            }
            if (fullTypes == null || fullTypes.Count <= 0)
            {
                fullTypes = new Dictionary<string, Type>();
                fullTypes.Add("text", typeof(Text));
                fullTypes.Add("image", typeof(Image));
                fullTypes.Add("rawimage", typeof(RawImage));
                fullTypes.Add("button", typeof(Button));
                fullTypes.Add("toggle", typeof(Toggle));
                fullTypes.Add("slider", typeof(Slider));
                fullTypes.Add("scrollbar", typeof(Scrollbar));
                fullTypes.Add("dropdown", typeof(Dropdown));
                fullTypes.Add("inputfield", typeof(InputField));
                fullTypes.Add("scrollrect", typeof(ScrollRect));
                fullTypes.Add("gridgroup", typeof(GridLayoutGroup));
                fullTypes.Add("horizontalgroup", typeof(HorizontalLayoutGroup));
                fullTypes.Add("verticalgroup", typeof(VerticalLayoutGroup));
            }

            foreach (KeyValuePair<string, Type> kvp in fullTypes)
            {
                if (newName.Contains(kvp.Key))
                {
                    return kvp.Value;
                }
            }
            return default(Type);
        }
    }

    /// <summary>
    /// 当前选择的View根节点
    /// </summary>
    public static Transform OptionView;

    /// <summary>
    /// 要到处的View信息
    /// </summary>
    private string exportInfo;

    /// <summary>
    /// 左侧树列表显示的宽度
    /// </summary>
    private float ListWidth = 250;

    private bool GenerateTransform = false;
    private string synopsis = "UI Generate to lua tool is created by liuaf\n" +
        "1.勾选要导出的节点\n" +
        "2.注意命名(支持简写与全称，想要导出的节点以组件形式获取去的，名字要符合规范)";

    [MenuItem("GameObject/UI Element Exproter", false, 11)]
    public static void Open()
    {
        UIElementGenerate window = EditorWindow.CreateInstance<UIElementGenerate>();
        window.Show();
    }

    [MenuItem("GameObject/UI Element Exproter", true, 11)]
    public static bool ValidateOpen()
    {
        bool validate = false;
        if (Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<Canvas>() != null)
        {
            validate = true;
        }
        return validate;
    }

    private void OnEnable()
    {
        GetTreeList();
    }

    private void OnDestroy()
    {
        trees.Clear();
        OptionView = null;
    }

    private Vector2 treeScroll;
    private Vector2 infoScroll;
    private Dictionary<Transform, UIElementTreeItem> trees = new Dictionary<Transform, UIElementTreeItem>();

    private void OnGUI()
    {
        EditorGUILayout.BeginVertical();
        {
            EditorGUILayout.HelpBox(synopsis, MessageType.Info, true);
            EditorGUILayout.BeginHorizontal();
            {
                DrawLeft();
                DrawRight();
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawLeft()
    {
        EditorGUILayout.BeginVertical("box", GUILayout.Width(ListWidth));
        {
            GUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("All", GUILayout.Width(40)))
                {
                    AllElementUnifyOption(true);
                }
                if (GUILayout.Button("None", GUILayout.Width(40)))
                {
                    AllElementUnifyOption(false);
                }
                if (GUILayout.Button("Generate"))
                {
                    GenerateInfo(GenerateTransform);
                }
                GenerateTransform = GUILayout.Toggle(GenerateTransform, "GT", GUILayout.Width(40));
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);
            treeScroll = EditorGUILayout.BeginScrollView(treeScroll, GUILayout.Width(ListWidth));
            {
                if (OptionView != null)
                {
                    UIElementTreeItem item = trees[OptionView];
                    if (item != null)
                    {
                        ShowObject(item);
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }
        EditorGUILayout.EndVertical();
    }

    private void ShowObject(UIElementTreeItem item)
    {
        GUILayout.BeginVertical();
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(item.Depth * 20 + 5);
                item.IsOpen = Foldout(item, item.IsOpen);
            }
            GUILayout.EndHorizontal();
            if (item.IsOpen)
            {
                if (item.childs != null && item.childs.Count > 0)
                {
                    for (int i = 0; i < item.childs.Count; i++)
                    {
                        if (trees.ContainsKey(item.childs[i]))
                        {
                            UIElementTreeItem child = trees[item.childs[i]];
                            if (child != null)
                            {
                                ShowObject(child);
                            }
                        }
                    }
                }
            }
        }
        GUILayout.EndVertical();
    }

    /// <summary>
    /// 自定义折叠页
    /// 如果当前操作的组件没有子对象，则折叠功能取消，点击后相应选中或未选中状态
    /// </summary>
    /// <param name="item"> 操作的对象 </param>
    /// <param name="display"> 是否折叠 </param>
    /// <returns></returns>
    private bool Foldout(UIElementTreeItem item, bool display)
    {
        bool isFoldout = item.childs != null && item.childs.Count > 0;

        ///Box样式
        GUIStyle style = new GUIStyle("ShurikenModuleTitle");
        style.font = new GUIStyle(EditorStyles.label).font;
        style.border = new RectOffset(15, 7, 4, 4);
        style.fixedHeight = 22;
        style.contentOffset = new Vector2(isFoldout ? 35f : 20f, -2f);
        ///绘制box
        Rect rect = GUILayoutUtility.GetRect(16f, 22f, style);
        GUI.Box(rect, item.Name, style);

        if (isFoldout)
        {
            ///每一项是否能显示的Toggle
            Rect selectedRect = new Rect(rect.x + 18f, rect.y, 13f, 13f);
            item.IsGenerate = GUI.Toggle(selectedRect, item.IsGenerate, "");

            Event e = Event.current;
            Rect toggleRect = new Rect(rect.x + 4f, rect.y + 2f, 13f, 13f);
            if (e.type == EventType.Repaint)
            {
                EditorStyles.foldout.Draw(toggleRect, false, false, display, false);
            }

            if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
            {
                display = !display;
                e.Use();
            }
        }
        else
        {
            Event e = Event.current;
            Rect toggleRect = new Rect(rect.x + 4f, rect.y, 13f, 13f);
            if (e.type == EventType.Repaint)
            {
                EditorStyles.toggle.Draw(toggleRect, false, false, item.IsGenerate, false);
            }

            if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
            {
                item.IsGenerate = !item.IsGenerate;
                e.Use();
            }
        }
        return display;
    }

    private void DrawRight()
    {
        EditorGUILayout.BeginVertical("box");
        {
            GUILayout.Space(5);
            infoScroll = EditorGUILayout.BeginScrollView(infoScroll);
            {
                EditorGUILayout.SelectableLabel(exportInfo, GUILayout.Height(position.height - 25));
            }
            EditorGUILayout.EndScrollView();
        }
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 获取选择的View根节点列表
    /// </summary>
    private void GetTreeList()
    {
        OptionView = Selection.activeTransform;
        if (OptionView == null)
            return;
        if (trees == null || trees.Count <= 0)
        {
            trees = new Dictionary<Transform, UIElementTreeItem>();
            RectTransform[] rts = OptionView.GetComponentsInChildren<RectTransform>();
            foreach (RectTransform rt in rts)
            {
                UIElementTreeItem item = new UIElementTreeItem();
                item.gameObject = rt.gameObject;
                item.transform = rt.transform;
                item.Name = item.gameObject.name;
                item.Depth = 0;
                if (item.transform != OptionView)
                {
                    item.parent = item.transform.parent;
                }
                if (item.parent != null)
                {
                    Transform parent = item.transform;
                    while (parent != OptionView)
                    {
                        item.Depth++;
                        parent = parent.parent;
                    }
                }
                if (item.transform.childCount > 0)
                {
                    item.childs = new List<Transform>();
                    for (int i = 0; i < item.transform.childCount; i++)
                    {
                        item.childs.Add(item.transform.GetChild(i));
                    }
                }
                item.IsOpen = true;
                trees.Add(item.transform, item);
            }
        }
    }

    /// <summary>
    /// 所有组件是否选中统一操作
    /// </summary>
    /// <param name="isSelected"></param>
    private void AllElementUnifyOption(bool isSelected)
    {
        if (trees.Count > 0)
        {
            foreach (UIElementTreeItem item in trees.Values)
            {
                item.IsGenerate = isSelected;
            }
        }
    }

    /// <summary>
    /// 生成信息
    /// </summary>
    /// <param name="gt"></param>
    private void GenerateInfo(bool gt)
    {
        StringBuilder sb = new StringBuilder();
        if (trees.Count > 0)
        {
            foreach (UIElementTreeItem item in trees.Values)
            {
                if (item.IsGenerate)
                {
                    sb.AppendLine(item.ToString(gt));
                }
            }
        }
        exportInfo = sb.ToString();
    }
}