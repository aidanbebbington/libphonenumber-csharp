using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PhoneNumbers;
using PhoneNumbers.Carrier;

namespace BuildTools
{
    /**
 * A utility that generates the binary serialization of the phone prefix mappings from
 * human-readable text files. It also generates a configuration file which contains inFormation on
 * data files available for use.
 *
 * <p> The text files must be located in sub-directories of the provided input path. For each input
 * file inputPath/lang/countryCallingCode.txt the corresponding binary file is generated as
 * outputPath/countryCallingCode_lang.
 */
    internal class PhonePrefixData
    {
        public static async Task<bool> Generate()
        {
            try
            {
                await Thing();
                return true
            }
            catch (Exception)
            {
                return false;
            }
        }


        // The path to the input directory containing the languages directories.
        private string inputPath;

        private static int NANPA_COUNTRY_CODE = 1;

        // Pattern used to match the language code contained in the input text file path. This may be a
        // two-letter code like fr, or a three-letter code like ban, or a code containing script
        // inFormation like zh_Hans (simplified Chinese).
        private static Regex LANGUAGE_IN_FILE_PATH_PATTERN =
            new Regex("(.*/)(?:[a-zA-Z_]+)(/\\d+\\.txt)", RegexOptions.Compiled);

        // Dictionary used to store the English mappings to avoid reading the English text files multiple times.
        private Dictionary<int /* country code */, SortedDictionary<int, string>> englishDictionaries =
            new Dictionary<int, SortedDictionary<int, string>>();

        // The IO Handler used to output the generated binary files.
        private AbstractPhonePrefixDataIOHandler ioHandler;

        public PhonePrefixData(string inputPath, AbstractPhonePrefixDataIOHandler ioHandler)
        {
            if (!Directory.Exists(inputPath))
            {
                throw new IOException($"The provided input path does not exist: {inputPath}");
            }

            this.inputPath = inputPath;
            this.ioHandler = ioHandler;
        }

        /**
     * Implement this interface to provide a callback to the ParseTextFile() method.
     */
        internal interface PhonePrefixmappingHandler
        {
            /**
         * Method called every time the parser matches a mapping. Note that 'prefix' is the prefix as
         * it is written in the text file (i.e phone number prefix appended to country code).
         */
            internal void process(int prefix, string location);
        }

        /**
     * Reads phone prefix data from the provided input stream and invokes the given handler for each
     * mapping read.
     */
        internal static async Task ParseTextFile(Stream input,
            Action<int, string> handler)
        {
            var bufferedReader = new StreamReader(input);
            var lineNumber = 1;

            for (string line; (line = await bufferedReader.ReadLineAsync()) != null; lineNumber++)
            {
                line = line.Trim();
                if (line.Length == 0 || line.StartsWith("#"))
                {
                    continue;
                }

                var IndexOfPipe = line.IndexOf('|');
                if (IndexOfPipe == -1)
                {
                    throw new Exception("line %d: malFormatted data, expected '|'");
                }

                var prefix = line.Substring(0, IndexOfPipe);
                var location = line.Substring(IndexOfPipe + 1);
                handler.process(int.Parse(prefix), location);
            }
        }

        /**
     * Writes the provided phone prefix Dictionary to the provided output stream.
     *
     * @throws IOException
     */
        // @VisibleForTesting
        static void writeToBinaryFile(ImmutableSortedDictionary<int, string> sortedDictionary, Stream output)
        {
            // Build the corresponding phone prefix Dictionary and serialize it to the binary Format.
            var phonePrefixMap = new PhonePrefixMap();
            phonePrefixMap.ReadPhonePrefixMap(sortedDictionary);
            Stream objectStream = new FileStream(output);
            phonePrefixMap.WriteExternal(objectStream);
            objectStream.Flush();
        }

        /**
     * Reads the mappings contained in the provided input stream pointing to a text file.
     *
     * @return    a Dictionary containing the mappings that were read
     */
        internal static ImmutableSortedDictionary<int, string> ReadMappingsFromTextFile(Stream input)
        {
            var phonePrefixMap = new Dictionary<int, string>();
            ParseTextFile(input, ((prefix, location) => phonePrefixMap.Add(prefix, location)));
            return phonePrefixMap.ToImmutableSortedDictionary();
        }

