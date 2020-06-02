using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;
using UnityEngine.Serialization;

namespace XNodeEditor {
    public enum NoodlePath { Curvy, Straight, Angled, ShaderLab }
    public enum NoodleStroke { Full, Dashed }

    public interface INodePreferenceSettings
    {
        Color32 GridBGColor { get; set; }
        Color32 GridLineColor { get; set; }

        //float ZoomOutLimit { get; set; }
        float MaxZoom { get; set; }
        float MinZoom { get; set; }

        Color32 HighlightColor { get; set; }
        bool GridSnap { get; set; }
        bool AutoSave { get; set; }

        bool DragToCreate { get; set; }
        bool ZoomToMouse { get; set; }

        bool PortTooltips { get; set; }

        NoodleStroke NoodleStroke { get; set; }
        NoodlePath NoodlePath { get; set; }

        bool GetPortColor( Type type, out Color color );
        void SetPortColor( Type type, Color color );

        bool GetSelectedPortColor( Type type, out Color color );
        void SetSelectedPortColor( Type type, Color color );
    }

    public static class INodePreferencesExtensions
    {
        private static Dictionary<INodePreferenceSettings, Texture2D> s_GridTextures = new Dictionary<INodePreferenceSettings, Texture2D>();
        private static Dictionary<INodePreferenceSettings, Texture2D> s_CrossTextures = new Dictionary<INodePreferenceSettings, Texture2D>();

        public static Texture2D GetGridTexture( this INodePreferenceSettings settings )
        {
            Texture2D texture;
            if ( !s_GridTextures.TryGetValue( settings, out texture ) )
                s_GridTextures[settings] = texture = NodeEditorResources.GenerateGridTexture( settings.GridLineColor, settings.GridBGColor );
            return texture;
        }

        public static void ClearGridTexture( this INodePreferenceSettings settings )
        {
            s_GridTextures.Remove( settings );
        }

        public static Texture2D GetCrossTexture( this INodePreferenceSettings settings )
        {
            Texture2D texture;
            if ( !s_CrossTextures.TryGetValue( settings, out texture ) )
                s_CrossTextures[settings] = texture = NodeEditorResources.GenerateCrossTexture( settings.GridLineColor );
            return texture;
        }

        public static void ClearCrossTexture( this INodePreferenceSettings settings )
        {
            s_CrossTextures.Remove( settings );
        }
    }

    public static class NodeEditorPreferences {

		/// <summary> The last editor we checked. This should be the one we modify </summary>
		private static XNodeEditor.NodeGraphEditor lastEditor;
		/// <summary> The last key we checked. This should be the one we modify </summary>
		private static string lastKey = "xNode.Settings";

		private static Dictionary<Type, Color> typeColors = new Dictionary<Type, Color>();
		private static Dictionary<Type, Color> typeSelectedColors = new Dictionary<Type, Color>();
		private static Dictionary<string, Settings> settings = new Dictionary<string, Settings>();

        [System.Serializable]
        public class Settings : INodePreferenceSettings, ISerializationCallbackReceiver
        {
            [SerializeField] private Color32 _gridLineColor = new Color(0.45f, 0.45f, 0.45f);
            public Color32 GridLineColor { get { return _gridLineColor; } set { _gridLineColor = value; this.ClearGridTexture(); this.ClearCrossTexture(); } }

            [SerializeField] private Color32 _gridBgColor = new Color(0.18f, 0.18f, 0.18f);
            public Color32 GridBGColor { get { return _gridBgColor; } set { _gridBgColor = value; this.ClearGridTexture(); } }

            [Obsolete("Use maxZoom instead")]
            public float zoomOutLimit { get { return MaxZoom; } set { MaxZoom = value; } }

            [UnityEngine.Serialization.FormerlySerializedAs("zoomOutLimit")]
            [SerializeField] private float maxZoom = 5f;
            public float MaxZoom { get { return maxZoom; } set { maxZoom = value; } }

            [SerializeField] private float minZoom = 1f;
            public float MinZoom { get { return minZoom; } set { minZoom = value; } }

