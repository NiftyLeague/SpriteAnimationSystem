#if UNITY_EDITOR
/*
using UnityEditor;

[CustomEditor(typeof(CustomizationManager))]
public class InspectorTools : Editor
{
	#region SerializedProperties

	SerializedProperty name1;
	SerializedProperty name2;

	private bool nameGroup1 = false;
	private bool nameGroup2 = false;

	#endregion

	private void OnEnable()
	{
		name1 = serializedObject.FindProperty("name1");
		name2 = serializedObject.FindProperty("name2");
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		EditorGUILayout.PropertyField(name1);
		EditorGUILayout.LabelField("1");
		EditorGUILayout.Space(10);

		nameGroup1 = EditorGUILayout.BeginFoldoutHeaderGroup(nameGroup1, "name1");
		if (nameGroup1)
		{
			EditorGUILayout.PropertyField(name1);
		}
		EditorGUILayout.EndFoldoutHeaderGroup();

		if (true) // todo: Update.
		{
			nameGroup2 = EditorGUILayout.BeginFoldoutHeaderGroup(nameGroup2, "name2");
			if (nameGroup2)
			{
				if (true) // todo: Update.
				{
					EditorGUILayout.PropertyField(name2);
				}
			}
			EditorGUILayout.EndFoldoutHeaderGroup();
		}

		serializedObject.ApplyModifiedProperties();
	}

	void Update()
	{
		
	}
}
*/
#endif //#if UNITY_EDITOR
