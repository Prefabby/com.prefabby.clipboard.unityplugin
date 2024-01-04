/*
	Prefabby Clipboard Unity plugin
    Copyright (C) 2024  Matthias Gall <matt@prefabby.com>

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as
    published by the Free Software Foundation, either version 3 of the
    License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;

namespace PrefabbyClipboard
{

public static class EditorUtils
{

	public const string genericErrorMessage = "An error occurred. Please check your Internet connection and try again. If the error persists, please get in touch at matt@prefabby.com or Twitter @digitalbreed.";

	private static readonly string[] PREFAB_FBX_EXTENSIONS = new string[] { ".prefab", ".fbx" };
	private static readonly string[] MAT_EXTENSION = new string[] { ".mat" };

	public static void GenericError()
	{
		Debug.LogError(genericErrorMessage);
		EditorUtility.DisplayDialog("Error", genericErrorMessage, "OK");
	}

	public static void Error(string message)
	{
		Debug.LogError(message);
		EditorUtility.DisplayDialog("Error", message, "OK");
	}

	public static void CenterEditorWindow(EditorWindow window, float desiredWidth, float desiredHeight)
	{
		Rect rect = EditorGUIUtility.GetMainWindowPosition();
		float width = Mathf.Min(rect.width, desiredWidth);
		float height = Mathf.Min(rect.height, desiredHeight);
		float centerWidth = (rect.width - width) * 0.5f;
		float centerHeight = (rect.height - height) * 0.5f;
		window.position = new Rect(centerWidth, centerHeight, width, height);
	}

	public static string SanitizePath(string path)
	{
		// There should be only one Assets/ directory at the beginning which we can ignore
		if (path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
		{
			path = path[7..];
		}

		// There may be a Resources/ directory at the beginning or somewhere in the middle; we're looking at what follows after it
		while (path.StartsWith("Resources/", StringComparison.OrdinalIgnoreCase) || path.Contains("/Resources/", StringComparison.OrdinalIgnoreCase))
		{
			if (path.StartsWith("Resources", StringComparison.OrdinalIgnoreCase))
			{
				path = path[10..];
			}
			else if (path.Contains("/Resources/", StringComparison.OrdinalIgnoreCase))
			{
				path = path[(path.IndexOf("/Resources/", StringComparison.OrdinalIgnoreCase) + 11)..];
			}
		}

		return path;
	}

	private static (string path, string name) GetPathAndName(string path, string[] extensions)
	{
		path = SanitizePath(path);

		// Remove the expected extensions
		bool extensionFound = false;
		foreach (string extension in extensions)
		{
			if (path.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
			{
				path = path[..^extension.Length];
				extensionFound = true;
				break;
			}
		}
		Assert.IsTrue(extensionFound, $"Path {path} was supposed to end with one of {extensions}!");

		// Separate path and name
		int lastSeparatorIndex = path.LastIndexOf('/');
		string name = path[(lastSeparatorIndex + 1)..];
		path = path[..lastSeparatorIndex];

		return (path, name);
	}

	public static (string path, string name) GetPathAndNameFromPrefab(GameObject go)
	{
		string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);

		return GetPathAndName(path, PREFAB_FBX_EXTENSIONS);
	}

	public static (string path, string name) GetPathAndNameFromMaterial(Material material)
	{
		string path = AssetDatabase.GetAssetPath(material);

		return GetPathAndName(path, MAT_EXTENSION);
	}

	public static List<MaterialReference> FindMaterialChanges(GameObject go, GameObject prefab, PrefabDictionary prefabDictionary)
	{
		Renderer originalRenderer = prefab.GetComponent<Renderer>();
		Renderer userRenderer = go.GetComponent<Renderer>();
		if (originalRenderer == null || userRenderer == null)
		{
			return null;
		}
		Material[] originalMaterials = originalRenderer.sharedMaterials;
		Material[] userMaterials = userRenderer.sharedMaterials;
		List<MaterialReference> changedMaterials = null;
		if (originalMaterials.Length == userMaterials.Length)
		{
			for (int i = 0; i < originalMaterials.Length; ++i)
			{
				string originalMaterialPath = AssetDatabase.GetAssetPath(originalMaterials[i]);
				string userMaterialPath = AssetDatabase.GetAssetPath(userMaterials[i]);

				if (originalMaterialPath != userMaterialPath)
				{
					DebugUtils.Log(DebugContext.General, $"Material change detected!\noriginalMaterialPath = {originalMaterialPath}\nuserMaterialPath = {userMaterialPath}\n");

					(string path, string name) = GetPathAndNameFromMaterial(userMaterials[i]);
					PrefabDictionaryItem prefabDictionaryItem = prefabDictionary.ResolveOrCreate(path, name, PrefabDictionaryItemType.Material);

					changedMaterials ??= new List<MaterialReference>();
					MaterialReference materialReference = new()
					{
						slot = i,
						id = prefabDictionaryItem.id,
						name = name
					};
					changedMaterials.Add(materialReference);
				}
			}
		}
		else
		{
			DebugUtils.Log(DebugContext.General, $"User GO {go.name} has different material count than original prefab {prefab.name}!");
		}
		return changedMaterials;
	}

	public static List<PrefabDictionaryItem> GetAvailablePrefabDictionaryItems(DebugContext debugContext, PrefabDictionary dictionary)
	{
		List<PrefabDictionaryItem> result = new();

		foreach (PrefabDictionaryItem item in dictionary.items)
		{
			DebugUtils.Log(debugContext, $"Trying to find verification prefab {item.verification} in path {item.path}...");

			string type = item.type switch
			{
				PrefabDictionaryItemType.Prefab => "prefab",
				PrefabDictionaryItemType.Material => "material"
			};
			string[] guids = AssetDatabase.FindAssets($"{item.verification} t:{type}");
			foreach (string guid in guids)
			{
				string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
				if (prefabPath.Contains(item.path, StringComparison.OrdinalIgnoreCase))
				{
					DebugUtils.Log(debugContext, $"Verification prefab {item.verification} found!");

					result.Add(item);
					break;
				}
			}
		}

		return result;
	}

	public static string CreatePath(Transform transform, Transform fromParent)
	{
		string result = "";
		while (transform != fromParent)
		{
			var index = transform.GetSiblingIndex();
			var name = transform.name;
			result = NameEncoder.Encode(name, index) + result;

			transform = transform.parent;
		}
		return result;
	}

	public static Transform FindWithPath(Transform root, string path)
	{
		Transform node = root;
		string[] nodeSplits = path.Split("/n[", StringSplitOptions.RemoveEmptyEntries);
		foreach (string nodeSplit in nodeSplits)
		{
			string[] nameSplits = nodeSplit.Split("]i[", StringSplitOptions.RemoveEmptyEntries);
			string indexAsString = nameSplits[1][0..(nameSplits[1].Length - 1)];
			int index = int.Parse(indexAsString);
			Assert.IsTrue(index >= 0 && index < node.childCount);
			Transform child = node.GetChild(index);
			Assert.AreEqual(NameEncoder.Decode(nameSplits[0]), child.name, "unexpected child at index");
			node = child;
		}
		Assert.IsNotNull(node, $"Expected to find a node at path {path}, result should not be null!");
		return node;
	}

}

}
