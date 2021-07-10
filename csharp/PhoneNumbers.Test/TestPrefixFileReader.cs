/*
 * Copyright (C) 2013 The Libphonenumber Authors
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

using System.Reflection;
using PhoneNumbers.Carrier;
using Xunit;

namespace PhoneNumbers.Test
{
    /**
 * Unit tests for PrefixFileReader.java
 *
 * @author Cecilia Roes
 */
    public class PrefixFileReaderTest
    {
        private const string TEST_MAPPING_DATA_DIRECTORY = "PhoneNumbers.Test.Carrier.TestingData.";

        private readonly PrefixFileReader reader = new PrefixFileReader(TEST_MAPPING_DATA_DIRECTORY,
            Assembly.GetAssembly(typeof(PrefixFileReaderTest)));

        private static readonly PhoneNumber KONumber =
            new PhoneNumber.Builder().SetCountryCode(82).SetNationalNumber(22123456L).Build();
        private static readonly PhoneNumber USNumber1 =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(6502530000L).Build();
        private static readonly PhoneNumber USNumber2 =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(2128120000L).Build();
        private static readonly PhoneNumber USNumber3 =
            new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(6174240000L).Build();
        private static readonly PhoneNumber SENumber =
            new PhoneNumber.Builder().SetCountryCode(46).SetNationalNumber(81234567L).Build();

        [Fact]
        public void TestGetDescriptionForNumberWithMapping() {
            Assert.Equal("Kalifornien",
                reader.GetDescriptionForNumber(USNumber1, "de", "", "CH"));
            Assert.Equal("CA",
                reader.GetDescriptionForNumber(USNumber1, "en", "", "AU"));
            Assert.Equal("\uC11C\uC6B8",
                reader.GetDescriptionForNumber(KONumber, "ko", "", ""));
            Assert.Equal("Seoul",
                reader.GetDescriptionForNumber(KONumber, "en", "", ""));
        }

        [Fact]
        public void TestGetDescriptionForNumberWithMissingMapping() {
            Assert.Equal("", reader.GetDescriptionForNumber(USNumber3, "en", "", ""));
        }

        [Fact]
        public void TestGetDescriptionUsingFallbackLanguage() {
            // Mapping file exists but the number isn't present, causing it to fallback.
            Assert.Equal("New York, NY",
                reader.GetDescriptionForNumber(USNumber2, "de", "", "CH"));
            // No mapping file exists, causing it to fallback.
            Assert.Equal("New York, NY",
                reader.GetDescriptionForNumber(USNumber2, "sv", "", ""));
        }

        [Fact]
        public void TestGetDescriptionForNonFallbackLanguage() {
            Assert.Equal("", reader.GetDescriptionForNumber(USNumber2, "ko", "", ""));
        }

        [Fact]
        public void TestGetDescriptionForNumberWithoutMappingFile() {
            Assert.Equal("", reader.GetDescriptionForNumber(SENumber, "sv", "", ""));
            Assert.Equal("", reader.GetDescriptionForNumber(SENumber, "en", "", ""));
        }
    }
}