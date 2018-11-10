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
 * Unittests for GenerateTimeZonesMapData.java
 *
 * @author Walter Erquinigo
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PhoneNumbers;
using Xunit;

namespace Tools.Test
{
    public class GenerateTimeZonesMapDataTest
    {
        private const string BRUSSELS_TZ = "Europe/Brussels";
        private const string PARIS_TZ = "Europe/Paris";

        private const string PARIS_BRUSSELS_LINES =
            "322|" + BRUSSELS_TZ + "\n331|" + PARIS_TZ + "\n";

        private static SortedDictionary<int, string> ParseTextFileHelper(string input)
        {
            return GenerateTimeZonesMapData.ParseTextFile(new MemoryStream(Encoding.UTF8.GetBytes(input)));
        }

        [Fact]
        public void TestParseTextFile()
        {
            var result = ParseTextFileHelper(PARIS_BRUSSELS_LINES);
            Assert.Equal(2, result.Count);
            Assert.Equal(PARIS_TZ, result[331]);
            Assert.Equal(BRUSSELS_TZ, result[322]);
        }

        [Fact]
        public void TestParseTextFileIgnoresComments()
        {
            var result = ParseTextFileHelper("# Hello\n" + PARIS_BRUSSELS_LINES);
            Assert.Equal(2, result.Count);
            Assert.Equal(PARIS_TZ, result[331]);
            Assert.Equal(BRUSSELS_TZ, result[322]);
        }

        [Fact]
        public void TestParseTextFileIgnoresBlankLines()
        {
            var result = ParseTextFileHelper("\n" + PARIS_BRUSSELS_LINES);
            Assert.Equal(2, result.Count);
            Assert.Equal(PARIS_TZ, result[331]);
            Assert.Equal(BRUSSELS_TZ, result[322]);
        }

        [Fact]
        public void TestParseTextFileIgnoresTrailingWhitespaces()
        {
            var result = ParseTextFileHelper(
                "331|" + PARIS_TZ + "\n322|" + BRUSSELS_TZ + "  \n");
            Assert.Equal(2, result.Count);
            Assert.Equal(PARIS_TZ, result[331]);
            Assert.Equal(BRUSSELS_TZ, result[322]);
        }

        [Fact]
        public void TestParseTextFileThrowsExceptionWithMalformattedData()
        {
            Assert.Throws<Exception>(() => ParseTextFileHelper("331"));
        }

        [Fact]
        public void TestParseTextFileThrowsExceptionWithMissingTimeZone()
        {
            Assert.Throws<Exception>(() => ParseTextFileHelper("331|"));
        }

// Returns a string representing the input after serialization and deserialization by
// PrefixTimeZonesMap.
        private static string ConvertDataHelper(string input)
        {
            var byteArrayOutputStream = new MemoryStream();
            var prefixTimeZonesMapping = ParseTextFileHelper(input);

            GenerateTimeZonesMapData.WriteToBinaryFile(prefixTimeZonesMapping, byteArrayOutputStream);
            byteArrayOutputStream.Position = 0;
            // The byte array output stream now contains the corresponding serialized prefix to time zones
            // SortedDictionary. Try to deserialize it and compare it with the initial input.
            var prefixTimeZonesMap = new PrefixTimeZonesMap();
            prefixTimeZonesMap.ReadExternal(new BinaryReader(byteArrayOutputStream));

            return prefixTimeZonesMap.ToString();
        }

        [Fact]
        public void TestConvertData()
        {
            var input = PARIS_BRUSSELS_LINES;

            var dataAfterDeserialization = ConvertDataHelper(input);
            Assert.Equal(input, dataAfterDeserialization);
        }

        [Fact]
        public void TestConvertThrowsExceptionWithMissingTimeZone()
        {
            var input = PARIS_BRUSSELS_LINES + "3341|\n";
            Assert.Throws<Exception>(() => ConvertDataHelper(input));
        }

        [Fact]
        public void TestConvertDataThrowsExceptionWithDuplicatedPrefixes()
        {
            var input = "331|" + PARIS_TZ + "\n331|" + BRUSSELS_TZ + "\n";
            Assert.Throws<ArgumentException>(() =>ConvertDataHelper(input));
        }
    }
}