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

namespace PrefabbyClipboard
{

public enum DebugContext
{
	General,
	RestApi,
	Serialization,
	Deserialization,
	PrefabDictionaryIncoming,
	PrefabIdentification
}

public static class DebugUtils
{

	public static HashSet<DebugContext> enabledDebugContexts = new HashSet<DebugContext>
	{
		DebugContext.General,
		DebugContext.RestApi,
		DebugContext.Serialization,
		DebugContext.Deserialization,
		DebugContext.PrefabDictionaryIncoming,
		DebugContext.PrefabIdentification
	};

	public static bool logEnabled = true;

	public static void Log(DebugContext context, string message)
	{
		if (logEnabled && enabledDebugContexts.Contains(context))
		{
			Debug.Log($"[{context}]: {message}");
		}
	}

	public static void Log(DebugContext context, string message, Object obj)
	{
		if (logEnabled && enabledDebugContexts.Contains(context))
		{
			Debug.Log($"[{context}]: {message}", obj);
		}
	}

	public static void LogError(DebugContext context, string message)
	{
		if (logEnabled && enabledDebugContexts.Contains(context))
		{
			Debug.LogError($"[{context}]: {message}");
		}
	}

}

}
