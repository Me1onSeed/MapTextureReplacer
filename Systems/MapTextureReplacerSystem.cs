﻿using Colossal.Json;
using Game;
using Game.Notifications;
using Game.UI;
using MapTextureReplacer.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MapTextureReplacer.Systems
{
    public class MapTextureReplacerSystem : GameSystemBase
    {
        public string PackImportedText = "";

        private Dictionary<string, string> importedPacks = new Dictionary<string, string>();
        public string importedPacksJsonString;
        
        public string textureSelectDataJsonString;
        private List<KeyValuePair<string, string>> textureSelectData = new List<KeyValuePair<string, string>>() {
            new KeyValuePair<string, string>("Select Image", ""),
            new KeyValuePair<string, string>("Select Image", ""),
            new KeyValuePair<string, string>("Select Image", ""),
            new KeyValuePair<string, string>("Select Image", ""),
            new KeyValuePair<string, string>("Select Image", ""),
            new KeyValuePair<string, string>("Select Image", ""),
        };

        static Dictionary<string, Texture> mapTextureCache = new Dictionary<string, Texture>();
       
        static readonly Dictionary<string, string> textureTypes = new Dictionary<string, string>() {
            {"colossal_TerrainGrassDiffuse", "Grass_BaseColor.png"},
            {"colossal_TerrainGrassNormal", "Grass_Normal.png"},
            {"colossal_TerrainDirtDiffuse", "Dirt_BaseColor.png"},
            {"colossal_TerrainDirtNormal", "Dirt_Normal.png"},
            {"colossal_TerrainRockDiffuse", "Cliff_BaseColor.png"},
            {"colossal_TerrainRockNormal", "Cliff_Normal.png"},
        };

        protected override void OnCreate()
        {
            base.OnCreate();

            //cache original textures for reset function
            foreach (var item in textureTypes)
            {
                CacheExistingTexture(item.Key);
            }

            List<string> texturePackFolders = new List<string>();

            DirectoryInfo modsFolderDirectory = Directory.GetParent(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

            //find folders that contain pack config json files
            foreach (string filePath in Directory.GetFiles(modsFolderDirectory.FullName, "*.json", SearchOption.AllDirectories))
            {
                var filename = Path.GetFileName(filePath);              
                if (filename == "maptextureconfig.json")
                {            
                    texturePackFolders.Add(Directory.GetParent(filePath).FullName);
                }
            }

            //read pack config json files in the folders that have them
            foreach (var folder in texturePackFolders)
            {
                foreach (string filePath in Directory.GetFiles(folder))
                {
                    var filename = Path.GetFileName(filePath);
                    if (filename == "maptextureconfig.json")
                    {
                        MapTextureConfig mapTheme = JsonConvert.DeserializeObject<MapTextureConfig>(File.ReadAllText(filePath));
                        importedPacks.Add(filePath, mapTheme.pack_name);
                    }
                }
            }
            importedPacksJsonString = JsonConvert.SerializeObject(importedPacks);

            //populate string
            textureSelectDataJsonString = JsonConvert.SerializeObject(textureSelectData);
        }

        protected override void OnUpdate()
        {

        }
        public void ChangePack(string current)
        {

            if (current == "none")
            {
                foreach (var item in textureTypes)
                {
                    ResetTexture(item.Key);
                }
                SetTilingValueDefault();
                //SetSelectImageAllText("Select Image");
            }
            else
            {
                if (current.EndsWith(".zip"))
                {
                    OpenTextureZip(current.Split(',')[1]);
                }
                else if (current.EndsWith(".json"))
                {
                    var directory = Path.GetDirectoryName(current);
                    UnityEngine.Debug.Log("preloaded folder? " + directory);

                    foreach (string filePath in Directory.GetFiles(directory))
                    {
                        foreach (var item in textureTypes)
                        {
                            LoadImageFile(filePath, item.Value, item.Key);
                        }
                    }

                    MapTextureConfig config = JsonConvert.DeserializeObject<MapTextureConfig>(File.ReadAllText(current));
                    SetTilingValues(config.far_tiling, config.close_tiling, config.close_dirt_tiling);                   
                    SetSelectImageAllText(config.pack_name);
                }
            }
        }

        private void SetSelectImageAllText(string key)
        {
            //set select image text labels
            for (int i = 0; i < textureSelectData.Count; i++)
            {
                textureSelectData[i] = new KeyValuePair<string, string>(key, "");
                //textureSelectData[i].Value
            }
            textureSelectDataJsonString = JsonConvert.SerializeObject(textureSelectData);
        }

        public void SetTilingValues(string far, string close, string dirtClose)
        {
            Shader.SetGlobalVector(Shader.PropertyToID("colossal_TerrainTextureTiling"), new Vector4(float.Parse(far), float.Parse(close), float.Parse(dirtClose), 1f));
        }
        public void SetTilingValueDefault()
        {
            Shader.SetGlobalVector(Shader.PropertyToID("colossal_TerrainTextureTiling"), new Vector4(160f, 1600f, 2400f, 1f));
        }
        private static void LoadImageFile(string filePath, string textureFile, string shaderProperty)
        {
            if (Path.GetFileName(filePath) == textureFile)
            {
                byte[] data = File.ReadAllBytes(filePath);

                LoadTextureInGame(shaderProperty, data);
            }
        }

        public void OpenImage(string shaderProperty, string packPath)
        {
            if (packPath == "")
            {
                var file = OpenFileDialog.ShowDialog("Image files\0*.jpg;*.png\0");

                byte[] fileData;

                if (!string.IsNullOrEmpty(file))
                {
                    fileData = File.ReadAllBytes(file);
                    LoadTextureInGame(shaderProperty, fileData);

                    int index = textureTypes.Keys.ToList().IndexOf(shaderProperty);
                    string fileName = Path.GetFileName(file);
                    if (fileName.Length > 15)
                    {
                        fileName = fileName.Substring(0, 15);
                    }
                    textureSelectData[index] = new KeyValuePair<string, string>(fileName, file);
                    textureSelectDataJsonString = JsonConvert.SerializeObject(textureSelectData);
                }
            }
            else if (packPath.EndsWith(".zip"))
            {
                var filenameTexture = "";
                foreach (var item in textureTypes)
                {
                    if (item.Key == shaderProperty)
                    {
                        filenameTexture = item.Value;
                    }
                }

                using (ZipArchive archive = ZipFile.Open(packPath.Split(',')[1], ZipArchiveMode.Read))
                {
                    ExtractEntry(archive, filenameTexture, shaderProperty);
                }
            }
            else
            {
                var directory = Path.GetDirectoryName(packPath);

                var filename = "";
                foreach (var item in textureTypes)
                {
                    if (item.Key == shaderProperty)
                    {
                        filename = item.Value;
                    }
                }

                foreach (string filePath in Directory.GetFiles(directory))
                {
                    LoadImageFile(filePath, filename, shaderProperty);
                }
            }
        }
        public void GetTextureZip()
        {
            var zipFilePath = OpenFileDialog.ShowDialog("Zip archives\0*.zip\0");
            PackImportedText = Path.GetFileNameWithoutExtension(zipFilePath) + "," + zipFilePath;
        }
        public void OpenTextureZip(string zipFilePath)
        {
            //var zipFilePath = OpenFileDialog.ShowDialog("Zip archives\0*.zip\0");

            if (!string.IsNullOrEmpty(zipFilePath))
            {
                using (ZipArchive archive = ZipFile.Open(zipFilePath, ZipArchiveMode.Read))
                {
                    ExtractEntry(archive, "Grass_BaseColor.png", "colossal_TerrainGrassDiffuse");
                    ExtractEntry(archive, "Grass_Normal.png", "colossal_TerrainGrassNormal");
                    ExtractEntry(archive, "Dirt_BaseColor.png", "colossal_TerrainDirtDiffuse");
                    ExtractEntry(archive, "Dirt_Normal.png", "colossal_TerrainDirtNormal");
                    ExtractEntry(archive, "Cliff_BaseColor.png", "colossal_TerrainRockDiffuse");
                    ExtractEntry(archive, "Cliff_Normal.png", "colossal_TerrainRockNormal");
                }
            }
        }
        private static void CacheExistingTexture(string shaderProperty)
        {
            var existingTexture = Shader.GetGlobalTexture(Shader.PropertyToID(shaderProperty));
            if (!mapTextureCache.ContainsKey(shaderProperty))
            {
                mapTextureCache.Add(shaderProperty, existingTexture);
            }
        }

        private static void LoadTextureInGame(string shaderProperty, byte[] fileData)
        {
            Texture2D newTexture = new Texture2D(4096, 4096);
            newTexture.LoadImage(fileData);
            Shader.SetGlobalTexture(Shader.PropertyToID(shaderProperty), newTexture);
            Debug.Log("Replaced " + shaderProperty + " ingame");
        }

        public void ResetTexture(string shaderProperty)
        {
            mapTextureCache.TryGetValue(shaderProperty, out Texture texture);
            if (texture != null)
            {
                Shader.SetGlobalTexture(Shader.PropertyToID(shaderProperty), texture);
            }
            //reset neighboring button text to select image
            int index = textureTypes.Keys.ToList().IndexOf(shaderProperty);
            textureSelectData[index] = new KeyValuePair<string, string>("Select Image", "");
            textureSelectDataJsonString = JsonConvert.SerializeObject(textureSelectData);
        }

        private static void ExtractEntry(ZipArchive archive, string entryName, string shaderProperty)
        {
            ZipArchiveEntry entry = archive.GetEntry(entryName);

            if (entry != null)
            {
                using (Stream entryStream = entry.Open())
                {
                    byte[] data = new byte[entry.Length];
                    entryStream.Read(data, 0, data.Length);
                    LoadTextureInGame(shaderProperty, data);
                }
            }
        }

        public void SetTile(int v)
        {
            UnityEngine.Debug.Log("SetTile Pressed!");
            UnityEngine.Debug.Log("BF colossal_TerrainTextureTiling: " + Shader.GetGlobalVector(Shader.PropertyToID("colossal_TerrainTextureTiling")));
            Shader.SetGlobalVector(Shader.PropertyToID("colossal_TerrainTextureTiling"), new Vector4(new System.Random().Next(0, 10000), new System.Random().Next(0, 10000), new System.Random().Next(0, 10000), 1f));


        }

        public void ResetTextureSelectData()
        {

            Debug.Log("bf2: " + textureSelectDataJsonString);

            textureSelectData = new List<KeyValuePair<string, string>>() {
            new KeyValuePair<string, string>("Select Image", ""),
            new KeyValuePair<string, string>("Select Image", ""),
            new KeyValuePair<string, string>("Select Image", ""),
            new KeyValuePair<string, string>("Select Image", ""),
            new KeyValuePair<string, string>("Select Image", ""),
            new KeyValuePair<string, string>("Select Image", ""),
            };

            textureSelectDataJsonString = JsonConvert.SerializeObject(textureSelectData);

            Debug.Log("af2: " + textureSelectDataJsonString);
        }
    }
}
