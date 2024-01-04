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
using System.Linq;
using System.Text;

using UnityEngine;
using UnityEditor;
using Unity.EditorCoroutines.Editor;

using Newtonsoft.Json;

namespace PrefabbyClipboard
{

class ImportWindow : EditorWindow
{

	private const int previewPadding = 4;
	private const int spacer = 10;

	private GUIStyle iconStyle;
	private Texture2D checkmark;
	private Texture2D cross;
	private Texture2D favoriteIcon;
	private Texture2D deleteIcon;

	private ClipboardEntryMetadata clipboardEntry;
	private Texture2D texture;
	private Action<string> onClipboardEntryDeleted;
	private string editedName;
	private string editedTags;
	private List<PrefabDictionaryItem> missingItems;
	private bool checking = false;
	private bool importing = false;

	private Vector2 scrollPosition = new();
	private Vector2 requiredArtPacksScrollPos = new();

	public static ImportWindow Show(ClipboardEntryMetadata entry, Texture2D texture, Action<string> onClipboardEntryDeleted)
	{
		ImportWindow window = GetWindow(typeof(ImportWindow), true, "Import Clipboard Entry", false) as ImportWindow;
		window.clipboardEntry = entry;
		window.texture = texture;
		window.onClipboardEntryDeleted = onClipboardEntryDeleted;
		window.editedName = entry.name;
		window.editedTags = entry.tags;
		window.StartCheckRequiredPrefabDictionaryItems();

		EditorUtils.CenterEditorWindow(window, 1000, 510);

		return window;
	}

	void OnEnable()
	{
		iconStyle = new GUIStyle()
		{
			padding = new RectOffset(2, 2, 2, 2)
		};

		checkmark = Resources.Load("icons8-checkmark-16") as Texture2D;
		cross = Resources.Load("icons8-x-16") as Texture2D;
		favoriteIcon = EditorGUIUtility.FindTexture("Favorite@2x");
		deleteIcon = EditorGUIUtility.FindTexture("d_TreeEditor.Trash");
	}

	public void StartCheckRequiredPrefabDictionaryItems()
	{
		checking = true;

		EditorCoroutineUtility.StartCoroutine(
			DoCheckRequiredPrefabDictionaryItems(),
			this
		);
	}

	private IEnumerator DoCheckRequiredPrefabDictionaryItems()
	{
		yield return null;

		List<PrefabDictionaryItem> availableItems = EditorUtils.GetAvailablePrefabDictionaryItems(DebugContext.Deserialization, clipboardEntry.dictionary);
		missingItems = clipboardEntry.dictionary.items.Except(availableItems).ToList();

		checking = false;

		Repaint();
	}

	void OnGUI()
	{
		int columnWidth = (int)(position.width / 2 - spacer);

		scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUI.windowMargin);

		// Three columns
		EditorGUILayout.BeginHorizontal();

		// Preview image
		GUILayout.Box(null as Texture2D, GUI.previewImageContainerStyle, GUILayout.Width(columnWidth), GUILayout.Height(columnWidth), GUILayout.ExpandHeight(false));
		Rect pos = new(GUILayoutUtility.GetLastRect());
		pos.x += previewPadding;
		pos.y += previewPadding;
		pos.width -= previewPadding * 2;
		pos.height -= previewPadding * 2;
		UnityEngine.GUI.DrawTexture(pos, texture, ScaleMode.StretchToFill);
		if (clipboardEntry.favorite)
		{
			Rect favoriteIconRect = GUILayoutUtility.GetLastRect();
			favoriteIconRect.x = favoriteIconRect.x + favoriteIconRect.width - 25;
			favoriteIconRect.y = favoriteIconRect.y + favoriteIconRect.height - 25;
			favoriteIconRect.width = 20;
			favoriteIconRect.height = 20;
			UnityEngine.GUI.Label(favoriteIconRect, favoriteIcon);
		}

		// Spacer
		EditorGUILayout.Space(spacer, false);

		// Details
		EditorGUILayout.BeginVertical();

		UnityEngine.GUI.enabled = !importing && !checking;

		GUILayout.Label("Display Name", EditorStyles.boldLabel);
		editedName = EditorGUILayout.TextField(editedName);
		EditorGUILayout.Space();

