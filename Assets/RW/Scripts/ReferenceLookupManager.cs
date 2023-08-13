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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class ReferenceLookupManager : MonoBehaviour
{
    public static ReferenceLookupManager instance;
    //The keys' of assets we want to pull from each mod (common names)
    public List<string> requiredAssets = new List<string>();

    //The resource locations of each of the assets we need
    public  Dictionary<string, IResourceLocation> instances = new Dictionary<string, IResourceLocation>();

    //Currently loaded objects within the scene (stored for releasing when mod is switched)
    private List<AsyncOperationHandle> loadedGameObjects = new List<AsyncOperationHandle>();

    private void Awake()
    {
        if (ReferenceLookupManager.instance == null)
        {
            instance = this;
        }
    }

    //Abstraction to allow for instantiation from anywhere in the game without worrying about the current mod
    public AsyncOperationHandle<GameObject> Instantiate(string key, Vector3 position, Vector3 facing, Transform parent)
    {
        //If the object key exists
        if (!instances.ContainsKey(key))
        {
            Debug.LogError("The object you are looking for doesn't exist in this mod pack.'");
        }

        //Instantiate with given parameters
        InstantiationParameters instParams = new InstantiationParameters(position,Quaternion.LookRotation(facing), parent);

        AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(instances[key], instParams, true);
        loadedGameObjects.Add(handle);

        return handle;
    }

    //When mod is switched, all current GameObjects are released
    public void ClearLoadedGameObjects()
    {
        foreach (AsyncOperationHandle handle in loadedGameObjects)
        {
            if (handle.Result != null)
            {
                Addressables.ReleaseInstance(handle);
            }
        }
        loadedGameObjects.Clear();
    }
}