            [SerializeField] private Color32 highlightColor = new Color32(255, 255, 255, 255);
            public Color32 HighlightColor { get { return highlightColor; } set { highlightColor = value; } }

            [SerializeField] private bool gridSnap = true;
            public bool GridSnap { get { return gridSnap; } set { gridSnap = value; } }

            [SerializeField] private bool autoSave = true;
            public bool AutoSave { get { return autoSave; } set { autoSave = value; } }

            [SerializeField] private bool dragToCreate = true;
            public bool DragToCreate { get { return dragToCreate; } set { dragToCreate = value; } }

            [SerializeField] private bool zoomToMouse = true;
            public bool ZoomToMouse { get { return zoomToMouse; } set { zoomToMouse = value; } }

            [SerializeField] private bool portTooltips = true;
            public bool PortTooltips { get { return portTooltips; } set { portTooltips = value; } }

            [SerializeField] private string typeColorsData = "";
            [SerializeField] private string typeSelectedColorsData = "";

            [NonSerialized] private Dictionary<string, Color> typeColors = null;
            [NonSerialized] private Dictionary<string, Color> typeSelectedColors = null;

            private Dictionary<string, Color> TypeColors
            {
                get
                {
                    if ( typeColors == null )
                    {
				        // Deserialize typeColorsData
                        typeColors = new Dictionary<string, Color>();
                        string[] data = typeColorsData.Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries );
                        for ( int i = 0; i < data.Length; i += 2 )
                        {
                            Color col;
                            if ( ColorUtility.TryParseHtmlString( "#" + data[i + 1], out col ) )
                            {
                                typeColors.Add( data[i], col );
                            }
                        }
                    }
                    return typeColors;
                }
                set
                {
                }
            }
            
            private Dictionary<string, Color> TypeSelectedColors
            {
                get
                {
                    if ( typeSelectedColors == null )
                    {
                        // Deserialize typeSelectedColorsData
                        typeSelectedColors = new Dictionary<string, Color>();
                        string[] data = typeSelectedColorsData.Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries );
                        for ( int i = 0; i < data.Length; i += 2 )
                        {
                            Color col;
                            if ( ColorUtility.TryParseHtmlString( "#" + data[i + 1], out col ) )
                            {
                                typeSelectedColors.Add( data[i], col );
                            }
                        }
                    }
                    return typeSelectedColors;
                }
                set
                {
                }
            }

            public bool GetPortColor( Type type, out Color color )
            {
                return TypeColors.TryGetValue( NodeEditorUtilities.PrettyName( type ), out color );
            }

            public void SetPortColor( Type type, Color color )
            {
                TypeColors[NodeEditorUtilities.PrettyName( type )] = color;
            }

            public bool GetSelectedPortColor( Type type, out Color color )
            {
                return TypeSelectedColors.TryGetValue( NodeEditorUtilities.PrettyName( type ), out color );
            }

            public void SetSelectedPortColor( Type type, Color color )
            {
                TypeSelectedColors[NodeEditorUtilities.PrettyName( type )] = color;
            }

            [FormerlySerializedAs("noodleType")]
            [SerializeField] private NoodlePath noodlePath = NoodlePath.Curvy;
            public NoodlePath NoodlePath { get { return noodlePath; } set { noodlePath = value; } }

            [SerializeField] private NoodleStroke noodleStroke = NoodleStroke.Full;
            public NoodleStroke NoodleStroke { get { return noodleStroke; } set { noodleStroke = value; } }

			public void OnAfterDeserialize()
			{
			}

