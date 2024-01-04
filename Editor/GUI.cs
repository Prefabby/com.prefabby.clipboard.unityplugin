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

using UnityEngine;
using UnityEditor;

namespace PrefabbyClipboard
{

class GUI
{
	public static bool initialized = false;

	public static void InitializeIfNecessary()
	{
		if (initialized)
		{
			return;
		}

		windowMargin = new GUIStyle
		{
			margin = new RectOffset(5, 5, 5, 5)
		};
		previewTitleStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
		{
			margin = new RectOffset(0, 0, 4, 0),
			wordWrap = true
		};
		previewImageContainerStyle = new GUIStyle(UnityEngine.GUI.skin.GetStyle("HelpBox"));
		flatButtonStyle = new GUIStyle
		{
			margin = new RectOffset(4, 4, 4, 4),
			padding = new RectOffset(2, 2, 2, 2),
			alignment = TextAnchor.MiddleLeft,
			normal =
			{
				textColor = UnityEngine.GUI.skin.textField.normal.textColor,
				background = null
			},
		};
		selectableLabelStyle = new GUIStyle
		{
			alignment = TextAnchor.UpperLeft,
			padding = new RectOffset(3, 15, 2, 0),
			wordWrap = true,
			clipping = TextClipping.Clip,
			normal =
			{
				textColor = Color.grey
			},
		};
		placeholderStyle = new GUIStyle
		{
			alignment = TextAnchor.UpperLeft,
			padding = new RectOffset(3, 0, 2, 0),
			fontStyle = FontStyle.Italic,
			wordWrap = false,
			clipping = TextClipping.Clip,
			normal =
			{
				textColor = Color.grey
			},
		};
		placeholderWordwrappedStyle = new GUIStyle
		{
			alignment = TextAnchor.UpperLeft,
			padding = new RectOffset(3, 0, 2, 0),
			fontStyle = FontStyle.Italic,
			wordWrap = true,
			clipping = TextClipping.Clip,
			normal =
			{
				textColor = Color.grey
			},
		};

		groupStyle = new GUIStyle(UnityEngine.GUI.skin.GetStyle("HelpBox"))
		{
			padding = new RectOffset(10, 10, 10, 10)
		};

		titleBarStyle = new GUIStyle(UnityEngine.GUI.skin.GetStyle("HelpBox"))
		{
			padding = new RectOffset(5, 5, 5, 5),
			richText = true
		};
		titleBarStyle.normal.textColor = new Color(1f, 1f, 1f, 1f);

		richTextLabelStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
		{
			wordWrap = true,
			richText = true
		};

		initialized = true;
	}

	public static GUIStyle windowMargin;
	public static GUIStyle previewTitleStyle;
	public static GUIStyle previewImageContainerStyle;
	public static GUIStyle flatButtonStyle;
	public static GUIStyle selectableLabelStyle;
	public static GUIStyle placeholderStyle;
	public static GUIStyle placeholderWordwrappedStyle;
	public static GUIStyle groupStyle;
	public static GUIStyle richTextLabelStyle;
	public static GUIStyle titleBarStyle;

	public static void CenteredText(string text)
	{
		GUILayout.FlexibleSpace();
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		GUILayout.Label(text);
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		GUILayout.FlexibleSpace();
	}

	public static void Placeholder(string text, bool wordwrap = false)
	{
		Rect pos = new Rect(GUILayoutUtility.GetLastRect());
		EditorGUI.LabelField(pos, text, wordwrap ? placeholderWordwrappedStyle : placeholderStyle);
	}

	public static Texture2D CreateColorTexture(Color col)
	{
		Color32[] pixels = new Color32[4];
		Array.Fill(pixels, col);
		Texture2D result = new(2, 2);
		result.SetPixels32(pixels);
		result.Apply();
		return result;
	}

	public static bool DraggableArea(int height, string label)
	{
		Rect rect = GUILayoutUtility.GetRect(0, height, GUILayout.ExpandWidth(true));
		UnityEngine.GUI.Box(rect, label, "button");
		if (rect.Contains(Event.current.mousePosition))
		{
			if (Event.current.type == EventType.DragUpdated)
			{
				DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
				Event.current.Use();
				return false;
			}
			else if (Event.current.type == EventType.DragPerform)
			{
				Event.current.Use();
				return true;
			}
		}
		return false;
	}

}

}
