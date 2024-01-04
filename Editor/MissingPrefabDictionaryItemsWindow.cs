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

using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace PrefabbyClipboard
{

class MissingPrefabDictionaryItemsWindow : EditorWindow
{

	private List<PrefabDictionaryItem> missingItems;

	private Vector2 scrollPosition = new();

	public static MissingPrefabDictionaryItemsWindow Show(List<PrefabDictionaryItem> missingItems)
	{
		MissingPrefabDictionaryItemsWindow window = GetWindow(typeof(MissingPrefabDictionaryItemsWindow), true, "Prefab directories missing in project", false) as MissingPrefabDictionaryItemsWindow;
		window.missingItems = missingItems;
		return window;
	}

	void OnGUI()
	{
		scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUI.windowMargin);

		GUILayout.Label("The following directories are required for this clipboard entry but seem to be missing in the project.", GUI.richTextLabelStyle);
		EditorGUILayout.Space();

		foreach (PrefabDictionaryItem item in missingItems)
		{
			EditorGUILayout.BeginHorizontal(GUI.windowMargin);
			GUILayout.Label($"<b>{item.path}</b> with <b>{item.verification}</b>", GUI.richTextLabelStyle);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();
		}

		GUILayout.FlexibleSpace();

		if (GUILayout.Button("Close", GUILayout.Height(32)))
		{
			Close();
		}

		EditorGUILayout.Space();
		EditorGUILayout.EndScrollView();
	}

}

}
