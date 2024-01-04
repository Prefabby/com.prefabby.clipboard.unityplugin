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

using Newtonsoft.Json;

namespace PrefabbyClipboard
{

class ClipboardEntryMetadata
{

	[JsonIgnore]
	public string id;
	public string name;
	public string tags;
	public DateTime created;
	public PrefabDictionary dictionary;
	public int numberOfPrefabs;
	public bool compressed;
	public bool favorite;

	public void Save()
	{
		string json = JsonConvert.SerializeObject(this, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
		string metaFileName = Path.Combine(Settings.Data.contentDirectory, $"{id}.meta");
		using (StreamWriter metaFile = new StreamWriter(metaFileName))
		{
			metaFile.Write(json);
		}
	}

}

}
