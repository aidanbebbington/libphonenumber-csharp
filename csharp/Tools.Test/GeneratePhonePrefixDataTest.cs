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
 * Unittests for GeneratePhonePrefixData.java
 *
 * @author Philippe Liard
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PhoneNumbers;
using Xunit;

namespace Tools.Test
{
    public class GeneratePhonePrefixDataTest
    {
        private readonly SortedDictionary<int, HashSet<string>> availableDataFiles =
            new SortedDictionary<int, HashSet<string>>();

        // Languages for US.
        public GeneratePhonePrefixDataTest()
        {
            GeneratePhonePrefixData.AddConfigurationMapping(availableDataFiles, "1_en");
            GeneratePhonePrefixData.AddConfigurationMapping(availableDataFiles, "1_en_US");
            GeneratePhonePrefixData.AddConfigurationMapping(availableDataFiles, "1_es");

            // Languages for France.
            GeneratePhonePrefixData.AddConfigurationMapping(availableDataFiles, "33_fr");
            GeneratePhonePrefixData.AddConfigurationMapping(availableDataFiles, "33_en");

            // Languages for China.
            GeneratePhonePrefixData.AddConfigurationMapping(availableDataFiles, "86_zh_Hans");
        }

        [Fact]
        public void TestAddConfigurationMapping()
        {
            Assert.Equal(3, availableDataFiles.Count);

            var languagesForUS = availableDataFiles[1];
            Assert.Equal(3, languagesForUS.Count);
            Assert.True(languagesForUS.Contains("en"));
            Assert.True(languagesForUS.Contains("en_US"));
            Assert.True(languagesForUS.Contains("es"));

            var languagesForFR = availableDataFiles[33];
            Assert.Equal(2, languagesForFR.Count);
            Assert.True(languagesForFR.Contains("fr"));
            Assert.True(languagesForFR.Contains("en"));

            var languagesForCN = availableDataFiles[86];
            Assert.Equal(1, languagesForCN.Count);
            Assert.True(languagesForCN.Contains("zh_Hans"));
        }

        [Fact]
        public void TestOutputBinaryConfiguration()
        {
            var byteArrayOutputStream = new MemoryStream();
            GeneratePhonePrefixData.OutputBinaryConfiguration(availableDataFiles, byteArrayOutputStream);
            byteArrayOutputStream.Position = 0;
            var mappingFileProvider = new MappingFileProvider();
            mappingFileProvider.ReadExternal(new BinaryReader(byteArrayOutputStream));
            Assert.Equal("1|en,en_US,es,\n33|en,fr,\n86|zh_Hans,\n", mappingFileProvider.ToString());
        }

        private static Dictionary<int, string> ParseTextStreamHelper(Stream input)
        {
            var mappings = new Dictionary<int, string>();
            GeneratePhonePrefixData.ParseTextStream(input,
                new GeneratePhonePrefixData.PhonePrefixMappingHandler((phonePrefix, location) =>
                    mappings.Add(phonePrefix, location)));
            return mappings;
        }

        [Fact]
        public void TestParseTextFile()
        {
            var result = ParseTextStreamHelper(new MemoryStream(Encoding.UTF8.GetBytes("331|Paris\n334|Marseilles\n")));
            Assert.Equal(2, result.Count);
            Assert.Equal("Paris", result[331]);
            Assert.Equal("Marseilles", result[334]);
        }

        [Fact]
        public void TestParseTextFileIgnoresComments()
        {
            var result = ParseTextStreamHelper(new MemoryStream(Encoding.UTF8.GetBytes("# Hello\n331|Paris\n334|Marseilles\n")));
            Assert.Equal(2, result.Count);
            Assert.Equal("Paris", result[331]);
            Assert.Equal("Marseilles", result[334]);
        }

        [Fact]
        public void TestParseTextFileIgnoresBlankLines()
        {
            var result = ParseTextStreamHelper(new MemoryStream(Encoding.UTF8.GetBytes("\n331|Paris\n334|Marseilles\n")));
            Assert.Equal(2, result.Count);
            Assert.Equal("Paris", result[331]);
            Assert.Equal("Marseilles", result[334]);
        }

        [Fact]
        public void TestParseTextFileIgnoresTrailingWhitespaces()
        {
            var result = ParseTextStreamHelper(new MemoryStream(Encoding.UTF8.GetBytes("331|Paris  \n334|Marseilles  \n")));
            Assert.Equal(2, result.Count);
            Assert.Equal("Paris", result[331]);
            Assert.Equal("Marseilles", result[334]);
        }

        [Fact]
        public void TestParseTextFileThrowsExceptionWithMalformattedData()
        {
            Assert.Throws<Exception>(() => ParseTextStreamHelper(new MemoryStream(Encoding.UTF8.GetBytes("331"))));
        }

        [Fact]
        public void TestParseTextFileAcceptsMissingLocation()
        {
            ParseTextStreamHelper(new MemoryStream(Encoding.UTF8.GetBytes("331|")));
        }

