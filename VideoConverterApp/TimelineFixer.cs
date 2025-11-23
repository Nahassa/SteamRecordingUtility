using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VideoConverterApp
{
    public class TimelineFixer
    {
        public class FixResult
        {
            public int Fixed { get; set; }
            public int AlreadyValid { get; set; }
            public int Errors { get; set; }
            public List<string> Messages { get; } = new List<string>();
        }

        /// <summary>
        /// Fix Steam timeline JSON files in the specified input folder.
        /// Looks in 'timelines/' and 'clips/*/timelines/' subfolders.
        /// </summary>
        public static FixResult FixTimelines(string inputFolder, Action<string, Color>? logCallback = null)
        {
            var result = new FixResult();

            void Log(string message, Color color)
            {
                result.Messages.Add(message);
                logCallback?.Invoke(message, color);
            }

            Log("Steam Timeline Fixer", Color.Black);
            Log("====================", Color.Black);
            Log($"Scanning: {inputFolder}", Color.Black);
            Log("", Color.Black);

            // Collect all timeline JSON files
            var jsonFiles = new List<string>();

            // Check 'timelines/' folder
            string timelinesFolder = Path.Combine(inputFolder, "timelines");
            if (Directory.Exists(timelinesFolder))
            {
                jsonFiles.AddRange(Directory.GetFiles(timelinesFolder, "*.json"));
            }

            // Check 'clips/*/timelines/' folders
            string clipsFolder = Path.Combine(inputFolder, "clips");
            if (Directory.Exists(clipsFolder))
            {
                foreach (var clipDir in Directory.GetDirectories(clipsFolder))
                {
                    string clipTimelinesFolder = Path.Combine(clipDir, "timelines");
                    if (Directory.Exists(clipTimelinesFolder))
                    {
                        jsonFiles.AddRange(Directory.GetFiles(clipTimelinesFolder, "*.json"));
                    }
                }
            }

            if (jsonFiles.Count == 0)
            {
                Log("No JSON files found in timelines folders", Color.Orange);
                return result;
            }

            Log($"Found {jsonFiles.Count} JSON file(s) to check", Color.Black);
            Log("", Color.Black);

            foreach (var filePath in jsonFiles)
            {
                string fileName = Path.GetFileName(filePath);
                Log($"Checking: {fileName}", Color.Black);

                try
                {
                    string jsonContent = File.ReadAllText(filePath);
                    var jsonObj = JObject.Parse(jsonContent);

                    if (jsonObj.TryGetValue("entries", out JToken? entriesToken))
                    {
                        if (entriesToken is JObject entriesObj)
                        {
                            // entries is an object - needs to be converted to array
                            Log("  Status: INVALID - entries is an object, converting to array...", Color.Orange);

                            // Create backup
                            string backupPath = filePath + ".bak";
                            File.Copy(filePath, backupPath, true);
                            Log($"  Backup created: {Path.GetFileName(backupPath)}", Color.Gray);

                            // Convert object to array (sorted by key)
                            var entriesArray = new JArray();
                            var sortedProperties = entriesObj.Properties()
                                .OrderBy(p => p.Name)
                                .Select(p => p.Value);

                            foreach (var value in sortedProperties)
                            {
                                entriesArray.Add(value);
                            }

                            jsonObj["entries"] = entriesArray;

                            // Write fixed JSON
                            string fixedJson = jsonObj.ToString(Formatting.Indented);
                            File.WriteAllText(filePath, fixedJson);

                            Log("  Result: FIXED (original backed up to .bak)", Color.Green);
                            result.Fixed++;
                        }
                        else if (entriesToken is JArray)
                        {
                            // entries is already an array - OK
                            Log("  Status: OK - entries is already an array", Color.Green);
                            result.AlreadyValid++;
                        }
                        else
                        {
                            // Unknown type
                            Log($"  Status: UNKNOWN - entries type: {entriesToken.Type}", Color.Gray);
                            result.AlreadyValid++;
                        }
                    }
                    else
                    {
                        // No entries field
                        Log("  Status: SKIPPED - no entries field found", Color.Gray);
                        result.AlreadyValid++;
                    }
                }
                catch (Exception ex)
                {
                    Log($"  Status: ERROR - {ex.Message}", Color.Red);
                    result.Errors++;
                }

                Log("", Color.Black);
            }

            // Summary
            Log("====================", Color.Black);
            Log("Summary:", Color.Black);
            Log($"  Fixed: {result.Fixed}", Color.Black);
            Log($"  Already valid: {result.AlreadyValid}", Color.Black);
            Log($"  Errors: {result.Errors}", Color.Black);

            if (result.Fixed > 0)
            {
                Log("", Color.Black);
                Log("Original files backed up with .bak extension", Color.Gray);
                Log("Remember to restart Steam for changes to take effect!", Color.Orange);
            }

            return result;
        }
    }
}
