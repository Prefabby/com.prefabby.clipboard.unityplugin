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
using System.Linq;

namespace PrefabbyClipboard
{

[System.Serializable]
public class PrefabDictionary
{

	public List<PrefabDictionaryItem> items = new();

	public PrefabDictionaryItem ResolveOrCreate(string path, string name, PrefabDictionaryItemType type)
	{
		DebugUtils.Log(DebugContext.PrefabDictionaryIncoming, $"Trying to resolve path={path}, name={name}...");

		path = EditorUtils.SanitizePath(path);

		foreach (PrefabDictionaryItem item in items)
		{
			DebugUtils.Log(DebugContext.PrefabDictionaryIncoming, $"- Looking at known item path {item.path}...");
			if (item.path == path && item.type == type)
			{
				DebugUtils.Log(DebugContext.PrefabDictionaryIncoming, "- Match found!");
				return item;
			}
		}

		PrefabDictionaryItem newItem = new()
		{
			id = Guid.NewGuid().ToString("N"),
			path = path,
			verification = name,
			type = type
		};
		items.Add(newItem);

		return newItem;
	}

	public PrefabDictionaryItem GetItemById(string id)
	{
		DebugUtils.Log(DebugContext.PrefabDictionaryIncoming, $"PrefabDictionary.GetItemById({id}): currently {items.Count} items in the list");
		return items.Find(item => item.id == id);
	}

	public override string ToString()
	{
		return $"{base.ToString()}: {items.Count} items";
	}

}

}