		GUILayout.Label("Tags (comma-separated, used for search)", EditorStyles.boldLabel);
		editedTags = EditorGUILayout.TextArea(editedTags);
		EditorGUILayout.Space();

		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("Update", GUILayout.ExpandWidth(true), GUILayout.Height(20)))
		{
			clipboardEntry.name = editedName;
			clipboardEntry.tags = editedTags;
			clipboardEntry.Save();
		}
		if (GUILayout.Button(favoriteIcon, GUILayout.Width(32), GUILayout.Height(20)))
		{
			clipboardEntry.favorite = !clipboardEntry.favorite;
			clipboardEntry.Save();
		}
		if (GUILayout.Button(deleteIcon, GUILayout.Width(32), GUILayout.Height(20)))
		{
			if (EditorUtility.DisplayDialog("Confirm deletion", "Are you sure to delete this clipboard entry? This action cannot be undone.", "OK", "Cancel"))
			{
				onClipboardEntryDeleted(clipboardEntry.id);
				Close();
			}
			GUIUtility.ExitGUI();
		}
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.LabelField("", UnityEngine.GUI.skin.horizontalSlider);
		EditorGUILayout.Space();

		GUILayout.Label("Required Prefab Directories", EditorStyles.boldLabel);
		requiredArtPacksScrollPos = GUILayout.BeginScrollView(requiredArtPacksScrollPos, EditorStyles.textArea, GUILayout.ExpandHeight(true));
		foreach (PrefabDictionaryItem requiredItem in clipboardEntry.dictionary.items)
		{
			Texture2D icon;
			if (checking)
			{
				icon = null;
			}
			else
			{
				bool isMissing = missingItems != null && missingItems.Contains(requiredItem);
				icon = isMissing ? cross : checkmark;
			}
			EditorGUILayout.BeginHorizontal(GUILayout.Height(20));
			GUILayout.Box(icon, iconStyle, GUILayout.Width(20), GUILayout.Height(20));
			GUILayout.Label($"<b>{requiredItem.path}</b> with <b>{requiredItem.verification}</b>", GUI.richTextLabelStyle);
			EditorGUILayout.EndHorizontal();
		}
		GUILayout.EndScrollView();

		EditorGUILayout.Space();

		GUILayout.Label("Number of nodes with prefabs", EditorStyles.boldLabel);
		GUILayout.Label($"{clipboardEntry.numberOfPrefabs}");

		EditorGUILayout.Space();

		if (GUILayout.Button(importing || checking ? "Please wait..." : "Import", GUILayout.Height(32)))
		{
			StartImport();
		}

		UnityEngine.GUI.enabled = true;

		// End details
		EditorGUILayout.EndVertical();

		// End columns
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.Space();

		EditorGUILayout.EndScrollView();
	}

	private void StartImport()
	{
		importing = true;

		EditorCoroutineUtility.StartCoroutine(
			DoImport(),
			this
		);
	}

	private IEnumerator DoImport()
	{
		yield return null;

		// Ensure all required art packs are installed
		List<PrefabDictionaryItem> availableItems = EditorUtils.GetAvailablePrefabDictionaryItems(DebugContext.Deserialization, clipboardEntry.dictionary);
		if (availableItems.Count != clipboardEntry.dictionary.items.Count)
		{
			List<PrefabDictionaryItem> missingItems = clipboardEntry.dictionary.items.Except(availableItems).ToList();
			MissingPrefabDictionaryItemsWindow.Show(missingItems);
			yield break;
		}

		yield return null;

		// Extract serialized data
		string dataFileName = Path.Combine(Settings.Data.contentDirectory, $"{clipboardEntry.id}.data");
		string json;
		if (clipboardEntry.compressed)
		{
			using FileStream fileStream = File.OpenRead(dataFileName);
			using var decompressor = new GZipStream(fileStream, CompressionMode.Decompress);
			using var byteStream = new MemoryStream();
			decompressor.CopyTo(byteStream);
			json = Encoding.UTF8.GetString(byteStream.GetBuffer());
		}
		else
		{
			json = File.ReadAllText(dataFileName);
		}
		ClipboardEntryData data = JsonConvert.DeserializeObject<ClipboardEntryData>(json);

		yield return null;

		// Determine parent
		Transform parent = null;
		if (Selection.transforms.Length == 1)
		{
			parent = Selection.transforms[0];
		}

		Transform result = null;

		switch (data.representation)
		{
			case "JsonV1":
				result = new JsonV1Deserializer(data.tree, clipboardEntry.dictionary).Deserialize(parent);
				break;
			default:
				EditorUtils.Error("Unknown asset representation: {data.representation}. Try upgrading the plugin.");
				break;
		}

		// Focus created object
		if (result != null)
		{
			Selection.activeGameObject = result.gameObject;
			SceneView.FrameLastActiveSceneView();
		}

		Close();

		yield return null;
	}

}

}
