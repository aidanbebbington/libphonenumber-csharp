/*
 * Copyright (C) 2011 The Libphonenumber Authors
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
 
/**
 * A utility that generates the binary serialization of the phone prefix mappings from
 * human-readable text files. It also generates a configuration file which contains information on
 * data files available for use.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using PhoneNumbers;

namespace Tools
{
    public class GeneratePhonePrefixData
    {
        // The path to the input directory containing the languages directories.
        private readonly string inputPath;

        private const int NANPA_COUNTRY_CODE = 1;

        // Pattern used to match the language code contained in the input text file path. This may be a
        // two-letter code like fr, or a three-letter code like ban, or a code containing script
        // information like zh_Hans (simplified Chinese).
        private static readonly Regex LanguageInFilePathPattern = new Regex("(.*/)(?:[a-zA-Z_]+)(/\\d+\\.txt)");

        // Dictionary used to store the English mappings to avoid reading the English text files multiple times.
        private readonly Dictionary<int /* country code */, SortedDictionary<int, string>> englishMaps =
            new Dictionary<int, SortedDictionary<int, string>>();

        public GeneratePhonePrefixData(string inputPath)
        {
            if (!Directory.Exists(inputPath))
            {
                throw new IOException("The provided input path does not exist: " + inputPath);
            }
            this.inputPath = inputPath;
        }

/**
 * Implement this interface to provide a callback to the parseTextFile() method.
 */
        internal class PhonePrefixMappingHandler
        {
            private readonly Action<int, string> action;

            public PhonePrefixMappingHandler(Action<int, string> action)
            {
                this.action = action;
            }

            /**
             * Method called every time the parser matches a mapping. Note that 'prefix' is the prefix as
             * it is written in the text file (i.e phone number prefix appended to country code).
             */
            internal void Process(int prefix, string location)
            {
                action.Invoke(prefix, location);
            }
        }

