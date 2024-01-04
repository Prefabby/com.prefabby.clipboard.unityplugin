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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

using UnityEngine;
using UnityEditor;
using Unity.EditorCoroutines.Editor;

using Newtonsoft.Json;
using System.Linq;

namespace PrefabbyClipboard
{

public class PrefabbyClipboardWindow : EditorWindow
{

	private Vector2 scrollPosition;
	private Texture2D logo;
	private GUIContent titleBarContent;
	private Vector2 titleBarLogoSize = new Vector2(32, 24);
	private Vector2 iconButtonSize = new Vector2(16, 16);
	private Texture2D refreshIcon;
	private Texture2D searchIcon;
	private Texture2D favoriteIcon;
	private Texture2D settingsIcon;
	private bool showSearch = false;
	private string searchText = "";
	private bool showFavorites = false;
	private bool isDragging = false;

	private List<ClipboardEntryMetadata> clipboardEntries;
	private List<ClipboardEntryMetadata> filteredClipboardEntries;
	private Dictionary<string, Texture2D> previewImages;

	[MenuItem("Tools/Prefabby Clipboard")]
	public static PrefabbyClipboardWindow Init()
	{
		return GetWindow(typeof(PrefabbyClipboardWindow), false, "Prefabby Clipboard") as PrefabbyClipboardWindow;
	}

	public PrefabbyClipboardWindow()
	{
	}

	void OnEnable()
	{
		logo = Resources.Load("PrefabbyTitle") as Texture2D;
		titleBarContent = new GUIContent($"<size=10><b>Prefabby Clipboard</b> v{Constants.version} by @digitalbreed</size>", logo);

		refreshIcon = EditorGUIUtility.FindTexture("d_Refresh@2x");
		searchIcon = EditorGUIUtility.FindTexture("Search Icon");
		favoriteIcon = EditorGUIUtility.FindTexture("Favorite@2x");
		settingsIcon = EditorGUIUtility.FindTexture("d_Settings@2x");

		Settings.Load();

		LoadClipboardEntries();

		DebugUtils.Log(DebugContext.General, $"Prefabby Clipboard v{Constants.version} enabled using data directory {Settings.Data.contentDirectory}");
	}

	public void OnGUI()
	{
		GUI.InitializeIfNecessary();

		RenderTitleBar();
		RenderSearch();
		RenderEntries();
		HandleDragAndDrop();
	}

	private void RenderTitleBar()
	{
		EditorGUILayout.BeginHorizontal(GUI.titleBarStyle, GUILayout.Height(34));

		EditorGUIUtility.SetIconSize(titleBarLogoSize);
		if (GUILayout.Button(titleBarContent, GUI.flatButtonStyle))
		{
			Application.OpenURL("https://prefabby.com?ref=clipboard-unity");
		}

		GUILayout.FlexibleSpace();

		EditorGUIUtility.SetIconSize(iconButtonSize);

		EditorGUILayout.BeginVertical();
		GUILayout.FlexibleSpace();
		if (GUILayout.Button(refreshIcon, GUI.flatButtonStyle))
		{
			LoadClipboardEntries();
		}
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndVertical();

		EditorGUILayout.BeginVertical();
		GUILayout.FlexibleSpace();
		if (GUILayout.Button(searchIcon, GUI.flatButtonStyle))
		{
			showSearch = !showSearch;
		}
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndVertical();

		EditorGUILayout.BeginVertical();
		GUILayout.FlexibleSpace();
		if (GUILayout.Button(favoriteIcon, GUI.flatButtonStyle))
		{
			showFavorites = !showFavorites;
			FilterClipboardEntries();
		}
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndVertical();

		EditorGUILayout.BeginVertical();
		GUILayout.FlexibleSpace();
		if (GUILayout.Button(settingsIcon, GUI.flatButtonStyle))
		{
			SettingsWindow.Show(HandleSettingsSaved);
		}
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndVertical();

		EditorGUIUtility.SetIconSize(Vector2.zero);

		EditorGUILayout.EndHorizontal();

		EditorGUILayout.Space();
	}

	private void HandleSettingsSaved()
	{
		LoadClipboardEntries();
	}

	private void RenderSearch()
	{
		if (!showSearch)
		{
			return;
		}

		EditorGUI.BeginChangeCheck();
		searchText = EditorGUILayout.TextField(searchText);
		if (string.IsNullOrEmpty(searchText))
		{
			GUI.Placeholder("Enter search text...");
		}
		if (EditorGUI.EndChangeCheck())
		{
			FilterClipboardEntries();
		}
		EditorGUILayout.Space();
	}

