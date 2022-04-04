using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fizz6.Autofill.Editor
{
    [InitializeOnLoad]
    public static class AutofillManager
    {
        private const string ArrayPropertyPathExtension = ".Array.data";
        
        static AutofillManager()
        {
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
        }

        private static void OnHierarchyChanged()
        {
            if (Application.isPlaying) return;
            
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            var rootGameObjects = prefabStage == null
                ? SceneManager
                    .GetActiveScene()
                    .GetRootGameObjects()
                : new [] { prefabStage.prefabContentsRoot };
            foreach (var rootGameObject in rootGameObjects)
            {
                AutofillHelper(rootGameObject);
            }
        }
        
        private static bool AutofillHelper(GameObject rootGameObject)
        {
            var isDirty = false;
            
            isDirty |= AutofillGameObject(rootGameObject);
            for (var index = 0; index < rootGameObject.transform.childCount; ++index)
            {
                var childTransform = rootGameObject.transform.GetChild(index);
                isDirty |= AutofillHelper(childTransform.gameObject);
            }

            return isDirty;
        }

        private static bool AutofillGameObject(GameObject gameObject)
        {
            var isDirty = false;
            
            var components = gameObject.GetComponents<Component>();
            foreach (var component in components)
            {
                var type = component.GetType();
                var fieldInfos = type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                foreach (var fieldInfo in fieldInfos)
                {
                    var autofillAttribute = fieldInfo.GetCustomAttribute<AutofillAttribute>();
                    if (autofillAttribute == null) continue;
                    isDirty |= Autofill(gameObject, component, fieldInfo, autofillAttribute);
                }
            }

            return isDirty;
        }

        public static bool Autofill(GameObject gameObject, Component component, FieldInfo fieldInfo, AutofillAttribute autofillAttribute, SerializedProperty serializedProperty = null)
        {
            if (Application.isPlaying) return false;
            
            var isDirty = false;

            if (serializedProperty == null)
            {
                var serializedObject = new SerializedObject(component);
                serializedProperty = serializedObject.FindProperty(fieldInfo.Name);
            }
            
            if (!fieldInfo.FieldType.IsArray)
            {
                isDirty = AutofillInstanceSerializedProperty(gameObject, fieldInfo, autofillAttribute, serializedProperty);
            }
            else
            {
                isDirty = AutofillArraySerializedProperty(gameObject, fieldInfo, autofillAttribute, serializedProperty);
            }

            return isDirty;
        }

        private static bool AutofillInstanceSerializedProperty(GameObject gameObject, FieldInfo fieldInfo, AutofillAttribute autofillAttribute, SerializedProperty serializedProperty)
        {
            var value = serializedProperty.objectReferenceValue as Component;
            var validValues = FindValidValues(gameObject, fieldInfo, autofillAttribute);

            if (value != null && validValues.Contains(value)) return false;
            
            serializedProperty.objectReferenceValue = validValues.FirstOrDefault();
            serializedProperty.serializedObject.ApplyModifiedProperties();

            return true;
        }

        private static bool AutofillArraySerializedProperty(GameObject gameObject, FieldInfo fieldInfo, AutofillAttribute autofillAttribute, SerializedProperty serializedProperty)
        {
            var arraySerializedProperty = FindArraySerializedProperty(serializedProperty);
            
            var validValues = FindValidValues(gameObject, fieldInfo, autofillAttribute);
            if (arraySerializedProperty.arraySize == validValues.Count &&
                validValues.Select((component, index) => arraySerializedProperty.GetArrayElementAtIndex(index).objectReferenceValue == validValues[index] ? 0 : 1).Sum() == 0) return false;

            arraySerializedProperty.arraySize = validValues.Count;

            for (var index = 0; index < arraySerializedProperty.arraySize; ++index)
            {
                var childSerializedProperty = arraySerializedProperty.GetArrayElementAtIndex(index);
                var value = validValues[index];
                childSerializedProperty.objectReferenceValue = value;
            }

            arraySerializedProperty.serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(gameObject);

            return true;
        }
        
        private static SerializedProperty FindArraySerializedProperty(SerializedProperty serializedProperty)
        {
            var propertyPath = serializedProperty.propertyPath;
            var arrayPathIndex = propertyPath.LastIndexOf(ArrayPropertyPathExtension, StringComparison.Ordinal);
            if (arrayPathIndex == -1)
            {
                return serializedProperty;
            }
            
            var arrayPropertyPath = propertyPath.Substring(0, arrayPathIndex);
            return serializedProperty.serializedObject.FindProperty(arrayPropertyPath);
        }
        
        private static IReadOnlyList<Component> FindValidValues(GameObject gameObject, FieldInfo fieldInfo, AutofillAttribute autofillAttribute)
        {
            var fieldType = !fieldInfo.FieldType.IsArray
                ? fieldInfo.FieldType
                : fieldInfo.FieldType.GetElementType();
            
            var values = new List<Component>();

            if (autofillAttribute.Targets.HasFlag(AutofillAttribute.Target.Self))
            {
                var selfValues = gameObject.GetComponents(fieldType);
                values.AddRange(selfValues);
            }

            if (autofillAttribute.Targets.HasFlag(AutofillAttribute.Target.Parent))
            {
                var parentTransform = gameObject.transform.parent;
                if (parentTransform != null)
                {
                    var parentGameObject = parentTransform.gameObject;
                    var parentValues = parentGameObject.GetComponentsInParent(fieldType, true);
                    values.AddRange(parentValues);
                }
            }

            if (autofillAttribute.Targets.HasFlag(AutofillAttribute.Target.Children))
            {
                for (int index = 0; index < gameObject.transform.childCount; ++index)
                {
                    var childTransform = gameObject.transform.GetChild(index);
                    var childGameObject = childTransform.gameObject;
                    var childrenValues = childGameObject.GetComponentsInChildren(fieldType, true);
                    values.AddRange(childrenValues);
                }
            }

            return values;
        }
    }
}