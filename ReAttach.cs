using UnityEngine;
using UnityEditor;

public class ReAttach {

	[MenuItem("CONTEXT/PrefabReference/ReAttach")]
	static void ContextStart(MenuCommand mc) {
		PrefabReference pr = (PrefabReference)mc.context;

		var serializedObject = new SerializedObject (pr);
		serializedObject.Update ();

		Transform[] children = pr.transform.GetComponentsInChildren<Transform> ();
		foreach (Transform child in children) {
			if (pr.rootBone.name == child.name) {
				serializedObject.FindProperty ("rootBone").objectReferenceValue = child;
				break;
			}
		}

		SkinnedMeshRenderer[] meshes = pr.transform.GetComponentsInChildren<SkinnedMeshRenderer> ();

		for (int i = 0; i < pr.body.Length; i++) {
			serializedObject.FindProperty ("body").GetArrayElementAtIndex (i).objectReferenceValue = AttachMesh (pr.body[i], meshes);
		}
		for (int i = 0; i < pr.clothes.Length; i++) {
			serializedObject.FindProperty ("clothes").GetArrayElementAtIndex (i).objectReferenceValue = AttachMesh (pr.clothes[i], meshes);
		}
		for (int i = 0; i < pr.hair.Length; i++) {
			serializedObject.FindProperty ("hair").GetArrayElementAtIndex (i).objectReferenceValue = AttachMesh (pr.hair[i], meshes);
		}

		serializedObject.ApplyModifiedProperties ();

	}

	static SkinnedMeshRenderer AttachMesh(SkinnedMeshRenderer skm, SkinnedMeshRenderer[] meshes) {
		foreach(SkinnedMeshRenderer mesh in meshes) {
			if (skm.name == mesh.name) {
				return mesh;
			}
		}
		return null;
	}
}
