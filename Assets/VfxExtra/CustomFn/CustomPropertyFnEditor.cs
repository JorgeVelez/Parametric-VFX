using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditorInternal;
using System;
using System.IO;
using QuickType;


namespace UnityEditor.VFX.Block
{
    [CustomEditor(typeof(CustomPropertyFn))]
    public class CustomPropertyFnEditor : Editor
    {

        SerializedProperty BlockName;
        SerializedProperty ContextType;
        SerializedProperty CompatibleData;
        SerializedProperty Attributes;
        SerializedProperty Properties;
        SerializedProperty UseTotalTime;
        SerializedProperty UseDeltaTime;
        SerializedProperty UseRandom;
        SerializedProperty PropertyName;
        SerializedProperty SetupFn;
        SerializedProperty XFn;
        SerializedProperty YFn;
        SerializedProperty ZFn;
        SerializedProperty UResolution;
        SerializedProperty VResolution;
        SerializedProperty UMinimum;
        SerializedProperty UMaximum;
        SerializedProperty VMinimum;
        SerializedProperty VMaximum;
        SerializedProperty ScaleFactor;

        ReorderableList attributeList;
        ReorderableList propertiesList;
        
        public List<string> presetNames;
        public List<MathModel> presetParams;
        
        MathmodPresets mathmodPresets;
        public int presetIndex = 0;

        bool dirty;

        private void OnEnable()
        {
            StreamReader reader = new StreamReader("Assets/MathModPresets/mathmodcollection.json"); 
            string jsonString = reader.ReadToEnd();
            reader.Close();
            mathmodPresets = MathmodPresets.FromJson(jsonString);
            presetNames = new List<string> {"(Choose a preset)"};
            presetParams = new List<MathModel>{ null };
            foreach (var p in mathmodPresets.MathModels)
            {
                if (p.Param3D != null && p.Param3D["Name"].Count == 1)
                {
                    presetNames.Add(p.Param3D["Name"][0]);
                    presetParams.Add(p);
                }
            }
            
            Reload();
            
            BlockName = serializedObject.FindProperty("BlockName");
            ContextType = serializedObject.FindProperty("ContextType");
            CompatibleData = serializedObject.FindProperty("CompatibleData");
            Attributes = serializedObject.FindProperty("Attributes");
            Properties = serializedObject.FindProperty("Properties");
            UseTotalTime = serializedObject.FindProperty("UseTotalTime");
            UseDeltaTime = serializedObject.FindProperty("UseDeltaTime");
            UseRandom = serializedObject.FindProperty("UseRandom");
            PropertyName = serializedObject.FindProperty("PropertyName");
            SetupFn = serializedObject.FindProperty("SetupFn");
            XFn = serializedObject.FindProperty("XFn");
            YFn = serializedObject.FindProperty("YFn");
            ZFn = serializedObject.FindProperty("ZFn");
            UResolution = serializedObject.FindProperty("UResolution");
            VResolution = serializedObject.FindProperty("VResolution");
            UMinimum = serializedObject.FindProperty("UMinimum");
            UMaximum = serializedObject.FindProperty("UMaximum");
            VMinimum = serializedObject.FindProperty("VMinimum");
            VMaximum = serializedObject.FindProperty("VMaximum");
            ScaleFactor = serializedObject.FindProperty("ScaleFactor");

            if (Attributes.arraySize == 0)
            {
                AddAttribute("position", 2);
            }

            if (Properties.arraySize == 0)
            {
                AddProperty("voffset", "float");
                AddProperty("uoffset", "float");
                AddProperty("index", "uint");                
            }

            dirty = false;
            serializedObject.Update();

            if (attributeList == null)
            {
                attributeList = new ReorderableList(serializedObject, Attributes, true, true, true, true);
                attributeList.drawHeaderCallback = (r) => { GUI.Label(r, "Attributes"); };
                attributeList.onAddCallback = OnAddAttribute;
                attributeList.onRemoveCallback = OnRemoveAttribute;
                attributeList.drawElementCallback = OnDrawAttribute;
                attributeList.onReorderCallback = OnReorderAttribute;
            }

            if (propertiesList == null)
            {
                propertiesList = new ReorderableList(serializedObject, Properties, true, true, true, true);
                propertiesList.drawHeaderCallback = (r) => { GUI.Label(r, "Properties"); };
                propertiesList.onAddCallback = OnAddProperty;
                propertiesList.onRemoveCallback = OnRemoveProperty;
                propertiesList.drawElementCallback = OnDrawProperty;
                propertiesList.onReorderCallback = OnReorderProperty;
            }
        }

