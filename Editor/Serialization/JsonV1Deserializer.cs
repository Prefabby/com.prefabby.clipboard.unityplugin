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

using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;

namespace PrefabbyClipboard
{

class JsonV1Deserializer : IDeserializer
{

	private readonly SerializedTree tree;
	private readonly PrefabDictionary dictionary;
	private readonly Dictionary<string, SerializedGameObject> idToSerializedGameObject = new();

	public JsonV1Deserializer(SerializedTree tree, PrefabDictionary dictionary)
	{
		Assert.IsNotNull(tree);
		Assert.IsNotNull(dictionary);

		this.tree = tree;
		this.dictionary = dictionary;
	}

	public string GetRepresentation()
	{
		return "JsonV1";
	}

	public Transform Deserialize(Transform parent)
	{
		tree.ids ??= new();

		foreach (SerializedGameObject sgo in tree.gameObjects)
		{
			idToSerializedGameObject.Add(sgo.id, sgo);
		}

		return Deserialize(idToSerializedGameObject[tree.root], parent).transform;
	}

	private GameObject Deserialize(SerializedGameObject serializedGameObject, Transform parent)
	{
		DebugUtils.Log(DebugContext.Deserialization, $"Deserializing serialized GO {serializedGameObject.name}");

		GameObject go = null;
		Transform useParent = parent;

		if (serializedGameObject.path != null)
		{
			// The path might point to an existing prefab part which is modified,
			// or to a parent for a newly created prefab
			Transform transformAtPath = EditorUtils.FindWithPath(parent, serializedGameObject.path);
			if (transformAtPath != null && serializedGameObject.prefab == null)
			{
				go = transformAtPath.gameObject;
			}
			else
			{
				useParent = transformAtPath;
			}
		}
		else if (serializedGameObject.siblingIndex is int siblingIndex)
		{
			// The siblingIndex might point to an existing prefab part which is modified
			if (siblingIndex < useParent.childCount)
			{
				go = useParent.GetChild(siblingIndex).gameObject;
			}
		}

		if (go == null)
		{
			if (serializedGameObject.prefab != null)
			{
				PrefabDictionaryItem item = dictionary.GetItemById(serializedGameObject.prefab.id);
				if (item != null)
				{
					go = InstantiateGameObjectFromPath(item.path, serializedGameObject.prefab.name, serializedGameObject.prefab.type);
				}

				if (go == null)
				{
					EditorUtils.Error($"Failed to instantiate serialized game object {serializedGameObject.prefab.name}");
					return null;
				}
			}
			else
			{
				go = new();
			}
		}

		tree.ids.Add(go, serializedGameObject.id);

		if (serializedGameObject.status != null)
		{
			if (serializedGameObject.status == SerializedGameObjectStatus.Active)
			{
				go.SetActive(true);
			}
			else if (serializedGameObject.status == SerializedGameObjectStatus.Inactive)
			{
				go.SetActive(false);
			}
		}
		if (serializedGameObject.name != null)
		{
			go.name = serializedGameObject.name;
		}
		if (serializedGameObject.position != null)
		{
			go.transform.localPosition = serializedGameObject.position.ToVector3();
		}
		if (serializedGameObject.rotation != null)
		{
			go.transform.localRotation = Quaternion.Euler(serializedGameObject.rotation.ToVector3());
		}
		if (serializedGameObject.scale != null)
		{
			go.transform.localScale = serializedGameObject.scale.ToVector3();
		}

		// We may be changing properties of a prefab child, in that case reparenting is not necessary and would cause an error message.
		if (go.transform.parent == null)
		{
			go.transform.SetParent(useParent, false);
		}

		ApplyMaterialChanges(go, serializedGameObject, dictionary);

		if (serializedGameObject.children != null)
		{
			foreach (string childId in serializedGameObject.children)
			{
				Deserialize(idToSerializedGameObject[childId], go.transform);
			}
		}

		return go;
	}

