//-----------------------------------------------------------------------
// <copyright file="JsonExtensionObject.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NJsonSchema
{
    /// <summary>The base JSON class with extension data.</summary>
    [JsonConverter(typeof(ExtensionDataDeserializationConverter))]
    public class JsonExtensionObject : IJsonExtensionObject
    {
        /// <summary>Gets or sets the extension data (i.e. additional properties which are not directly defined by the JSON object).</summary>
        [JsonExtensionData]
        public IDictionary<string, object> ExtensionData { get; set; }
    }

    /// <summary>Deserializes all JSON Schemas in the extension data property.</summary>
    internal class ExtensionDataDeserializationConverter : JsonConverter
    {
        public override bool CanRead => true;

        public override bool CanWrite => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var obj = (IJsonExtensionObject)Activator.CreateInstance(objectType);
            serializer.Populate(reader, obj);
            DeserializeExtensionDataSchemas(obj, serializer);
            return obj;
        }

        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        /// <summary>Transforms the extension data so that contained schemas are correctly deserialized.</summary>
        /// <param name="serializer">The serializer.</param>
        internal void DeserializeExtensionDataSchemas(IJsonExtensionObject extensionObject, JsonSerializer serializer)
        {
            if (extensionObject.ExtensionData != null)
            {
                foreach (var pair in extensionObject.ExtensionData.ToArray())
                    extensionObject.ExtensionData[pair.Key] = TryDeserializeValueSchemas(pair.Value, serializer);
            }
        }

        private object TryDeserializeValueSchemas(object value, JsonSerializer serializer)
        {
            var obj = value as JObject;
            if (obj != null)
            {
                var isSchema = obj.Property("type") != null || obj.Property("properties") != null;
                if (isSchema)
                {
                    try
                    {
                        return obj.ToObject<JsonSchema4>(serializer);
                    }
                    catch
                    {
                        // object was probably not a JSON Schema
                    }
                }

                var dictionary = new Dictionary<string, object>();
                foreach (var property in obj.Properties())
                    dictionary[property.Name] = TryDeserializeValueSchemas(property.Value, serializer);

                return dictionary;
            }

            var array = value as JArray;
            if (array != null)
                return array.Select(i => TryDeserializeValueSchemas(i, serializer)).ToArray();

            var token = value as JValue;
            if (token != null)
                return token.Value;

            return value;
        }
    }
}