			public void OnBeforeSerialize()
			{
                // Serialize typeColors
                TypeColors.Any();
				typeColorsData = "";
				foreach ( var item in typeColors )
				{
					typeColorsData += item.Key + "," + ColorUtility.ToHtmlStringRGB( item.Value ) + ",";
				}

                // Serialize typeSelectedColors
                TypeSelectedColors.Any();
                typeSelectedColorsData = "";
				foreach ( var item in typeSelectedColors )
				{
					typeSelectedColorsData += item.Key + "," + ColorUtility.ToHtmlStringRGB( item.Value ) + ",";
				}
			}
		}

        private static Func<string, INodePreferenceSettings> GetSettingsOverride = GetSettingsInternal;
        public static void SetSettingsOverride( Func<string, INodePreferenceSettings> settingsOverride )
        {
            GetSettingsOverride = settingsOverride;
        }

        /// <summary> Get settings of current active editor </summary>
        public static INodePreferenceSettings GetSettings() {
            return GetSettingsInternal();
        }

        private static INodePreferenceSettings GetSettingsInternal( string key ) {
            if ( !settings.ContainsKey( lastKey ) ) VerifyLoaded();
            return settings[lastKey];
        }

        /// <summary> Get settings of current active editor </summary>
        private static INodePreferenceSettings GetSettingsInternal() {
            if (XNodeEditor.NodeEditorWindow.current == null) return new Settings();

            if (lastEditor != XNodeEditor.NodeEditorWindow.current.graphEditor) {
                object[] attribs = XNodeEditor.NodeEditorWindow.current.graphEditor.GetType().GetCustomAttributes(typeof(XNodeEditor.NodeGraphEditor.CustomNodeGraphEditorAttribute), true);
                if (attribs.Length == 1) {
                    XNodeEditor.NodeGraphEditor.CustomNodeGraphEditorAttribute attrib = attribs[0] as XNodeEditor.NodeGraphEditor.CustomNodeGraphEditorAttribute;
                    lastEditor = XNodeEditor.NodeEditorWindow.current.graphEditor;
                    lastKey = attrib.editorPrefsKey;
                } else return null;
            }
            return GetSettingsOverride( lastKey );
        }

#if UNITY_2019_1_OR_NEWER
        [SettingsProvider]
        public static SettingsProvider CreateXNodeSettingsProvider() {
            if ( GetSettingsOverride != GetSettingsInternal )
                return null;
            SettingsProvider provider = new SettingsProvider("Preferences/Node Editor", SettingsScope.User) {
                guiHandler = (searchContext) => { XNodeEditor.NodeEditorPreferences.PreferencesGUI(); },
                keywords = new HashSet<string>(new [] { "xNode", "node", "editor", "graph", "connections", "noodles", "ports" })
            };
            return provider;
        }
#endif

#if !UNITY_2019_1_OR_NEWER
        [PreferenceItem("Node Editor")]