	private void RenderEntries()
	{
		int entriesPerColumn = Mathf.Max(1, (int) position.width / Settings.Data.previewThumbnailWidth);
		int safeWidth = (int) position.width - entriesPerColumn * 8;
		int columnWidth = safeWidth / entriesPerColumn;

		EditorGUILayout.BeginHorizontal();
		// TODO This fixes an alignment issue in 2021.3 / macOS, test on other systems
		if (clipboardEntries != null && clipboardEntries.Count > 0)
		{
			GUILayout.Space(3);
		}
		scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUIStyle.none, UnityEngine.GUI.skin.verticalScrollbar);
		if (clipboardEntries == null || clipboardEntries.Count == 0)
		{
			EditorGUILayout.HelpBox("No clipboard entries found yet! Drag and drop a sub-tree onto this window!", MessageType.Info);
		}
		else
		{
			int numberOfItems = filteredClipboardEntries.Count;
			if (numberOfItems % entriesPerColumn != 0)
			{
				numberOfItems += entriesPerColumn - (numberOfItems % entriesPerColumn);
			}

			for (int i = 0; i < numberOfItems; ++i)
			{
				if (i % entriesPerColumn == 0)
				{
					EditorGUILayout.BeginHorizontal(GUIStyle.none);
				}

				EditorGUILayout.BeginVertical(GUIStyle.none);

				ClipboardEntryMetadata entry = i < filteredClipboardEntries.Count ? filteredClipboardEntries[i] : null;
				Texture2D itemTexture = entry != null && previewImages.ContainsKey(entry.id) ? previewImages[entry.id] : Texture2D.blackTexture;

				GUILayout.Box(GUIContent.none, GUIStyle.none, GUILayout.Width(columnWidth), GUILayout.Height(columnWidth));

				if (entry != null)
				{
					Rect pos = GUILayoutUtility.GetLastRect();
					if (UnityEngine.GUI.Button(pos, itemTexture))
					{
						ImportWindow.Show(
							entry,
							entry != null && previewImages.ContainsKey(entry.id) ? previewImages[entry.id] : null,
							HandleClipboardEntryDeleted
						);
					}
					if (entry.favorite)
					{
						Rect favoriteIconRect = pos;
						favoriteIconRect.x = favoriteIconRect.x + favoriteIconRect.width - 20;
						favoriteIconRect.y = favoriteIconRect.y + favoriteIconRect.height - 20;
						favoriteIconRect.width = 20;
						favoriteIconRect.height = 20;
						UnityEngine.GUI.Label(favoriteIconRect, favoriteIcon);
					}
					if (columnWidth > 150)
					{
						EditorGUILayout.BeginVertical(GUILayout.MaxWidth(columnWidth));
						GUILayout.Label(entry.name, GUI.previewTitleStyle);
						EditorGUILayout.EndVertical();
					}
					else
					{
						EditorGUILayout.Space();
					}
				}

				EditorGUILayout.EndVertical();

				if (i % entriesPerColumn == entriesPerColumn - 1)
				{
					EditorGUILayout.EndHorizontal();
				}
			}
		}
		EditorGUILayout.EndScrollView();
		EditorGUILayout.EndHorizontal();
	}

	private void HandleClipboardEntryDeleted(string id)
	{
		// Remove the files
		File.Delete(Path.Combine(Settings.Data.contentDirectory, $"{id}.meta"));
		File.Delete(Path.Combine(Settings.Data.contentDirectory, $"{id}.data"));
		File.Delete(Path.Combine(Settings.Data.contentDirectory, $"{id}.png"));
		// Remove the entry
		clipboardEntries = clipboardEntries.Where(entry => entry.id != id).ToList();
		FilterClipboardEntries();
	}

	private void HandleDragAndDrop()
	{
		isDragging = false;
		Vector2 mouseOnWindow = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
		if (position.Contains(mouseOnWindow))
		{
			if (Event.current.type == EventType.DragUpdated)
			{
				if (DragAndDrop.objectReferences.Length > 0)
				{
					GameObject go = DragAndDrop.objectReferences[0] as GameObject;
					if (go != null)
					{
						DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
						Event.current.Use();
						isDragging = true;
					}
				}
			}
			else if (Event.current.type == EventType.DragPerform)
			{
				Event.current.Use();

				// Collect all valid GameObjects from the drag:
				// Must be GameObject
				List<GameObject> validGameObjects = DragAndDrop.objectReferences.OfType<GameObject>().ToList();
				// Must be top-level GameObject (i.e. if parent and child are selected, only pick the parent as the child is included anyway)
				GameObject[] topLevelGameObjects = validGameObjects.Where(gameObject => {
					foreach (GameObject otherGameObject in validGameObjects)
					{
						if (otherGameObject != gameObject)
						{
							if (gameObject.transform.IsChildOf(otherGameObject.transform))
							{
								return false;
							}
						}
					}
					return true;
				}).ToArray();

				if (topLevelGameObjects.Length > 0)
				{
					SaveToClipboard(topLevelGameObjects);
				}
			}
		}
	}

