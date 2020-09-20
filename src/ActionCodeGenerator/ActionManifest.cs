using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenXRActionCodeGenerator
{

    class Action
    {
        public string Name { get; set; }
        public string LocalizedName { get; set; }
        public ActionType Type { get; set; }
        public bool UseSubactionPaths { get; set; }
    }

    class SuggestedBindings
    {
        public string InteractionProfile { get; set; }

        public Dictionary<string, IEnumerable<string>> Bindings { get; set; } = new Dictionary<string, IEnumerable<string>>();
    }

    // Dynamic property names matching the action names mrequires a json converter.
    class SuggestedBindingsJsonConverter : JsonConverter<SuggestedBindings>
    {
        public override SuggestedBindings Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Expected start of object in suggestedBindings array but got {reader.TokenType}");
            }


            var suggestedBindings = new SuggestedBindings();

            while (reader.Read())
            {
                if(reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException($"Expected property in suggestedBindings object but got {reader.TokenType}");
                }

                var propertyName = reader.GetString();

                if (propertyName == "interactionProfile")
                {
                    reader.Read();
                    suggestedBindings.InteractionProfile = reader.GetString();
                }
                else
                {
                    var bindings = JsonSerializer.Deserialize<IEnumerable<string>>(ref reader);
                    suggestedBindings.Bindings.Add(propertyName, bindings);
                }
            }

            if (string.IsNullOrEmpty(suggestedBindings.InteractionProfile))
            {
                throw new JsonException($"Expected non-empty string for suggestedBindings property 'interactionProfile'");
            }

            return suggestedBindings;
        }

        public override void Write(Utf8JsonWriter writer, SuggestedBindings value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }

    class ActionSet
    {
        public string Name { get; set; }

        public string LocalizedName { get; set; }

        public int Priority { get; set; }

        public IEnumerable<Action> Actions { get; set; }

        public IEnumerable<SuggestedBindings> SuggestedBindings { get; set; }
    }

    struct ActionManifest
    {
        public IEnumerable<ActionSet> ActionSets { get; set; }
    }
}
