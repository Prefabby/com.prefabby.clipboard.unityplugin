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
using UnityEditor;

namespace PrefabbyClipboard
{

class SettingsWindow : EditorWindow
{

	private Action onSettingsSaved;

	private Texture2D folderIcon;
	private bool resized = false;

	private string contentDirectory;
	private Vector3 offset;
	private int thumbnailWidth;
	private int maximumDistance;
	private bool saveCompressed;

	public static SettingsWindow Show(Action onSettingsSaved)
	{
		SettingsWindow window = GetWindow(typeof(SettingsWindow), true, "Prefabby Clipboard Settings", false) as SettingsWindow;
		window.onSettingsSaved = onSettingsSaved;
		window.Initialize();
		EditorUtils.CenterEditorWindow(window, 600, 400);
		return window;
	}

	public void Initialize()
	{
		contentDirectory = Settings.Data.contentDirectory;
		offset = Settings.Data.objectOffsetForPreview.ToVector3();
		thumbnailWidth = Settings.Data.previewThumbnailWidth;
		maximumDistance = Settings.Data.maximumDistance;
		saveCompressed = Settings.Data.saveCompressed;
	}

	void OnEnable()
	{
		folderIcon = EditorGUIUtility.FindTexture("d_Folder Icon");
	}

	void OnGUI()
	{
		GUILayout.BeginVertical(GUI.windowMargin);

		if (Settings.IsProjectLocal)
		{
			EditorGUILayout.HelpBox("These settings are project-local. If you want to remove these settings and use global settings, please click below.", MessageType.Info);
			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Remove local settings"))
			{
				Settings.RemoveLocal();
				Initialize();
			}
			EditorGUILayout.EndHorizontal();
		}
		else
		{
			EditorGUILayout.HelpBox("These settings are global. If you want to create a project-local settings file e.g. to store clipboard entries in the project directory, please click below.", MessageType.Info);
			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Create local settings"))
			{
				Settings.MakeLocal();
				Initialize();
			}
			EditorGUILayout.EndHorizontal();
		}
		EditorGUILayout.Space();

		EditorGUILayout.BeginVertical(GUI.groupStyle);
		GUILayout.Label("Content Directory", EditorStyles.boldLabel);
		EditorGUILayout.BeginHorizontal();
		UnityEngine.GUI.enabled = false;
		EditorGUILayout.TextField(contentDirectory, GUILayout.ExpandWidth(true), GUILayout.Height(20));
		UnityEngine.GUI.enabled = true;
		if (GUILayout.Button(folderIcon, GUILayout.Width(32), GUILayout.Height(20)))
		{
			string newPath = EditorUtility.OpenFolderPanel("Select content folder", contentDirectory, "");
			if (!string.IsNullOrEmpty(newPath))
			{
				contentDirectory = newPath;
			}
			GUIUtility.ExitGUI();
		}
		if (GUILayout.Button("Project", GUILayout.ExpandWidth(false), GUILayout.Height(20)))
		{
			contentDirectory = Directory.GetCurrentDirectory();
		}
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.EndVertical();
		EditorGUILayout.Space();

		EditorGUILayout.BeginVertical(GUI.groupStyle);
		GUILayout.Label("Maximum selection distance", EditorStyles.boldLabel);
		GUILayout.Label("Unity's select might add unwanted objects to the selection. You can specify a maximum distance from the closest object to the camera to reduce the selection.", EditorStyles.wordWrappedLabel);
		maximumDistance = EditorGUILayout.IntField(maximumDistance);
		GUILayout.EndVertical();
		EditorGUILayout.Space();

		EditorGUILayout.BeginVertical(GUI.groupStyle);
		GUILayout.Label("Offset for preview screenshot", EditorStyles.boldLabel);
		GUILayout.Label("When a clipboard entry is created, Prefabby Clipboard rebuilds the selected object in the scene to create a screenshot using the given offset from the scene origin. If parts of your original scene appear in the preview, you can increase this offset.", EditorStyles.wordWrappedLabel);
		offset = EditorGUILayout.Vector3Field("", offset);
		EditorGUILayout.EndVertical();
		EditorGUILayout.Space();

		EditorGUILayout.BeginVertical(GUI.groupStyle);
		GUILayout.Label("Preview thumbnail width", EditorStyles.boldLabel);
		GUILayout.Label("Width of the thumbnails in the Prefabby Clipboard window in pixels.", EditorStyles.wordWrappedLabel);
		thumbnailWidth = EditorGUILayout.IntField(thumbnailWidth);
		GUILayout.EndVertical();
		EditorGUILayout.Space();

		EditorGUILayout.BeginVertical(GUI.groupStyle);
		GUILayout.Label("Save compressed", EditorStyles.boldLabel);
		GUILayout.BeginHorizontal();
		saveCompressed = EditorGUILayout.ToggleLeft("", saveCompressed, GUILayout.Width(20));
		GUILayout.Label("Enable to save the structure data compressed to save some space; disable to store as plain JSON for better version control integration.", EditorStyles.wordWrappedLabel, GUILayout.ExpandWidth(true));
		GUILayout.EndHorizontal();
		GUILayout.EndVertical();
		EditorGUILayout.Space();

		if (GUILayout.Button("Save", GUILayout.Height(32)))
		{
			SaveAndClose();
		}

		if (Event.current.type == EventType.Repaint && !resized)
		{
			Rect pos = new Rect(GUILayoutUtility.GetLastRect());
			float margin = pos.x;
			Rect currentRect = position;
			currentRect.size = new Vector2(pos.width + 2 * margin, pos.y + pos.height + margin);
			position = currentRect;
			EditorUtils.CenterEditorWindow(this, currentRect.width, currentRect.height);
			resized = true;
		}
	}

	private void SaveAndClose()
	{
		Settings.Data.objectOffsetForPreview = new SerializedVector(offset);
		Settings.Data.maximumDistance = maximumDistance;
		Settings.Data.previewThumbnailWidth = thumbnailWidth;
		Settings.Data.contentDirectory = contentDirectory;
		Settings.Data.saveCompressed = saveCompressed;

		Settings.Save();

		onSettingsSaved();

		Close();
	}

}

}