#endif
        private static void PreferencesGUI() {
            VerifyLoaded();
            Settings settings = NodeEditorPreferences.settings[lastKey];

            if (GUILayout.Button(new GUIContent("Documentation", "https://github.com/Siccity/xNode/wiki"), GUILayout.Width(100))) Application.OpenURL("https://github.com/Siccity/xNode/wiki");
            EditorGUILayout.Space();

            NodeSettingsGUI(lastKey, settings);
            GridSettingsGUI(lastKey, settings);
            SystemSettingsGUI(lastKey, settings);
            TypeColorsGUI(lastKey, settings);
            if (GUILayout.Button(new GUIContent("Set Default", "Reset all values to default"), GUILayout.Width(120))) {
                ResetPrefs();
            }
        }

        private static void GridSettingsGUI(string key, Settings settings) {
            //Label
            EditorGUILayout.LabelField("Grid", EditorStyles.boldLabel);
            settings.GridSnap = EditorGUILayout.Toggle(new GUIContent("Snap", "Hold CTRL in editor to invert"), settings.GridSnap);
            settings.ZoomToMouse = EditorGUILayout.Toggle(new GUIContent("Zoom to Mouse", "Zooms towards mouse position"), settings.ZoomToMouse);
            EditorGUILayout.LabelField("Zoom");
            EditorGUI.indentLevel++;
            settings.MaxZoom = EditorGUILayout.FloatField(new GUIContent("Max", "Upper limit to zoom"), settings.MaxZoom);
            settings.MinZoom = EditorGUILayout.FloatField(new GUIContent("Min", "Lower limit to zoom"), settings.MinZoom);
            EditorGUI.indentLevel--;
            settings.GridLineColor = EditorGUILayout.ColorField("Color", settings.GridLineColor);
            settings.GridBGColor = EditorGUILayout.ColorField(" ", settings.GridBGColor);
            if (GUI.changed) {
                SavePrefs(key, settings);

                NodeEditorWindow.RepaintAll();
            }
            EditorGUILayout.Space();
        }

        private static void SystemSettingsGUI(string key, Settings settings) {
            //Label
            EditorGUILayout.LabelField("System", EditorStyles.boldLabel);
            settings.AutoSave = EditorGUILayout.Toggle(new GUIContent("Autosave", "Disable for better editor performance"), settings.AutoSave);
            if (GUI.changed) SavePrefs(key, settings);
            EditorGUILayout.Space();
        }

        private static void NodeSettingsGUI(string key, Settings settings) {
            //Label
            EditorGUILayout.LabelField("Node", EditorStyles.boldLabel);
            settings.HighlightColor = EditorGUILayout.ColorField("Selection", settings.HighlightColor);
            settings.NoodlePath = (NoodlePath) EditorGUILayout.EnumPopup("Noodle path", (Enum) settings.NoodlePath);
            settings.NoodleStroke = (NoodleStroke) EditorGUILayout.EnumPopup("Noodle stroke", (Enum) settings.NoodleStroke);
            settings.PortTooltips = EditorGUILayout.Toggle("Port Tooltips", settings.PortTooltips);
            settings.DragToCreate = EditorGUILayout.Toggle(new GUIContent("Drag to Create", "Drag a port connection anywhere on the grid to create and connect a node"), settings.DragToCreate);
            if (GUI.changed) {
                SavePrefs(key, settings);
                NodeEditorWindow.RepaintAll();
            }
            EditorGUILayout.Space();
        }

		private static void TypeColorsGUI( string key, Settings settings )
		{
			//Label
			EditorGUILayout.LabelField( "Types", EditorStyles.boldLabel );

			//Display type colors. Save them if they are edited by the user
			var keys = typeColors.Keys.ToArray();
			foreach ( var type in keys )
			{
				string typeColorKey = NodeEditorUtilities.PrettyName( type );
				Color col;
				typeColors.TryGetValue( type, out col );
				Color selectedCol = col;
				typeSelectedColors.TryGetValue( type, out selectedCol );
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PrefixLabel( typeColorKey );
				EditorGUILayout.LabelField( "Color", GUILayout.Width( 70 ) );
				col = EditorGUILayout.ColorField( col );
				EditorGUILayout.LabelField( "Selected", GUILayout.Width( 70 ) );
				selectedCol = EditorGUILayout.ColorField( selectedCol );
				EditorGUILayout.EndHorizontal();
				if ( EditorGUI.EndChangeCheck() )
				{
					typeColors[type] = col;
                    settings.SetPortColor( type, col );
					
					typeSelectedColors[type] = selectedCol;
                    settings.SetSelectedPortColor( type, selectedCol );

					SavePrefs( key, settings );
					NodeEditorWindow.RepaintAll();
				}
			}
		}

        /// <summary> Load prefs if they exist. Create if they don't </summary>
        private static Settings LoadPrefs() {
            // Create settings if it doesn't exist
            if (!EditorPrefs.HasKey(lastKey)) {
                if (lastEditor != null) EditorPrefs.SetString(lastKey, JsonUtility.ToJson(lastEditor.GetDefaultPreferences()));
                else EditorPrefs.SetString(lastKey, JsonUtility.ToJson(new Settings()));
            }
            return JsonUtility.FromJson<Settings>(EditorPrefs.GetString(lastKey));
        }

        /// <summary> Delete all prefs </summary>
        public static void ResetPrefs() {
            if (EditorPrefs.HasKey(lastKey)) EditorPrefs.DeleteKey(lastKey);
            if (settings.ContainsKey(lastKey)) settings.Remove(lastKey);
            typeColors = new Dictionary<Type, Color>();
            VerifyLoaded();
            NodeEditorWindow.RepaintAll();
        }

        /// <summary> Save preferences in EditorPrefs </summary>
        private static void SavePrefs(string key, Settings settings) {
            EditorPrefs.SetString(key, JsonUtility.ToJson(settings));
        }

        /// <summary> Check if we have loaded settings for given key. If not, load them </summary>
        private static void VerifyLoaded() {
            if (!settings.ContainsKey(lastKey)) settings.Add(lastKey, LoadPrefs());
        }

		/// <summary> Return color based on type </summary>
		public static Color GetTypeColor( System.Type type )
		{
			VerifyLoaded();
			if ( type == null ) return Color.gray;
			Color col;
			Color selectedCol;
			if ( !typeColors.TryGetValue( type, out col ) ) {
				string typeName = type.PrettyName();

                if ( settings[lastKey].GetPortColor( type, out col ) ) {
                    typeColors.Add( type, col );
                    if ( settings[lastKey].GetSelectedPortColor( type, out selectedCol ) )
                        typeSelectedColors.Add( type, selectedCol );
                    else
                        typeSelectedColors[type] = col;
                }
                else
                {
                    DefaultNoodleColorAttribute defaultColorsAttribute = System.ComponentModel.TypeDescriptor.GetAttributes( type ).OfType<DefaultNoodleColorAttribute>().FirstOrDefault();
                    if ( defaultColorsAttribute == null ) {
#if UNITY_5_4_OR_NEWER
                        UnityEngine.Random.InitState( typeName.GetHashCode() );
#else
                        UnityEngine.Random.seed = typeName.GetHashCode();
#endif

                        selectedCol = new Color( UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value );
                        col = new Color( selectedCol.r * 0.6f, selectedCol.g * 0.6f, selectedCol.b * 0.6f );
                        typeSelectedColors[type] = selectedCol;
                        typeColors.Add( type, col );
                    }
                    else {
                        selectedCol = defaultColorsAttribute.SelectedColor;
                        col = defaultColorsAttribute.Color;
                        typeSelectedColors[type] = selectedCol;
                        typeColors.Add( type, col );
                    }
                }
			}
			return col;
		}

		/// <summary> Return color based on type </summary>
		public static Color GetSelectedTypeColor( System.Type type )
		{
			VerifyLoaded();
			if ( type == null ) return Color.gray;
            Color col;
            Color selectedCol;
            if ( !typeSelectedColors.TryGetValue( type, out selectedCol ) ) {
				string typeName = type.PrettyName();

                if ( settings[lastKey].GetSelectedPortColor( type, out selectedCol ) ) {
                    typeSelectedColors.Add( type, selectedCol );
                    if ( settings[lastKey].GetPortColor( type, out col ) )
                        typeColors[type] = col;
                    else
                        typeColors[type] = selectedCol;
                }
                else {
                    DefaultNoodleColorAttribute defaultColorsAttribute = System.ComponentModel.TypeDescriptor.GetAttributes( type ).OfType<DefaultNoodleColorAttribute>().FirstOrDefault();
                    if ( defaultColorsAttribute == null ) {
#if UNITY_5_4_OR_NEWER
                        UnityEngine.Random.InitState( typeName.GetHashCode() );
#else
                        UnityEngine.Random.seed = typeName.GetHashCode();
#endif

                        selectedCol = new Color( UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value );
                        col = new Color( selectedCol.r * 0.6f, selectedCol.g * 0.6f, selectedCol.b * 0.6f );
                        typeSelectedColors[type] = selectedCol;
                        typeColors.Add( type, col );
                    }
                    else {
                        selectedCol = defaultColorsAttribute.SelectedColor;
                        col = defaultColorsAttribute.Color;
                        typeSelectedColors[type] = selectedCol;
                        typeColors.Add( type, col );
                    }
                }
			}
			return selectedCol;
		}
	}
}