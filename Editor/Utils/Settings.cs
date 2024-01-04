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
using System.IO;

using UnityEngine;

using Newtonsoft.Json;

namespace PrefabbyClipboard
{

public static class Settings
{

	public static SettingsData Data { get; private set; }
	public static bool IsProjectLocal { get; private set; }

	private static string settingsFileName = "PrefabbyClipboardSettings.json";
	private static string localSettingsFile = Path.Combine(Directory.GetCurrentDirectory(), settingsFileName);
	private static string globalSettingsFile = Path.Combine(GetApplicationSettingsDirectory(), settingsFileName);

	private static string GetApplicationSettingsDirectory()
	{
		string basePath;
		if (Application.platform == RuntimePlatform.WindowsEditor)
		{
			basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		}
		else if (Application.platform == RuntimePlatform.OSXEditor)
		{
			string homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			basePath = Path.Combine(homePath, "Library/Application Support");
		}
		else
		{
			basePath = "~/.config";
		}

		string applicationSettingsDirectory = Path.Combine(basePath, "PrefabbyClipboard");
		Directory.CreateDirectory(applicationSettingsDirectory);
		return applicationSettingsDirectory;
	}

	public static void Load()
	{
		string fileToLoad = null;

		if (File.Exists(localSettingsFile))
		{
			fileToLoad = localSettingsFile;
			IsProjectLocal = true;
		}
		else if (File.Exists(globalSettingsFile))
		{
			fileToLoad = globalSettingsFile;
			IsProjectLocal = false;
		}

		if (fileToLoad != null)
		{
			string content = File.ReadAllText(fileToLoad);
			Data = JsonConvert.DeserializeObject<SettingsData>(content);
		}

		if (Data == null)
		{
			string contentDirectory = Path.Combine(GetApplicationSettingsDirectory(), "Content");
			Directory.CreateDirectory(contentDirectory);

			IsProjectLocal = false;
			Data = new()
			{
				contentDirectory = contentDirectory
			};
		}
	}

	public static void Save()
	{
		string baseDir = IsProjectLocal ? Directory.GetCurrentDirectory() : GetApplicationSettingsDirectory();
		string settingsFile = Path.Combine(baseDir, settingsFileName);
		string json = JsonConvert.SerializeObject(Data, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
		using (StreamWriter metaFile = new StreamWriter(settingsFile))
        {
            metaFile.Write(json);
        }
	}

	public static void RemoveLocal()
	{
		File.Delete(localSettingsFile);
		Load();
	}

	public static void MakeLocal()
	{
		IsProjectLocal = true;
		Save();
	}

}

}
