﻿using Newtonsoft.Json;

namespace XBMCRemoteRT.Models.Files
{
    public class Source
    {
        [JsonProperty(PropertyName = "file")]
        public string Path { get; set; }

        public string Label { get; set; }

        public string Media { get; set; }
    }
}