        private class PhonePrefixLanguagePair
        {
            public readonly string Prefix;
            public readonly string Language;

            internal PhonePrefixLanguagePair(string prefix, string language)
            {
                Prefix = prefix;
                Language = language;
            }
        }

        private static string GenerateBinaryFilename(int prefix, string lang)
            => $"{prefix}_{lang}";

        /**
     * Extracts the phone prefix and the language code contained in the provided file name.
     */
        private static PhonePrefixLanguagePair GetPhonePrefixLanguagePairFromFilename(string filename)
        {
            var indexOfUnderscore = filename.IndexOf('_');
            var prefix = filename.Substring(0, indexOfUnderscore);
            var language = filename.Substring(indexOfUnderscore + 1);
            return new PhonePrefixLanguagePair(prefix, language);
        }

        /**
     * Method used by {@code #createInputOutputMappings()} to generate the list of output binary files
     * from the provided input text file. For the data files expected to be large (currently only
     * NANPA is supported), this method generates a list containing one output file for each area
     * code. Otherwise, a single file is added to the list.
     */
        private List<string> CreateOutputFiles(string countryCodeFile, int countryCode, string language)
        {
            var outputFiles = new List<string>();
            // For NANPA, split the data into multiple binary files.
            if (countryCode == NANPA_COUNTRY_CODE)
            {
                // Fetch the 4-digit prefixes stored in the file.
                var phonePrefixes = new HashSet<int>();
                var stream = new FileStream(countryCodeFile);
                ParseTextFile(stream, (prefix, location) => phonePrefixes.Add(int.Parse(prefix.ToString().Substring(0, 4))));
                outputFiles.AddRange(phonePrefixes.Select(prefix => ioHandler.createFile(GenerateBinaryFilename(prefix, language))).Select(dummy => (string) dummy));
            }
            else
            {
                outputFiles.Add(ioHandler.createFile(GenerateBinaryFilename(countryCode, language)));
            }

            return outputFiles;
        }

        /**
     * Returns the country code extracted from the provided text file name expected as
     * [1-9][0-9]*.txt.
     *
     * @throws RuntimeException if the file path is not Formatted as expected
     */
        private static int getCountryCodeFromTextFileName(string filename)
        {
            var IndexOfDot = filename.IndexOf('.');
            if (IndexOfDot < 1)
            {
                throw new Exception($"unexpected file name {filename}, expected pattern [1-9][0-9]*.txt");
            }

            var countryCode = filename.Substring(0, IndexOfDot);
            return int.Parse(countryCode);
        }

        /**
     * Generates the mappings between the input text files and the output binary files.
     *
     * @throws IOException
     */
        private Dictionary<string, List<string>> createInputOutputmappings()
        {
            var mappings = new Dictionary<string, List<string>>();
            var languageDirectories = Directory.EnumerateDirectories(inputPath).ToImmutableSortedSet();
            // Make sure that filenames are processed in the same order build-to-build.
            //Arrays.sort(languageDirectories);

            foreach (var languageDirectory in languageDirectories)
            {

                var countryCodeFiles = Directory.GetFiles(languageDirectory).ToImmutableSortedSet();

                foreach (var countryCodeFile in
                    countryCodeFiles)
                {
                    var countryCodeFileName = countryCodeFile;
                    var outputFiles = CreateOutputFiles(
                        countryCodeFile, getCountryCodeFromTextFileName(countryCodeFileName),
                        languageDirectory);
                    mappings.Add(countryCodeFile, outputFiles);
                }
            }

            return mappings;
        }

        /**
     * Adds a phone number prefix/language mapping to the provided Dictionary. The prefix and language are
     * generated from the provided file name previously used to output the phone prefix mappings for
     * the given country.
     */
        internal static void AddConfigurationMapping(SortedDictionary<int, HashSet<string>> availableDataFiles,
            string outputPhonePrefixMappingsFile)
        {
            var outputPhonePrefixMappingsFileName = outputPhonePrefixMappingsFile;
            var phonePrefixLanguagePair =
                GetPhonePrefixLanguagePairFromFilename(outputPhonePrefixMappingsFileName);
            var prefix = int.Parse(phonePrefixLanguagePair.Prefix);
            var language = phonePrefixLanguagePair.Language;
            var languageHashSet = availableDataFiles[prefix];
            if (languageHashSet == null)
            {
                languageHashSet = new HashSet<string>();
                availableDataFiles.Add(prefix, languageHashSet);
            }

            languageHashSet.Add(language);
        }

