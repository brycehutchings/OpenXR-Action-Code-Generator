/*
 * TODO:
 * * Allow priority to default to 0
 * * Replace strcpy_s with something cross-platform
 * * Solve localizedName :-(
 * * Add error validations
 *      1. Name and Localized name match?
 *      2. Either name or localized name are empty.
 *      3. Duplication action names
 *      4. Duplicated bindings on an action for an interaction profile (may have forgotten to change left<-->right).
 *      5. Unsupported binding for known interaction profile
 *      6. Wrong binding type for action type 
 * * Add warning validations
 *      1. No actions in an action set
 *      2. Missing action bindings for an action for a interaction profile
 *      3. Unknown interaction profile
 *      4. Not using subaction path for pose
 * * Add more naming convention converters and wire to command line or manifest
 * * Use enum for subactions with json converter
 * * Support for looping over subaction paths for action state (e.g. for (auto subactionPath : {Left,Right}) {})
 * * Support for haptic helpers
 */

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenXRActionCodeGenerator
{
    enum ActionType
    {
        Boolean,
        Pose,
        Haptic,
        Float,
        Vector2f
    }

    enum TopLevelPath
    {
        Null,
        UserHandLeft,
        UserHandRight,
    }

    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: GenerateBindings <file.json>");
                return 1;
            }

            try
            {
                var manifestFile = args[0];
                var generatedFile = Path.GetFileNameWithoutExtension(manifestFile) + ".h";

                var options = new JsonSerializerOptions { AllowTrailingCommas = true, PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip };
                options.Converters.Add(new JsonStringEnumConverter());
                options.Converters.Add(new SuggestedBindingsJsonConverter());

                var res = JsonSerializer.Deserialize<ActionManifest>(File.ReadAllText(manifestFile), options);
                var namingConverter = new CamelCaseConverter();

                File.WriteAllText(generatedFile, CLibraryGenerator.Generate(res, namingConverter));
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unhandled exception: {ex}");
                return 1;
            }
        }
    }
}
