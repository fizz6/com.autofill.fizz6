using UnityEditor;
using UnityEngine;

namespace Fizz6.Autofill.Editor
{
    [CustomPropertyDrawer(typeof(AutofillAttribute))]
    public class AutofillAttributePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // base.OnGUI(position, property, label);

            if (Application.isPlaying) return;
            
            var component = property.serializedObject.targetObject as Component;
            if (!component) return;
            var gameObject = component.gameObject;
            var autofillAttribute = attribute as AutofillAttribute;
            
            AutofillManager.Autofill(gameObject, component, fieldInfo, autofillAttribute, property);

            if (!fieldInfo.FieldType.IsArray)
            {
                EditorGUI.PropertyField(position, property, label);
            }
            else
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUI.PropertyField(position, property, label);
                }
            }
        }
    }
}