        /**
     * Outputs the binary configuration file mapping country codes to language strings.
     */
        // @VisibleForTesting
        static void outputBinaryConfiguration(
            ImmutableSortedDictionary<int, ImmutableHashSet<string>> availableDataFiles, Stream stream)
        {
            var mappingFileProvider = new MappingFileProvider();
            mappingFileProvider.ReadFileConfigs(availableDataFiles);
            mappingFileProvider.WriteExternal(stream);
            stream.Flush();
        }

        /**
     * Splits the provided phone prefix Dictionary into multiple Dictionarys according to the provided list of
     * output binary files. A Dictionary associating output binary files to phone prefix Dictionarys is returned as
     * a result.
     * <pre>
     * Example:
     *     input Dictionary: { 12011: Description1, 12021: Description2 }
     *     outputBinaryFiles: { 1201_en, 1202_en }
     *     output Dictionary: { 1201_en: { 12011: Description1 }, 1202_en: { 12021: Description2 } }
     * </pre>
     */
        // @VisibleForTesting
        static Dictionary<string, SortedDictionary<int, string>> splitDictionary(
            SortedDictionary<int, string> mappings, List<string> outputBinaryFiles)
        {
            var mappingsForFiles =
                new Dictionary<string, SortedDictionary<int, string>>();
            foreach (var mapping in mappings)
            {
                var prefix = mapping.Key.ToString();
                var targetFile = outputBinaryFiles
                    .Select(outputBinaryFile => new
                    {
                        outputBinaryFile,
                        outputBinaryFilePrefix = GetPhonePrefixLanguagePairFromFilename(outputBinaryFile).Prefix
                    })
                    .Where(t => prefix.StartsWith(t.outputBinaryFilePrefix))
                    .Select(t => t.outputBinaryFile).FirstOrDefault();

                var mappingsForPhonePrefixLangPair = mappingsForFiles[targetFile];
                if (mappingsForPhonePrefixLangPair == null)
                {
                    mappingsForPhonePrefixLangPair = new SortedDictionary<int, string>();
                    mappingsForFiles.Add(targetFile, mappingsForPhonePrefixLangPair);
                }

                mappingsForPhonePrefixLangPair.Add(mapping.Key, mapping.Value);
            }

            return mappingsForFiles;
        }

        /**
     * Gets the English data text file path corresponding to the provided one.
     */
        // @VisibleForTesting
        static string getEnglishDataPath(string inputTextFileName)
        {
            return LANGUAGE_IN_FILE_PATH_PATTERN.Matcher(inputTextFileName).replaceFirst("$1en$2");
        }

        /**
     * Tests whether any prefix of the given number overlaps with any phone number prefix contained in
     * the provided Dictionary.
     */
        // @VisibleForTesting
        static bool hasOverlappingPrefix(int number, SortedDictionary<int, string> mappings)
        {
            while (number > 0)
            {
                number /= 10;
                if (mappings[number] != null)
                {
                    return true;
                }
            }

            return false;
        }

        /**
     * Compresses the provided non-English Dictionary according to the English Dictionary provided. For each mapping
     * which is contained in both Dictionarys with a same description this method either:
     * <ul>
     *    <li> Removes from the non-English Dictionary the mapping whose prefix does not overlap with an
     *             existing prefix in the Dictionary, or;
     *    <li> Keeps this mapping in both Dictionarys but makes the description an empty string in the
     *             non-English Dictionary.
     * </ul>
     */
        // @VisibleForTesting
        static void compressAccordingToEnglishData(
            SortedDictionary<int, string> englishDictionary, SortedDictionary<int, string> nonEnglishDictionary)
        {
            var it = nonEnglishDictionary.GetEnumerator();
            while (it.MoveNext())
            {
                var entry = it.Current;
                var prefix = entry.Key;
                var englishDescription = englishDictionary[prefix];
                if (englishDescription != null && englishDescription == entry.Value)
                {
                    if (!hasOverlappingPrefix(prefix, nonEnglishDictionary))
                    {
                        nonEnglishDictionary.Remove(entry.Key); // is this going to fail?
                    }
                    else
                    {
                        nonEnglishDictionary.Add(prefix, "");
                    }
                }
            }
        }