	private void LoadClipboardEntries()
	{
		clipboardEntries = new();
		previewImages = new();

		string[] files = Directory.GetFiles(Settings.Data.contentDirectory, "*.meta");
		foreach (string file in files)
		{
			string id = Path.GetFileNameWithoutExtension(file);
			string content = File.ReadAllText(Path.Combine(Settings.Data.contentDirectory, file));
			ClipboardEntryMetadata entry = JsonConvert.DeserializeObject<ClipboardEntryMetadata>(content);
			entry.id = id;

			AddEntry(entry, false);
		}

		clipboardEntries = clipboardEntries.OrderByDescending(entry => entry.created).ToList();

		FilterClipboardEntries();
	}

	private void FilterClipboardEntries()
	{
		if (!showSearch)
		{
			if (showFavorites)
			{
				filteredClipboardEntries = clipboardEntries
					.Where(entry => entry.favorite)
					.ToList();
			}
			else
			{
				filteredClipboardEntries = clipboardEntries;
			}
		}
		else
		{
			filteredClipboardEntries = clipboardEntries
				.Where(entry => entry.name.Contains(searchText, StringComparison.OrdinalIgnoreCase) || (!string.IsNullOrEmpty(entry.tags) && entry.tags.Contains(searchText, StringComparison.OrdinalIgnoreCase)))
				.ToList();
		}
	}

	private void AddEntry(ClipboardEntryMetadata entry, bool prepend)
	{
		if (prepend)
		{
			clipboardEntries.Insert(0, entry);
		}
		else
		{
			clipboardEntries.Add(entry);
		}

		EditorCoroutineUtility.StartCoroutine(
			LoadPreviewImage(entry.id, Path.Combine(Settings.Data.contentDirectory, $"{entry.id}.png")),
			this
		);
	}

	private IEnumerator LoadPreviewImage(string id, string fileName)
	{
		byte[] data = File.ReadAllBytes(fileName);
		Texture2D texture = new Texture2D(2, 2);
		texture.LoadImage(data);
		previewImages.Add(id, texture);

		yield return null;
	}

