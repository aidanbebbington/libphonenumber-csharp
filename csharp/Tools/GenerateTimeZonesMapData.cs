/*
 * Copyright (C) 2012 The Libphonenumber Authors
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
 * A utility that generates the binary serialization of the prefix/time zones mappings from a
 * human-readable text file.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PhoneNumbers;

namespace Tools
{
    public class GenerateTimeZonesMapData
    {
        private readonly string inputTextFile;
        private const string MAPPING_DATA_FILE_NAME = "map_data";

        public GenerateTimeZonesMapData(string inputTextFile)
        {
            this.inputTextFile = inputTextFile;
            if (!File.Exists(inputTextFile))
            {
                throw new IOException("The provided input text file does not exist.");
            }
        }


        /**
         * Reads phone prefix data from the provided input stream and returns a SortedDictionary with the
         * prefix to time zones mappings.
         */
        internal static SortedDictionary<int, string> ParseTextFile(Stream input)
        {
            var timeZoneMap = new SortedDictionary<int, string>();
            var bufferedReader = new StreamReader(input, Encoding.UTF8);
            var lineNumber = 1;

            for (string line; (line = bufferedReader.ReadLine()) != null; lineNumber++)
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
                var prefix = int.Parse(line.Substring(0, indexOfPipe));
                var timezones = line.Substring(indexOfPipe + 1);
                if (timezones == String.Empty)
                {
                    throw new Exception($"line {lineNumber}: missing time zones");
                }
                timeZoneMap.Add(prefix, timezones);
            }
            return timeZoneMap;
        }

        /**
         * Writes the provided phone prefix/time zones map to the provided output stream.
         *
         * @
         */
        internal static void WriteToBinaryFile(SortedDictionary<int, string> SortedDictionary, Stream output)

        {
            // Build the corresponding PrefixTimeZonesMap and serialize it to the binary format.
            var prefixTimeZonesMap = new PrefixTimeZonesMap();
            prefixTimeZonesMap.ReadPrefixTimeZonesMap(SortedDictionary);
            var objectOutputStream = new BinaryWriter(output);
            prefixTimeZonesMap.WriteExternal(objectOutputStream);
            objectOutputStream.Flush();
        }

        /**
         * Runs the prefix to time zones map data generator.
         *
         * @
         */
        public void Run()
        {
            using (var fileInputStream = File.OpenRead(inputTextFile))
            {
                var mappings = ParseTextFile(fileInputStream);
                using (var fileOutputStream = File.Create(MAPPING_DATA_FILE_NAME))
                {
                    WriteToBinaryFile(mappings, fileOutputStream);
                }
            }
        }
    }
}