        [Fact]
        public void TestSplitMap()
        {
            var mappings = new SortedDictionary<int, string>();
            var outputFiles = new List<string> {"1201_en", "1202_en"};
            mappings.Add(12011, "Location1");
            mappings.Add(12012, "Location2");
            mappings.Add(12021, "Location3");
            mappings.Add(12022, "Location4");

            var splitMaps = GeneratePhonePrefixData.SplitDictionary(mappings, outputFiles);
            Assert.Equal(2, splitMaps.Count);
            Assert.Equal("Location1", splitMaps["1201_en"][12011]);
            Assert.Equal("Location2", splitMaps["1201_en"][12012]);
            Assert.Equal("Location3", splitMaps["1202_en"][12021]);
            Assert.Equal("Location4", splitMaps["1202_en"][12022]);
        }

        private static string ConvertDataHelper(string input)
        {
            var byteArrayInputStream = new MemoryStream(Encoding.UTF8.GetBytes(input));
            var byteArrayOutputStream = new MemoryStream();

            var phonePrefixMappings = GeneratePhonePrefixData.ReadMappingsFromStream(byteArrayInputStream);
            GeneratePhonePrefixData.WriteToBinaryStream(phonePrefixMappings, byteArrayOutputStream);
            // The byte array output stream now Contains the corresponding serialized phone prefix SortedDictionary. Try
            // to deserialize it and compare it with the initial input.
            var phonePrefixMap = new PhonePrefixMap();
            byteArrayOutputStream.Position = 0;
            phonePrefixMap.ReadExternal(new BinaryReader(byteArrayOutputStream));

            return phonePrefixMap.ToString();
        }

        [Fact]
        public void TestConvertData()
        {
            var input = "331|Paris\n334|Marseilles\n";

            var dataAfterDeserialization = ConvertDataHelper(input);
            Assert.Equal(input, dataAfterDeserialization);
        }

        [Fact]
        public void TestConvertDataSupportsEmptyDescription()
        {
            var input = "331|Paris\n334|Marseilles\n3341|\n";

            var dataAfterDeserialization = ConvertDataHelper(input);
            Assert.Equal(3, dataAfterDeserialization.Trim().Split("\n").Length);
            Assert.Equal(input, dataAfterDeserialization);
        }

        [Fact]
        public void TestConvertDataThrowsExceptionWithDuplicatedPhonePrefixes()
        {
            var input = "331|Paris\n331|Marseilles\n";
            Assert.Throws<ArgumentException>(() => ConvertDataHelper(input));
        }

        [Fact]
        public void TestGetEnglishDataPath()
        {
            Assert.Equal("/path/en/33.txt", GeneratePhonePrefixData.GetEnglishDataPath("/path/fr/33.txt"));
        }

        [Fact]
        public void TestHasOverlap()
        {
            var SortedDictionary = new SortedDictionary<int, string> {{1234, ""}, {123, ""}, {2345, ""}};

            Assert.True(GeneratePhonePrefixData.HasOverlappingPrefix(1234, SortedDictionary));
            Assert.False(GeneratePhonePrefixData.HasOverlappingPrefix(2345, SortedDictionary));
        }

        [Fact]
        public void TestCompressAccordingToEnglishDataMakesDescriptionEmpty()
        {
            var frenchMappings = new SortedDictionary<int, string> {{411, "Genève"}, {4112, "Zurich"}};

            var englishMappings = new SortedDictionary<int, string> {{411, "Geneva"}, {4112, "Zurich"}};

            GeneratePhonePrefixData.CompressAccordingToEnglishData(englishMappings, frenchMappings);

            Assert.Equal(2, frenchMappings.Count);
            Assert.Equal("Genève", frenchMappings[411]);
            Assert.Equal("", frenchMappings[4112]);
        }

        [Fact]
        public void TestCompressAccordingToEnglishDataRemovesMappingWhenNoOverlap()
        {
            var frenchMappings = new SortedDictionary<int, string> {{411, "Genève"}, {412, "Zurich"}};

            var englishMappings = new SortedDictionary<int, string> {{411, "Geneva"}, {412, "Zurich"}};

            GeneratePhonePrefixData.CompressAccordingToEnglishData(englishMappings, frenchMappings);

            Assert.Equal(1, frenchMappings.Count);
            Assert.Equal("Genève", frenchMappings[411]);
        }

        [Fact]
        public void TestCompressAccordingToEnglishData()
        {
            var frenchMappings = new SortedDictionary<int, string> {{12, "A"}, {123, "B"}};

            var englishMappings = new SortedDictionary<int, string> {{12, "A"}, {123, "B"}};

            GeneratePhonePrefixData.CompressAccordingToEnglishData(englishMappings, frenchMappings);

            Assert.Equal(0, frenchMappings.Count);
        }

        [Fact]
        public void TestRemoveEmptyEnglishMappingsDoesNotRemoveNonEnglishMappings()
        {
            var frenchMappings = new SortedDictionary<int, string> {{331, "Paris"}, {334, ""}};

            GeneratePhonePrefixData.RemoveEmptyEnglishMappings(frenchMappings, "fr");

            Assert.Equal(2, frenchMappings.Count);
        }

        [Fact]
        public void TestRemoveEmptyEnglishMappings()
        {
            var englishMappings = new SortedDictionary<int, string> {{331, "Paris"}, {334, ""}};

            GeneratePhonePrefixData.RemoveEmptyEnglishMappings(englishMappings, "en");

            Assert.Equal(1, englishMappings.Count);
            Assert.Equal("Paris", englishMappings[331]);
        }
    }
}