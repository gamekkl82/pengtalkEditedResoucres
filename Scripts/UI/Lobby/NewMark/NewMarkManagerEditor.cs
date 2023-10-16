#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(NewMarkManager))]
public class NewMarkManagerEditor : Editor
{
    SerializedProperty newMarkType;
    SerializedProperty index;
    SerializedProperty teacherType;

    public override void OnInspectorGUI()
    {
        if (NewMarkManager.Instance == null)
            return;

        serializedObject.Update();

        newMarkType = serializedObject.FindProperty("testNewMarkType");
        index = serializedObject.FindProperty("testIndex");
        teacherType = serializedObject.FindProperty("testTeacherType");

        EditorGUILayout.PropertyField(newMarkType);
        NewMarkManager.NewMarkType _type = (NewMarkManager.NewMarkType)newMarkType.enumValueIndex;

        if (_type == NewMarkManager.NewMarkType.NewMark_MyBag || _type == NewMarkManager.NewMarkType.NewMark_Notice || _type == NewMarkManager.NewMarkType.NewMark_Stamp)
        {

        }
        else
        {
            if(_type != NewMarkManager.NewMarkType.NewMark_Ranking)
                EditorGUILayout.PropertyField(index);

            if(_type == NewMarkManager.NewMarkType.NewMark_TeacherItem)
                EditorGUILayout.PropertyField(teacherType);

            if (GUILayout.Button("Add", GUILayout.Width(100), GUILayout.Height(50)))
            {
                NewMarkManager.Instance.AddNewMark((NewMarkManager.NewMarkType)newMarkType.enumValueIndex, index.intValue.ToString());
            }

            if (GUILayout.Button("Remove", GUILayout.Width(100), GUILayout.Height(50)))
            {
                NewMarkManager.Instance.RemoveNewMark((NewMarkManager.NewMarkType)newMarkType.enumValueIndex, index.intValue.ToString());

            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif