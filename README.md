<a href="https://prefabby.com">
    <img src="Images/PrefabbyLogo.png" alt="Prefabby logo" title="Prefabby" align="right" height="60" />
</a>

# Prefabby Clipboard Unity Plugin

Prefabby is a platform to collaboratively build, share, and remix creations made out of prefab art packs; currently available for Unity and Godot.

This repository hosts the Prefabby Clipboard plugin for the Unity game engine.

## 💡 About the Prefabby Clipboard plugin

**Here's the short marketing version:**

Prefabby Clipboard allows you to store scene parts built out of prefabs in a project-local or system-global store for easier reuse. It's a great helper if you quickly want to copy content from one Unity instance to another, or if you want to build up a repository of reusable parts within a project.

**And here's a bit longer background which probably explains best what Prefabby Clipboard is and how it's meant to be used – my own use case:**

As a developer, I often find myself looking at the demo scenes of art packs I am purchasing, in awe for what talented designers are able to achieve. I am not a talented designer, so I tend to copy bits I like and then rearrange them.

But if I run the reference scene in a separate Unity instance, I can't copy it over using the system's clipboard at all; I would have go through a cumbersome process of creating, exporting, reimporting and unpacking a prefab. If I load the reference scene from within the same instance, I would still have to switch back and forth or load it as an "additive" scene, and if I wanted to reproduce the process, I would have to pick the same sub-tree again and start over. And either way, I wouldn't be able to reuse things in a different engine even if I had the same art packs in both engines!

I always wanted to have a simple repository of reusable reference parts, just like prefabs, but without the hassle.

That's why I built [Prefabby Community](https://github.com/Prefabby/com.prefabby.community.unityplugin), which is Prefabby Clipboard's 'big brother'. But it runs in the cloud, is meant for public sharing and collaboration, and only supports known prefab art packs. Prefabby Clipboard runs fully local on your harddrive, without having to create an account and without being restricted to supported art packs.

## ⚡️ Quickstart

In your Unity project, open the Package Manager window and click on the + icon in the top left, then select "Add package from Git URL...". Copy this URL into the input field which popups up:

	https://github.com/Prefabby/com.prefabby.clipboard.unityplugin.git

Once the plugin is installed, you will find a new menu entry `Tools -> Prefabby Clipboard` in your menu bar. Click it to open the Prefabby window and dock it somewhere to your liking.

You can now drag and drop an element of your scene hierarchy into the window to have a clipboard entry stored. Prefabby Clipboard will parse the element, rebuild it in your scene to take an isolated preview screenshot, and store everything locally on your harddisk.

You can then click on the thumbnail image in the Prefabby Clipboard window to open an "Import" window. From there, you can recreate this hierarchy as a child of the currently selected scene object by clicking on the "Import" button.

## 📋 How the clipboard works

### Storage

Once you drag and drop a scene element into the Prefabby Clipboard window, a clipboard entry is created and the plugin stores three things:

1. Metadata, like the name of the entry
2. The object structure
3. A screenshot

Prefabby Clipboard stores these items as separate files in a storage directory. The default for this directory is a subdirectory in your system's application settings directory.

On Windows, this would be
```
%USERPROFILE%\Local Settings\Application Data\PrefabbyClipboard\Content
```

On macOS, this would be
```
$HOME/Library/Application Support/PrefabbyClipboard/Content
```

If you prefer a project-local storage of clipboard entries, you can open the Prefabby Clipboard settings by clicking on the small cog icon at the top right of the window and then click on "Create local settings". This will create a local configuration file "PrefabbyClipboardSettings.json" in your project main directory which always takes precedence over the global settings, if found.

Note that you still have to manually change the content directory afterwards, as Prefabby Clipboard doesn't make any assumptions about your preferred project-local content directory.

The stored metadata also contains a list of all required art pack directories, i.e. the original directories from where the prefabs were created. If you want to reproduce the same hierarchy in a different project, you need to have the same art packs installed and Prefabby Clipboard checks for the existance whenever you attempt to import a clipboard entry into the scene.

### Entry names, searching and favorites

When a clipboard entry is stored, the selected prefab's name is used as the name for the entry. If more than one object is selected, the name will indicate the number of items instead.

You can change the name by clicking on the clipboard entry thumbnail, changing the name on the Import dialog, and clicking "Update".

You can enter additional "tags" for the entry, which are typically comma-separated keywords, like e.g. `building,city,skyscraper,residential`.

You can mark this entry as a favorite in the same window by clicking on the button with the star icon.

When you have many entries stored and need to search for one specific entry quickly, you can use the search function by clicking on the magnifier icon at the top right of the window. This will show a search text input where you can enter a search term. Prefabby Clipboard will show all entries which match the entered term in the name or in the tags.

You can also show favorites only by clicking on the star icon in the top right of the Prefabby Clipboard window.

### Deleting entries

If you don't need a specific entry anymore, you can delete it by clicking on it once to open the Import dialog and then clicking on the recycle bin icon.

⚠️ This will delete the corresponding files from the content directory and cannot be undone. ⚠️

### More settings

The settings dialog accessible through the cog icon at the top right of the Prefabby Clipboard window contains more settings you may find useful apart from the already discussed content directory setting.

The 'Offset for preview screenshot' setting configures where in the scene the rebuilt entry should be placed when a screenshot is taken. This is to avoid that artifacts of the remaining scene are visible in the screenshot. By default it's a rather large offset of -10000/-10000/-10000 but depending on your scene, this may not be sufficient and may need to be changed.

The 'Preview thumbnail width' is the width of the thumbnails shown in the Prefabby Clipboard window. If you prefer a larger or smaller preview, you can adjust this setting. Note that the name will only be shown under the thumbnail from 150 pixels upwards.

The 'Save compressed' option toggles whether the stored object structure will be stored compressed or as plain JSON. Storing it compressed saves a few bytes of harddrive space as it's only needed when it's actually imported, but if you're checking your content directory into version control, having plain text files might be preferable.

## 🚧 Restrictions

All Prefabby plugins only look at prefabs and only store transforms. Other native Unity objects which are not prefabs, like a box or a camera GameObject, or custom scripts attached to prefabs, won't be stored.

## 🗺️ Roadmap

Currently considered future updates:

* Currently, for every dragged scene object, a new entry is created. I would like to distinguish between creating a whole new entry and updating an existing entry. (This is also where uncompressed object structure might become more relevant.)

* There's currently no progress indicator when larger subtrees are processed in either direction. I would like to show some feedback to avoid the impression that Unity froze.

I am looking forward to reading your feedback and more suggestions.

## License [![AGPLv3](https://img.shields.io/badge/License-AGPL_v3-blue.svg)](./LICENSE.md)

The Prefabby Clipboard plugin for Unity is published under the AGPLv3 license. Details on used third party resources can be found in [Editor/ThirdParty](Editor/ThirdParty/README.txt).
