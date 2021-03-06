﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using CasualMeter.Common.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tera.Data;
using Tera.Game;

namespace CasualMeter.Common.Helpers
{

    class IPAddressConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(IPAddress));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            IPAddress ip = (IPAddress)value;
            writer.WriteValue(ip.ToString());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JToken token = JToken.Load(reader);

            return token.Value<string>() != null ? IPAddress.Parse(token.Value<string>()) : null;
        }
    }

    public sealed class SettingsHelper
    {
        private static readonly Lazy<SettingsHelper> Lazy = new Lazy<SettingsHelper>(() => new SettingsHelper());
        
        public readonly BasicTeraData BasicTeraData;
        private readonly Dictionary<PlayerClass, string> _classIcons;
        
        public static SettingsHelper Instance => Lazy.Value;

        private static readonly string SettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CasualMeter");
        private static readonly string ConfigFilePath = Path.Combine(SettingsPath, "settings.json");

        private readonly JsonSerializerSettings _jsonSerializerSettings ;

        public Settings Settings { get; set; }

        private SettingsHelper()
        {
            Directory.CreateDirectory(SettingsPath);//ensure settings directory is created
            _classIcons = new Dictionary<PlayerClass, string>();
            _jsonSerializerSettings = new JsonSerializerSettings();
            _jsonSerializerSettings.DefaultValueHandling = DefaultValueHandling.Populate;
            _jsonSerializerSettings.Converters.Add(new IPAddressConverter());

            BasicTeraData = new BasicTeraData(SettingsPath);
            LoadClassIcons();
            Load();
        }

        public void Initialize()
        {
            //empty method to ensure initialization
        }

        private void LoadClassIcons()
        {
            var directory = Path.Combine(BasicTeraData.ResourceDirectory, @"class-icons");
            foreach (var playerClass in (PlayerClass[]) Enum.GetValues(typeof (PlayerClass)))
            {
                var filename = Path.Combine(directory, playerClass.ToString().ToLowerInvariant() + ".png");
                _classIcons.Add(playerClass, filename);
            }
        }

        private void Load()
        {
            if (File.Exists(ConfigFilePath))
            {   //load settings if the file exists
                try
                {
                    Settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(ConfigFilePath),
                        _jsonSerializerSettings);
                }
                catch (JsonSerializationException)
                {
                    //someone fucked up their settings...
                    Settings = new Settings();
                }
            }
            else
            {   //no saved settings found
                Settings = new Settings();
            }
            Save();
        }

        public void Save()
        {
            File.WriteAllText(ConfigFilePath, JsonConvert.SerializeObject(Settings, Formatting.Indented, _jsonSerializerSettings));
        }

        public string GetImage(PlayerClass @class)
        {
            return _classIcons[@class];
        }
    }
}