/**
 * Reads phone prefix data from the provided input stream and invokes the given handler for each
 * mapping read.
 */
        internal static void ParseTextStream(Stream input,
            PhonePrefixMappingHandler handler)
        {
            var streamReader =
                new StreamReader(input, Encoding.UTF8);
            var lineNumber = 1;

            for (string line; (line = streamReader.ReadLine()) != null; lineNumber++)
            {
                line = line.Trim();
                if (line.Length == 0 || line.StartsWith("#"))
                {
                    continue;
                }
                var indexOfPipe = line.IndexOf('|');
                if (indexOfPipe == -1)
                {
                    throw new Exception($"line {lineNumber}: malformatted data, expected '|'");
                }
                var prefix = line.Substring(0, indexOfPipe);
                var location = line.Substring(indexOfPipe + 1);
                handler.Process(int.Parse(prefix), location);
            }
            streamReader.Close();
        }

        /**
         * Writes the provided phone prefix Dictionary to the provided output stream.
         *
         * @throws IOException
         */
        internal static void WriteToBinaryStream(SortedDictionary<int, string> sortedDictionary, Stream output)
        {
            // Build the corresponding phone prefix Dictionary and serialize it to the binary format.
            var phonePrefixMap = new PhonePrefixMap();
            phonePrefixMap.ReadPhonePrefixMap(sortedDictionary);
            var objectBinaryStream = new BinaryWriter(output);
            phonePrefixMap.WriteExternal(objectBinaryStream);
            objectBinaryStream.Flush();
        }

        /**
         * Reads the mappings contained in the provided input stream pointing to a text file.
         *
         * @return  a Dictionary containing the mappings that were read
         */
        internal static SortedDictionary<int, string> ReadMappingsFromStream(Stream input)
        {
            var phonePrefixMap = new SortedDictionary<int, string>();
            ParseTextStream(input, new PhonePrefixMappingHandler((prefix, location) =>
                    phonePrefixMap.Add(prefix, location)
            ));
            return phonePrefixMap;
        }

        private class PhonePrefixLanguagePair
        {
            public readonly string Prefix;
            public readonly string Language;

            public PhonePrefixLanguagePair(string prefix, string language)
            {
                Prefix = prefix;
                Language = language;
            }
        }

        private static string GenerateBinaryFilename(int prefix, string lang)
        {
            return $"{prefix}_{lang}";
        }

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
        private List<string> CreateOutputFiles(Stream countryCodeFile, int countryCode, string language)
        {
            var outputFiles = new List<string>();
            // For NANPA, split the data into multiple binary files.
            if (countryCode == NANPA_COUNTRY_CODE)
            {

                // Fetch the 4-digit prefixes stored in the file.
                var phonePrefixes = new HashSet<int>();
                ParseTextStream(countryCodeFile,
                    new PhonePrefixMappingHandler((prefix, location) =>
                        phonePrefixes.Add(int.Parse(prefix.ToString().Substring(0, 4)))));
                outputFiles.AddRange(phonePrefixes.Select(prefix => GenerateBinaryFilename(prefix, language)));
            }
            else
            {
                outputFiles.Add(GenerateBinaryFilename(countryCode, language));
            }
            return outputFiles;
        }

        /**
         * Returns the country code extracted from the provided text file name expected as
         * [1-9][0-9]*.txt.
         *
         * @throws RuntimeException if the file path is not formatted as expected
         */
        private static int GetCountryCodeFromTextFileName(string filename)
        {
            var indexOfDot = filename.IndexOf('.');
            if (indexOfDot < 1)
            {
                throw new Exception($"unexpected file name {filename}, expected pattern [1-9][0-9]*.txt");
            }
            var countryCode = filename.Substring(0, indexOfDot);
            return int.Parse(countryCode);
        }

        /**
         * Generates the mappings between the input text files and the output binary files.
         *
         * @throws IOException
         */
        private Dictionary<string, List<string>> CreateInputOutputMappings()
        {
            var mappings = new Dictionary<string, List<string>>();
            var languageDirectories = Directory.GetDirectories(inputPath);
            // Make sure that filenames are processed in the same order build-to-build.
            Array.Sort(languageDirectories);

            foreach (var languageDirectory in languageDirectories)
            {
                if (File.GetAttributes(languageDirectory).HasFlag(FileAttributes.Hidden))
                {
                    continue;
                }
                var countryCodeFiles = Directory.GetFiles(languageDirectory);
                Array.Sort(countryCodeFiles);

                foreach (var countryCodeFile in countryCodeFiles)
                {
                    if (File.GetAttributes(countryCodeFile).HasFlag(FileAttributes.Hidden))
                    {
                        continue;
                    }
                    var countryCodeFileName = Path.GetFileName(countryCodeFile);
                    var outputFiles = CreateOutputFiles(
                        File.OpenRead(countryCodeFile), GetCountryCodeFromTextFileName(countryCodeFileName),
                        Path.GetFileName(languageDirectory));
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
            var outputPhonePrefixMappingsFileName = Path.GetFileName(outputPhonePrefixMappingsFile);
            var phonePrefixLanguagePair = GetPhonePrefixLanguagePairFromFilename(outputPhonePrefixMappingsFileName);
            var prefix = int.Parse(phonePrefixLanguagePair.Prefix);
            var language = phonePrefixLanguagePair.Language;
            if (!availableDataFiles.ContainsKey(prefix))
            {
                var languageSet = new HashSet<string>();
                availableDataFiles.Add(prefix, languageSet);
            }
            availableDataFiles[prefix].Add(language);
        }

        /**
         * Outputs the binary configuration file mapping country codes to language strings.
         */
        internal static void OutputBinaryConfiguration(SortedDictionary<int, HashSet<string>> availableDataFiles,
            Stream outputStream)
        {
            var mappingFileProvider = new MappingFileProvider();
            mappingFileProvider.ReadFileConfigs(availableDataFiles);
            var objectOutputStream = new BinaryWriter(outputStream);
            mappingFileProvider.WriteExternal(objectOutputStream);
            objectOutputStream.Flush();
        }

        /**
         * Splits the provided phone prefix Dictionary into multiple maps according to the provided list of
         * output binary files. A Dictionary associating output binary files to phone prefix maps is returned as
         * a result.
         * <pre>
         * Example:
         *   input Dictionary: { 12011: Description1, 12021: Description2 }
         *   outputBinaryFiles: { 1201_en, 1202_en }
         *   output Dictionary: { 1201_en: { 12011: Description1 }, 1202_en: { 12021: Description2 } }
         * </pre>
         */
        internal static Dictionary<string, SortedDictionary<int, string>> SplitDictionary(
            SortedDictionary<int, string> mappings, List<string> outputBinaryFiles)
        {
            var mappingsForFiles =
                new Dictionary<string, SortedDictionary<int, string>>();
            foreach (var mapping in mappings)
            {
                var prefix = mapping.Key.ToString();
                var targetFile = (from outputBinaryFile in outputBinaryFiles
                        let outputBinaryFilePrefix =
                            GetPhonePrefixLanguagePairFromFilename(Path.GetFileName(outputBinaryFile)).Prefix
                        where prefix.StartsWith(outputBinaryFilePrefix)
                        select outputBinaryFile)
                    .FirstOrDefault();

                if (!mappingsForFiles.ContainsKey(targetFile))
                {
                    mappingsForFiles.Add(targetFile, new SortedDictionary<int, string>());
                }
                mappingsForFiles[targetFile].Add(mapping.Key, mapping.Value);
            }
            return mappingsForFiles;
        }

        /**
         * Gets the English data text file path corresponding to the provided one.
         */
        internal static string GetEnglishDataPath(string inputTextFileName)
        {
            return LanguageInFilePathPattern.Replace(inputTextFileName, "$1en$2", 1);
        }

        /**
         * Tests whether any prefix of the given number overlaps with any phone number prefix contained in
         * the provided Dictionary.
         */
        internal static bool HasOverlappingPrefix(int number, IReadOnlyDictionary<int, string> mappings)
        {
            while (number > 0)
            {
                number = number / 10;
                if (mappings.ContainsKey(number))
                {
                    return true;
                }
            }
            return false;
        }

        /**
         * Compresses the provided non-English Dictionary according to the English Dictionary provided. For each mapping
         * which is contained in both maps with a same description this method either:
         * <ul>
         *  <li> Removes from the non-English Dictionary the mapping whose prefix does not overlap with an
         *       existing prefix in the Dictionary, or;
         *  <li> Keeps this mapping in both maps but makes the description an empty string in the
         *       non-English Dictionary.
         * </ul>
         */
        internal static void CompressAccordingToEnglishData(
            SortedDictionary<int, string> englishMap, SortedDictionary<int, string> nonEnglishMap)
        {
            foreach (var prefix in nonEnglishMap.Keys.ToList())
            {
                var englishDescription = englishMap[prefix];
                if (englishDescription != null && englishDescription.Equals(nonEnglishMap[prefix]))
                {
                    if (!HasOverlappingPrefix(prefix, nonEnglishMap))
                    {
                        nonEnglishMap.Remove(prefix);
                    }
                    else
                    {
                        nonEnglishMap[prefix] = "";
                    }
                }
            }
        }

/**
 * Compresses the provided mappings according to the English data file if any.
 *
 * @throws IOException
 */
        private void MakeDataFallbackToEnglish(string inputTextFile, SortedDictionary<int, string> mappings)
        {
            var englishTextFile = GetEnglishDataPath(inputTextFile);
            if (Path.GetFullPath(inputTextFile).Equals(Path.GetFullPath(englishTextFile))
                || !File.Exists(englishTextFile))
            {
                return;
            }
            var countryCode = GetCountryCodeFromTextFileName(Path.GetFileName(inputTextFile));
            var englishMap = englishMaps[countryCode];
            if (englishMap == null)
            {
                using (var englishTextStream = File.OpenRead(englishTextFile))
                {
                    englishMap = ReadMappingsFromStream(englishTextStream);
                    englishMaps.Add(countryCode, englishMap);
                }
            }
            CompressAccordingToEnglishData(englishMap, mappings);
        }

        /**
         * Removes the empty-description mappings in the provided Dictionary if the language passed-in is "en".
         */
        internal static void RemoveEmptyEnglishMappings(SortedDictionary<int, string> dictionary, string lang)
        {
            if (!lang.Equals("en"))
            {
                return;
            }


            foreach (var emptyMapping in dictionary.Where(mapping => mapping.Value == string.Empty)
                .Select(mapping => mapping.Key).ToList())
            {
                dictionary.Remove(emptyMapping);
            }
        }

        /**
         * Runs the phone prefix data generator.
         */
        public void Run()
        {
            var inputOutputMappings = CreateInputOutputMappings();
            var availableDataFiles = new SortedDictionary<int, HashSet<string>>();

            foreach (var inputOutputMapping in inputOutputMappings)
            {
                using (var inputFileStream = File.OpenRead(inputOutputMapping.Key))
                {
                    var outputBinaryFiles = inputOutputMapping.Value;
                    var mappings = ReadMappingsFromStream(inputFileStream);
                    RemoveEmptyEnglishMappings(mappings, Path.GetDirectoryName(inputOutputMapping.Key));
                    MakeDataFallbackToEnglish(inputOutputMapping.Key, mappings);
                    var mappingsForFiles = SplitDictionary(mappings, outputBinaryFiles);

                    foreach (var mappingsForFile in mappingsForFiles)
                    {
                        using (var outputBinaryFile = File.Create(mappingsForFile.Key))
                        {
                            WriteToBinaryStream(mappingsForFile.Value, outputBinaryFile);
                            AddConfigurationMapping(availableDataFiles, mappingsForFile.Key);
                        }
                    }
                }

            }
            // Output the binary configuration file mapping country codes to languages.
            using (var fileOutputStream = File.Create("config"))
            {
                OutputBinaryConfiguration(availableDataFiles, fileOutputStream);
            }
        }
    }
}