	private void ApplyMaterialChanges(GameObject go, SerializedGameObject serializedGameObject, PrefabDictionary prefabDictionary)
	{
		if (go == null || serializedGameObject.materials == null || serializedGameObject.materials.Count == 0)
		{
			return;
		}
		if (!go.TryGetComponent<Renderer>(out var renderer))
		{
			DebugUtils.Log(DebugContext.Deserialization, $"GO {go.name} has no renderer, but there are material changes tracked!");
			return;
		}

		Material[] newMaterials = (Material[]) renderer.sharedMaterials.Clone();
		foreach (MaterialReference materialReference in serializedGameObject.materials)
		{
			newMaterials[materialReference.slot] = FindMaterial(prefabDictionary, materialReference.id, materialReference.name);
		}
		renderer.sharedMaterials = newMaterials;
	}

	public Material FindMaterial(PrefabDictionary prefabDictionary, string id, string name)
	{
		PrefabDictionaryItem item = prefabDictionary.GetItemById(id);
		Assert.IsNotNull(item);

		Material material = InstantiateMaterialFromPath(item.path, name);
		return material;
	}

	private GameObject InstantiateGameObjectFromPath(string path, string name, string type)
	{
		string unityType = MapTypeToFilter(type);
		string extension = MapTypeToExtension(type);

		DebugUtils.Log(DebugContext.Deserialization, $"Trying to find: {path} {name} t:{unityType}");
		string[] guids = AssetDatabase.FindAssets($"{name} t:{unityType}");
		DebugUtils.Log(DebugContext.Deserialization, $"Found {guids.Length} candidates: {guids}");

		for (int i = 0; i < guids.Length; ++i)
		{
			string prefabPath = AssetDatabase.GUIDToAssetPath(guids[i]);
			DebugUtils.Log(DebugContext.Deserialization, $"Checking if prefab path {prefabPath} contains path {path} and ends with /{name}.{extension}...");

			// Make sure the found prefab has the requested name/extension,
			// see material method below for a more specific example
			if (prefabPath.Contains(path, StringComparison.OrdinalIgnoreCase) && prefabPath.EndsWith($"/{name}.{extension}", StringComparison.OrdinalIgnoreCase))
			{
				DebugUtils.Log(DebugContext.Deserialization, $"Found {unityType} {name} at: {prefabPath}");

				GameObject prefab = (GameObject) AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject));
				PrefabAssetType prefabAssetType = PrefabUtility.GetPrefabAssetType(prefab);

				if (type == null ||
					(type == "Prefab" && prefabAssetType == PrefabAssetType.Regular) ||
					(type == "Model" && prefabAssetType == PrefabAssetType.Model) ||
					(type == "Variant" && prefabAssetType == PrefabAssetType.Variant))
				{
					return (GameObject) PrefabUtility.InstantiatePrefab(prefab);
				}
			}
		}
		return null;
	}

	private Material InstantiateMaterialFromPath(string path, string name)
	{
		DebugUtils.Log(DebugContext.Deserialization, $"Trying to find: {name} t:Material");
		string[] guids = AssetDatabase.FindAssets($"{name} t:Material");

		for (int i = 0; i < guids.Length; ++i)
		{
			string prefabPath = AssetDatabase.GUIDToAssetPath(guids[i]);

			// When searching for "Wall_01_A", this method may find e.g. "Wall_01_Alt_01_Triplanar", too.
			// Therefore we're making sure the file ends with the requested name.
			// To avoid that phone.prefab finds gramophone.prefab, too, we also include the leading slash.
			if (prefabPath.Contains(path, StringComparison.OrdinalIgnoreCase) && prefabPath.EndsWith($"/{name}.mat", StringComparison.OrdinalIgnoreCase))
			{
				DebugUtils.Log(DebugContext.Deserialization, $"Found material {name} at: {prefabPath}");
				return (Material) AssetDatabase.LoadAssetAtPath(prefabPath, typeof(Material));
			}
		}
		return null;
	}

	private string MapTypeToFilter(string type)
	{
		return type switch
		{
			null => "prefab",
			"Model" => "model",
			"Prefab" or "Variant" => "prefab",
			_ => "",
		};
	}

	private string MapTypeToExtension(string type)
	{
		return type switch
		{
			null => "prefab",
			"Model" => "fbx",
			"Prefab" or "Variant" => "prefab",
			_ => "",
		};
	}

}

}