        void OnAddAttribute(ReorderableList list)
        {
            AddAttribute("position", 3); // ReadWrite
        }
        void AddAttribute(string name, int type)
        {
            Attributes.InsertArrayElementAtIndex(0);
            var sp = Attributes.GetArrayElementAtIndex(0);
            sp.FindPropertyRelative("name").stringValue = name;
            sp.FindPropertyRelative("mode").enumValueIndex = type; 
            dirty = true;
            Apply();
        }
        void OnRemoveAttribute(ReorderableList list)
        {
            if (list.index != -1)
                Attributes.DeleteArrayElementAtIndex(list.index);
            dirty = true;
            Apply();
        }

        void OnReorderAttribute(ReorderableList list)
        {
            dirty = true;
            Apply();
        }

        void OnDrawAttribute(Rect rect, int index, bool isActive, bool isFocused)
        {
            var sp = Attributes.GetArrayElementAtIndex(index);
            rect.yMin += 2;
            var nameRect = rect;
            float split = rect.width / 2;

            nameRect.width = split - 40;
            string name = sp.FindPropertyRelative("name").stringValue;
            int attribvalue = EditorGUI.Popup(nameRect, Array.IndexOf(VFXAttribute.All, name), VFXAttribute.All);
            sp.FindPropertyRelative("name").stringValue = VFXAttribute.All[attribvalue];

            var modeRect = rect;
            modeRect.xMin = split;
            var mode = sp.FindPropertyRelative("mode");
            var value = EditorGUI.EnumFlagsField(modeRect, (VFXAttributeMode)mode.intValue);
            mode.intValue = (int)System.Convert.ChangeType(value, typeof(VFXAttributeMode));

            if(GUI.changed)
                Apply();
        }

        void OnAddProperty(ReorderableList list)
        {
            AddProperty("newProperty", "float");
        }
        
        void AddProperty(string name, string type)
        {
            Properties.InsertArrayElementAtIndex(0);
            var sp = Properties.GetArrayElementAtIndex(0);
            sp.FindPropertyRelative("name").stringValue = name;
            sp.FindPropertyRelative("type").stringValue = type;
            dirty = true;
            Apply();
        }

        void OnRemoveProperty(ReorderableList list)
        {
            if (list.index != -1)
                Properties.DeleteArrayElementAtIndex(list.index);
            dirty = true;
            Apply();
        }
        
        void OnReorderProperty(ReorderableList list)
        {
            dirty = true;
            Apply();
        }

        void OnDrawProperty(Rect rect, int index, bool isActive, bool isFocused)
        {
            var sp = Properties.GetArrayElementAtIndex(index);
            rect.yMin += 2;
            rect.height = 16;
            var nameRect = rect;
            float split = rect.width / 2 ;

            nameRect.width = split - 40;
            string name = sp.FindPropertyRelative("name").stringValue;
            sp.FindPropertyRelative("name").stringValue = EditorGUI.TextField(nameRect, name);

            var modeRect = rect;
            modeRect.xMin = split;

            var knownTypes = CustomPropertyFn.knownTypes.Keys.ToArray();
            var type = sp.FindPropertyRelative("type");
            var value = EditorGUI.Popup(modeRect, Array.IndexOf(knownTypes, type.stringValue), knownTypes);
            type.stringValue = knownTypes[value];

            if(GUI.changed)
                Apply();
        }