        /**
     * Compresses the provided mappings according to the English data file if any.
     *
     * @throws IOException
     */
        private void makeDataFallbackToEnglish(string inputTextFile, SortedDictionary<int, string> mappings)
        {
            var englishTextFile = getEnglishDataPath(Path.GetFullPath(inputTextFile));
            if (Path.GetFullPath(inputTextFile) == Path.GetFullPath(englishTextFile)
                || !englishTextFile.exists())
            {
                return;
            }

            var countryCode = getCountryCodeFromTextFileName(inputTextFile.getName());
            var englishDictionary = englishDictionaries[countryCode];
            if (englishDictionary == null)
            {
                FileStream englishFileStream = null;
                try
                {
                    englishFileStream = new FileStream(englishTextFile);
                    englishDictionary = ReadMappingsFromTextFile(englishFileStream);
                    englishDictionaries.Add(countryCode, englishDictionary);
                }
                finally
                {
                    ioHandler.closeFile(englishFileStream);
                }
            }

            compressAccordingToEnglishData(englishDictionary, mappings);
        }

        /**
     * Removes the empty-description mappings in the provided Dictionary if the language passed-in is "en".
     */
        // @VisibleForTesting
        static void removeEmptyEnglishmappings(SortedDictionary<int, string> dictionary, string lang)
        {
            if (lang != "en")
            {
                return;
            }

            var it = dictionary.GetEnumerator();
            while (it.MoveNext())
            {
                if (string.IsNullOrEmpty(it.Current.Value))
                {
                    dictionary.Remove(it.Current.Key);
                }
            }
        }


        public void run()
        {
            var inputOutputMappings = createInputOutputmappings();

            var availableDataFiles = new SortedDictionary<int, HashSet<string>>();
            Stream fileInputStream = null;
            Stream fileOutputStream = null;

            foreach (var inputOutputMapping in inputOutputMappings)
            {

                try
                {
                    var textFile = inputOutputMapping.Key;
                    var outputBinaryFiles = inputOutputMapping.Value;
                    fileInputStream = new FileStream(textFile);
                    var mappings = ReadMappingsFromTextFile(fileInputStream);
                    removeEmptyEnglishmappings(mappings, textFile.getParentFile().getName());
                    makeDataFallbackToEnglish(textFile, mappings);
                    Dictionary<string, SortedDictionary<int, string>> mappingsForFiles =
                        splitDictionary(mappings, outputBinaryFiles);

                    foreach (var mappingsForFile in mappingsForFiles)
                    {
                        var outputBinaryFile = mappingsForFile.Key;
                        fileOutputStream = null;
                        try
                        {
                            fileOutputStream = new FileStream(outputBinaryFile);
                            writeToBinaryFile(mappingsForFile.Value, fileOutputStream);
                            AddConfigurationMapping(availableDataFiles, outputBinaryFile);
                            ioHandler.addFileToOutput(outputBinaryFile);
                        }
                        finally
                        {
                            ioHandler.closeFile(fileOutputStream);
                        }
                    }
                }
                catch (Exception)
                {
                }
                finally
                {
                    ioHandler.closeFile(fileInputStream);
                    ioHandler.closeFile(fileOutputStream);
                }
            }

            // Output the binary configuration file mapping country codes to languages.
            fileOutputStream = null;
            try
            {
                var configFile = ioHandler.createFile("config");
                fileOutputStream = new FileStream(configFile);
                outputBinaryConfiguration(availableDataFiles, fileOutputStream);
                ioHandler.addFileToOutput(configFile);
            }
            finally
            {
                ioHandler.closeFile(fileOutputStream);
                ioHandler.close();
            }
        }
    }
}
