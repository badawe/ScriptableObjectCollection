using System;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;
using UnityEngine;

namespace BrunoMikoski.ScriptableObjectCollections
{
    public sealed class CreateNewCollectableType : EditorWindow
    {
        private const string LAST_COLLECTABLE_SCRIPT_PATH = "CollectableScriptPathKey";
        private const string LAST_COLLECTABLE_FULL_NAME_KEY = "NewCollectableFullNameKey";

        
        private string newClassName;
        private DefaultAsset targetFolder;
        
        private static Type targetType;
        
        public static string LastGeneratedCollectionScriptPath
        {
            get => EditorPrefs.GetString(LAST_COLLECTABLE_SCRIPT_PATH, String.Empty);
            set => EditorPrefs.SetString(LAST_COLLECTABLE_SCRIPT_PATH, value);
        }
        
        public static string LastCollectionFullName
        {
            get => EditorPrefs.GetString(LAST_COLLECTABLE_FULL_NAME_KEY, String.Empty);
            set => EditorPrefs.SetString(LAST_COLLECTABLE_FULL_NAME_KEY, value);
        }

        public static void Show(Type baseType)
        {
            targetType = baseType;
            CreateNewCollectableType newCollectableWindow = GetWindow<CreateNewCollectableType>("Creating new collectable Fix");
            newCollectableWindow.ShowPopup();
        }
        
        private void OnGUI()
        {
            using (new EditorGUILayout.VerticalScope("Box"))
            {
                using (new EditorGUILayout.VerticalScope("Box"))
                {
                    EditorGUILayout.LabelField("Settings", EditorStyles.foldoutHeader);
                    EditorGUILayout.Space();

                    GUI.SetNextControlName("NameInputField");
                    newClassName = EditorGUILayout.TextField("Type Name", newClassName);
                    if (this.targetFolder == null)
                    {
                        ScriptableObject instance = CreateInstance(targetType);
                        MonoScript path = MonoScript.FromScriptableObject(instance);
                        this.targetFolder =
                            AssetDatabase.LoadAssetAtPath<DefaultAsset>(
                                Path.GetDirectoryName(AssetDatabase.GetAssetPath(path)));
                    }

                    targetFolder = (DefaultAsset) EditorGUILayout.ObjectField("Script Folder",
                        targetFolder, typeof(DefaultAsset), false);

                    if (GUI.GetNameOfFocusedControl() != "NameInputField")
                    {
                        if (!string.IsNullOrEmpty(newClassName))
                            newClassName = newClassName.Sanitize();
                    }
                    
                    using (new EditorGUI.DisabledScope(!AreSettingsValid()))
                    {
                        Color color = GUI.color;
                        GUI.color = Color.green;


                        if (GUILayout.Button("Create"))
                        {
                            string targetNamespace = String.Empty;

                            if (!string.IsNullOrEmpty(targetType.Namespace))
                                targetNamespace = targetType.Namespace;
                            
                            string parentFolder = AssetDatabase.GetAssetPath(targetFolder);
                            CodeGenerationUtility.CreateNewEmptyScript(newClassName,
                                parentFolder, targetNamespace, string.Empty,$"public class {newClassName} : {targetType}",
                                targetNamespace);
                            
                            AssetDatabase.SaveAssets();
                            AssetDatabase.Refresh();
                            
                            if (string.IsNullOrEmpty(targetNamespace))
                                LastCollectionFullName = $"{newClassName}";
                            else
                                LastCollectionFullName = $"{targetNamespace}.{newClassName}";
                            
                            LastGeneratedCollectionScriptPath = Path.Combine(parentFolder, $"{newClassName}.cs");
                            Close();
                        }

                        GUI.color = color;
                    }

                }
            }
        }

        private bool AreSettingsValid()
        {
            if (string.IsNullOrEmpty(newClassName))
                return false;

            if (targetFolder == null)
                return false;

            string parentFolder = AssetDatabase.GetAssetPath(targetFolder);
            if (AssetDatabase.LoadAssetAtPath<MonoScript>(Path.Combine(parentFolder, $"{newClassName}.cs")) != null)
                return false;

            return true;
        }
    }
}
