using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace ubco.hcilab.roadmap.editor
{
    [CustomPropertyDrawer(typeof(PlaceableContainer))]
    public class PlaceableContainerEditor: PropertyDrawer
    {
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.LabelField(new Rect(rect.x, rect.y, 55, EditorGUIUtility.singleLineHeight), "Identifier:");
            EditorGUI.PropertyField(new Rect(rect.x + 60, rect.y, 200, EditorGUIUtility.singleLineHeight),
                                    property.FindPropertyRelative("identifier"), GUIContent.none);
            EditorGUI.LabelField(new Rect(rect.x + 270, rect.y, 50, EditorGUIUtility.singleLineHeight), "Object:");
            EditorGUI.PropertyField(new Rect(rect.x + 330, rect.y, 150, EditorGUIUtility.singleLineHeight),
                                    property.FindPropertyRelative("prefab"), GUIContent.none);
        }
    }
}