	private void SaveToClipboard(GameObject[] gameObjects)
	{
		// Create serialized representation; merge multiple GOs into one tree afterwards
		PrefabDictionary prefabDictionary = new();
		bool multipleGOs = gameObjects.Length > 1;
		List<SerializedTree> trees = new();
		for (int i = 0; i < gameObjects.Length; ++i)
		{
			GameObject go = gameObjects[i];
			ISerializer serializer = new JsonV1Serializer(go, prefabDictionary);
			SerializedTree child = serializer.Serialize();
			SerializedGameObject rootOfChild = child.FindById(child.root);
			rootOfChild.siblingIndex = multipleGOs ? i : null;
			rootOfChild.position = multipleGOs ? new SerializedVector(go.transform.position) : null;
			rootOfChild.rotation = multipleGOs ? new SerializedVector(go.transform.rotation.eulerAngles) : null;
			rootOfChild.scale = multipleGOs ? new SerializedVector(go.transform.localScale) : null;
			trees.Add(child);
		}
		SerializedTree tree;
		SerializedGameObject root;
		if (multipleGOs)
		{
			root = new()
			{
				id = Guid.NewGuid().ToString("N"),
				name = $"{gameObjects.Length} game objects",
				children = trees.Select(tree => tree.root).ToList()
			};
			tree = new()
			{
				gameObjects = new(),
				root = root.id
			};
			tree.gameObjects.Add(root);
			foreach (SerializedTree child in trees)
			{
				tree.gameObjects.AddRange(child.gameObjects);
			}
		}
		else
		{
			tree = trees[0];
			root = tree.FindById(tree.root);
		}

		ClipboardEntryData data = new()
		{
			representation = "JsonV1",
			tree = tree
		};
		string json = JsonConvert.SerializeObject(data, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

		// Write tree JSON to file
		string guid = Guid.NewGuid().ToString("N");
		string dataFileName = Path.Combine(Settings.Data.contentDirectory, $"{guid}.data");
		if (Settings.Data.saveCompressed)
		{
			using var jsonByteStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
			using FileStream compressedFileStream = File.Create(dataFileName);
			using var compressor = new GZipStream(compressedFileStream, CompressionMode.Compress);
			jsonByteStream.CopyTo(compressor);
		}
		else
		{
			using StreamWriter file = new StreamWriter(dataFileName);
			file.Write(json);
		}

		int numberOfPrefabs = tree.gameObjects.Where(sgo => sgo.prefab != null).Count();

		// Write metadata
		ClipboardEntryMetadata meta = new()
		{
			id = guid,
			name = root.name,
			created = DateTime.Now,
			numberOfPrefabs = numberOfPrefabs,
			dictionary = prefabDictionary,
			compressed = Settings.Data.saveCompressed
		};
		meta.Save();

		// Rebuild the tree and take a screenshot
		Transform result = new JsonV1Deserializer(
			tree,
			prefabDictionary
		).Deserialize(null);
		result.transform.position += Settings.Data.objectOffsetForPreview.ToVector3();
		byte[] screenshot = TakePreviewScreenshot(result.gameObject);
		string screenshotFileName = Path.Combine(Settings.Data.contentDirectory, $"{guid}.png");
		using FileStream screenshotFile = new FileStream(screenshotFileName, FileMode.Create);
		using BinaryWriter writer = new BinaryWriter(screenshotFile, Encoding.UTF8);
		writer.Write(screenshot);
		DestroyImmediate(result.gameObject);

		// Update entries
		AddEntry(meta, true);
	}

	private byte[] TakePreviewScreenshot(GameObject go)
	{
		int width = 512;
		int height = 512;
		Bounds bounds = GetObjectBounds(go);
		GameObject tempCameraGO = new GameObject("PrefabbyClipboardTempCamera");
		RenderTexture rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
		Camera tempCamera = tempCameraGO.AddComponent<Camera>();
		tempCamera.targetTexture = rt;
		tempCamera.clearFlags = CameraClearFlags.SolidColor;
		tempCamera.backgroundColor = new Color();
		tempCamera.fieldOfView = 60f;
		GameObject tempLightGO1 = new GameObject("PrefabbyTempLight1");
		tempLightGO1.transform.SetParent(tempCameraGO.transform);
		Light light1 = tempLightGO1.AddComponent<Light>();
		light1.type = LightType.Point;
		// Try to position the camera to view the entire object. This was inspired by this post on the Unity forums:
		// https://forum.unity.com/threads/fit-object-exactly-into-perspective-cameras-field-of-view-focus-the-object.496472/#post-6712018
		float marginPercentage = 1.05f;
		float maxExtent = bounds.extents.magnitude;
		float minDistance = (maxExtent * marginPercentage) / Mathf.Sin(Mathf.Deg2Rad * tempCamera.fieldOfView / 2f);
		tempCamera.transform.position = bounds.center + Vector3.forward * minDistance;
		tempCamera.nearClipPlane = minDistance - maxExtent;
		tempCamera.transform.LookAt(bounds.center);
		// We rotate the camera slightly, though, for a more interesting perspective
		tempCamera.transform.RotateAround(bounds.center, tempCamera.transform.up, 35);
		tempCamera.transform.RotateAround(bounds.center, tempCamera.transform.right, 35);
		tempCamera.Render();
		RenderTexture currentRenderTexture = RenderTexture.active;
		RenderTexture.active = tempCamera.targetTexture;
		Texture2D screenshot = new Texture2D(width, height, TextureFormat.ARGB32, false);
		screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
		screenshot.Apply(false);
		byte[] result = screenshot.EncodeToPNG();
		RenderTexture.active = currentRenderTexture;
		tempCamera.targetTexture = null;
		DestroyImmediate(tempCameraGO);
		return result;
	}

	private Bounds GetObjectBounds(GameObject go)
	{
		if (go == null)
		{
			return new Bounds();
		}
		Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
		if (renderers.Length == 0)
		{
			return new Bounds();
		}
		Bounds bounds = renderers[0].bounds;
		for (int i = 1; i < renderers.Length; ++i)
		{
			// Ignore particle systems as they're not initialized yet and will screw the offset
			if (renderers[i] is not ParticleSystemRenderer)
			{
				bounds.Encapsulate(renderers[i].bounds);
			}
		}
		return bounds;
	}

}

}
