/*
 * Copyright (c) 2020 Razeware LLC
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * Notwithstanding the foregoing, you may not use, copy, modify, merge, publish,
 * distribute, sublicense, create a derivative work, and/or sell copies of the
 * Software in any work that is designed, intended, or marketed for pedagogical or
 * instructional purposes related to programming, coding, application development,
 * or information technology.  Permission for such use, copying, modification,
 * merger, publication, distribution, sublicensing, creation of derivative works,
 * or sale is expressly withheld.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;
using System.Threading.Tasks;
using UnityEngine.UI;
using System.IO;

[RequireComponent(typeof(ReferenceLookupManager))]
public class ModManager : MonoBehaviour
{
    public ReferenceLookupManager lookupManager;
    //In future this can be changed to the Application.persistentDataPath to put this in Local App Data
    [Header("Mod Path : /Assets/")]
    public string path;

    [Header("Loaded Mods")]
    public List<ModInfo> mods = new List<ModInfo>(); //List of existing mods in directory
    public Dictionary<string, ModInfo> modDictionary = new Dictionary<string, ModInfo>(); //Maps mods to a name so it can be easily loaded

    [Header("Current State")]
    public string activatedMod = ""; //Currently loaded mod

    [Header("UI")]
    public Button buttonPrefab;
    public Transform buttonParent;

    private List<Action> modUpdateListeners = new List<Action>();
    private List<Button> buttons = new List<Button>();

    private void Start()
    {
        path = Application.dataPath + "/" + path;
        //Getting the handle of the initial and loaded mod (default content pack)
        Addressables.InitializeAsync().Completed += handle =>
        {
            //Populating default mod information
            ModInfo defaultContent = new ModInfo
            {
                isDefault = true, //this is only try for the default mod
                locator = handle.Result,
                modAbsolutePath = "",
                modFile = null,
                modName = "Default"
            };

            mods.Add(defaultContent);

            ReloadDictionary();

            activatedMod = "Default";
            LoadCurrentMod();
        };

        //Load all other mods
        LoadMods();
    }

    public void RegisterListener(Action modChanged)
    {
        modUpdateListeners.Add(modChanged);
    }

    //Responsible for loading the possible mod's from a given directory
    private async void LoadMods()
    {
        //Getting directory information from path
        DirectoryInfo modDirectory = new DirectoryInfo(path);

        //Get all files in directory
        foreach (FileInfo file in modDirectory.GetFiles())
        {
            //Find files with a json extension to target catalogue files
            if (file.Extension == ".json")
            {
                string modName = file.Name;
                modName = modName.Replace(".json", "");
                modName = modName.Replace("_", " ");

                //Formatting the mod into Title Case for readability
                modName = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(modName.ToLower());

                //The resource locator for this mod
                IResourceLocator modLocator = await LoadCatalog(file.FullName);
                //Populating ModInfo
                ModInfo mod = new ModInfo
                {
                    modFile = file,
                    modAbsolutePath = file.FullName,
                    modName = modName,
                    isDefault = false,
                    locator = modLocator //Storing this mod's resource locator, allowing for future access this this mod's files and resources
                };

                //Add this mod to the list of existing mods (this does not load it into memory, just opens it to be loaded in future)
                mods.Add(mod);

                //Reload the dictionary of name and mod pairs
                ReloadDictionary();
            }
        }
    }

    //Loads the IResourceLocator from a catalog file (given the path)
    private async Task<IResourceLocator> LoadCatalog(string path)
    {
        AsyncOperationHandle<IResourceLocator> operation = Addressables.LoadContentCatalogAsync(path);
        IResourceLocator modLocator = await operation.Task; //Wait until the catalog file is loaded then retrieve the IResourceLocator for this mod
        return modLocator;
    }

    //This is called by button press
    public void ChangeMod (string newModName)
    {
        //Releases all the instances (unloading current mod from memory)
        lookupManager.ClearLoadedGameObjects();
        activatedMod = newModName;

        //Really loading the mod into memory
        LoadCurrentMod();
    }

    private void LoadCurrentMod()
    {
        if (modDictionary.ContainsKey(activatedMod))
        {
            //Clear all the IResourceLocation mappings
            lookupManager.instances.Clear();

            //Loop through all the required assets to find them in the newly loaded mod
            for (int i = 0; i < lookupManager.requiredAssets.Count; i++)
            {
                //Populate instance dictionary
                lookupManager.instances.Add(
                    lookupManager.requiredAssets[i],
                    FindAssetInMod(
                        lookupManager.requiredAssets[i], //Find an asset with this key
                        modDictionary[activatedMod] //Find the asset within this mod
                        )
                    );
            }

            //Call all instances
            for (int i = 0; i < modUpdateListeners.Count; i++)
            {
                modUpdateListeners[i](); //Call the function
            }
        }
    }

    //This uses the IResourceLocator stored within the mod to find the IResourceLocation of an asset with the key
    public IResourceLocation FindAssetInMod (string key, ModInfo mod)
    {
        //An IResourceLocation "contains enough information to load an asset (what/where/how/dependencies)" (Unity Docs)
        IList<IResourceLocation> locs;

        //Use the IResourceLocator.Locate function to find IResourceLocation
        if (mod.locator.Locate(key, typeof(object), out locs))
        {
            //Return the first location for singular asset
            return locs[0];
        }

        return null;
    }


    //Simply loops through mods and assigns them to dictionary
    private void ReloadDictionary()
    {
        modDictionary.Clear();

        for (int i = 0; i < mods.Count; i++)
        {
            modDictionary.Add(mods[i].modName, mods[i]);
        }

        //Populate UI

        for (int i = 0; i < buttons.Count; i++)
        {
            GameObject.Destroy(buttons[i].gameObject);
        }

        buttons.Clear();

        foreach (ModInfo info in mods)
        {
            //Instantiate a button using the buttonPrefab
            Button newButton = Instantiate(buttonPrefab, buttonParent);
            buttons.Add(newButton);

            //Add a listener to the button
            newButton.onClick.AddListener(() =>
            {
                //Call the ChangeMod function with the name of each mod
                ChangeMod(info.modName);
            });

            //Label the button
            newButton.GetComponentInChildren<Text>().text = info.modName;
        }
    }
}

//Struct to contain all mod information
[System.Serializable]
public struct ModInfo
{
    public string modName; //Name of Mod
    public string modAbsolutePath; //Absolute Path of Mod

    public FileInfo modFile; //File Information (file size etc.)

    public IResourceLocator locator; //Resource locator of the mod

    public bool isDefault; //Only the default mod pack will have this checked
}
