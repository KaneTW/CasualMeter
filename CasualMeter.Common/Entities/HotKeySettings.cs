﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CasualMeter.Common.Entities
{
    public class HotKeySettings : DefaultValueEntity
    {
        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(ModifierKeys.Control)]
        public ModifierKeys Modifier { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(Key.Insert)]
        public Key PasteStats { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(Key.Delete)]
        public Key Reset { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(Key.End)]
        public Key SaveAndReset { get; set; }
    }
}