        public override void OnInspectorGUI()
        {
            
            presetIndex = EditorGUILayout.Popup(presetIndex, presetNames.ToArray());
            if (presetIndex > 0)
            {
                string setupFn = "";
                if (presetParams[presetIndex].Param3D.ContainsKey("Const"))
                    setupFn += BuildConsts(presetParams[presetIndex].Param3D["Const"]);
                if (presetParams[presetIndex].Param3D.ContainsKey("Funct"))
                    setupFn += BuildFuncts(presetParams[presetIndex].Param3D["Funct"]);
                SetupFn.stringValue = setupFn;
                UMaximum.stringValue = ConvertMathModFn(presetParams[presetIndex].Param3D["Umax"][0]);
                UMinimum.stringValue = ConvertMathModFn(presetParams[presetIndex].Param3D["Umin"][0]);
                VMaximum.stringValue = ConvertMathModFn(presetParams[presetIndex].Param3D["Vmax"][0]);
                VMinimum.stringValue = ConvertMathModFn(presetParams[presetIndex].Param3D["Vmin"][0]);
                XFn.stringValue = ConvertMathModFn(presetParams[presetIndex].Param3D["Fx"][0]);
                YFn.stringValue = ConvertMathModFn(presetParams[presetIndex].Param3D["Fy"][0]);
                ZFn.stringValue = ConvertMathModFn(presetParams[presetIndex].Param3D["Fz"][0]);
                presetIndex = 0;
                dirty = true;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(BlockName);
            EditorGUILayout.PropertyField(ContextType);
            EditorGUILayout.PropertyField(CompatibleData);
            attributeList.DoLayoutList();
            propertiesList.DoLayoutList();
            EditorGUILayout.PropertyField(UseTotalTime);
            EditorGUILayout.PropertyField(UseDeltaTime);
            EditorGUILayout.PropertyField(UseRandom);
            EditorGUILayout.PropertyField(UResolution);
            EditorGUILayout.PropertyField(VResolution);
            EditorGUILayout.PropertyField(UMinimum);
            EditorGUILayout.PropertyField(UMaximum);
            EditorGUILayout.PropertyField(VMinimum);
            EditorGUILayout.PropertyField(VMaximum);
            EditorGUILayout.PropertyField(PropertyName);
            EditorGUILayout.PropertyField(SetupFn);
            EditorGUILayout.PropertyField(XFn);
            EditorGUILayout.PropertyField(YFn);
            EditorGUILayout.PropertyField(ZFn);
            EditorGUILayout.PropertyField(ScaleFactor);
            
            if (EditorGUI.EndChangeCheck())
                dirty = true;

            using (new EditorGUI.DisabledGroupScope(!dirty))
            {
                if (GUILayout.Button("Apply"))
                {
                    Apply();
                }
            }

        }

        static string ConvertMathModFn(string input)
        {
            // Pick some random values until I figure out what these are used for.
            if (input == "umin" || input == "vmin")
                return "0";
            else if (input == "umax" || input == "vmax")
                return "100";
            else
            {
                // Fix some of the oddities in the Mathmod format
                return input.Replace("pi", "PI")
                    .Replace("DOTSYMBOL", ".");
            }
        }

        static string BuildFuncts(List<string> inputs)
        {
            // Takes a list of Mathmod functions declarations, reformats them as HLSL functions
            // and returns as a multiline string
            string output = "\n";
            foreach (var part in inputs)
            {
                string converted = "#define " + ConvertMathModFn(part) + "\n\n";
                converted = converted.Replace("=", "(u, v, t) ");
                output += converted;
            }

            return output;
        }
        
        static string BuildConsts(List<string> inputs)
        {
            // Takes a list of Mathmod const declarations, adds type, semicolon
            // and returns as a multiline string
            string output = "";
            foreach (var part in inputs)
            {
                output += "float " + ConvertMathModFn(part) + ";\n";
            }

            return output;
        }

        void Apply()
        {
            serializedObject.ApplyModifiedProperties();
            dirty = false;   
            serializedObject.Update();
            Reload();
        }


        void Reload()
        {
            (serializedObject.targetObject as VFXBlock).Invalidate(VFXModel.InvalidationCause.kSettingChanged);
        }

    }

}

