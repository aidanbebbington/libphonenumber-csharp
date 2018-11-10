/*
 * Copyright (C) 2011 Google Inc.
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

using System.Collections.Generic;
using Xunit;

namespace PhoneNumbers.Test
{
    /**
    * Unittests for PhonePrefixMap.java
    *
    * @author Shaopeng Jia
    */
    [Collection("TestMetadataTestCase")]
    public class TestPhonePrefixMap
    {
        private readonly PhonePrefixMap phonePrefixMapForUS = new PhonePrefixMap();
        private readonly PhonePrefixMap phonePrefixMapForIT = new PhonePrefixMap();

        public TestPhonePrefixMap()
        {
            var sortedMap = new SortedDictionary<int, string>
            {
                [1212] = "New York",
                [1480] = "Arizona",
                [1650] = "California",
                [1907] = "Alaska",
                [1201664] = "Westwood, NJ",
                [1480893] = "Phoenix, AZ",
                [1501372] = "Little Rock, AR",
                [1626308] = "Alhambra, CA",
                [1650345] = "San Mateo, CA",
                [1867993] = "Dawson, YT",
                [1972480] = "Richardson, TX"
            };
            phonePrefixMapForUS.ReadPhonePrefixMap(sortedMap);

            sortedMap = new SortedDictionary<int, string>
            {
                [3902] = "Milan",
                [3906] = "Rome",
                [39010] = "Genoa",
                [390131] = "Alessandria",
                [390321] = "Novara",
                [390975] = "Potenza"
            };
            phonePrefixMapForIT.ReadPhonePrefixMap(sortedMap);
        }

        private static SortedDictionary<int, string> CreateDefaultStorageMapCandidate()
        {
            // Make the area codes bigger to store them using integer.
            var sortedMap = new SortedDictionary<int, string>
            {
                [121212345] = "New York",
                [148034434] = "Arizona"
            };
            return sortedMap;
        }

        private static SortedDictionary<int, string> CreateFlyweightStorageMapCandidate()
        {
            var sortedMap = new SortedDictionary<int, string>
            {
                [1212] = "New York",
                [1213] = "New York",
                [1214] = "New York",
                [1480] = "Arizona"
            };
            return sortedMap;
        }

        [Fact]
        public void TestGetSmallerMapStorageChoosesDefaultImpl()
        {
            var mapStorage =
                new PhonePrefixMap().GetSmallerMapStorage(CreateDefaultStorageMapCandidate());
            Assert.False(mapStorage.GetType() == typeof(FlyweightMapStorage));
        }

        [Fact]
        public void TestGetSmallerMapStorageChoosesFlyweightImpl()
        {
            var mapStorage =
                new PhonePrefixMap().GetSmallerMapStorage(CreateFlyweightStorageMapCandidate());
            Assert.True(mapStorage.GetType() == typeof(FlyweightMapStorage));
        }

        [Fact]
        public void TestLookupInvalidNumber_US()
        {
            // central office code cannot start with 1.
            var number = new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(2121234567L).Build();
            Assert.Equal("New York", phonePrefixMapForUS.Lookup(number));
        }

        [Fact]
        public void TestLookupNumber_NJ()
        {
            var number = new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(2016641234L).Build();
            Assert.Equal("Westwood, NJ", phonePrefixMapForUS.Lookup(number));
        }

        [Fact]
        public void TestLookupNumber_NY()
        {
            var number = new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(2126641234L).Build();
            Assert.Equal("New York", phonePrefixMapForUS.Lookup(number));
        }

        [Fact]
        public void TestLookupNumber_CA_1()
        {
            var number = new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(6503451234L).Build();
            Assert.Equal("San Mateo, CA", phonePrefixMapForUS.Lookup(number));
        }

        [Fact]
        public void TestLookupNumber_CA_2()
        {
            var number = new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(6502531234L).Build();
            Assert.Equal("California", phonePrefixMapForUS.Lookup(number));
        }

        [Fact]
        public void TestLookupNumberFound_TX()
        {
            var number = new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(9724801234L).Build();
            Assert.Equal("Richardson, TX", phonePrefixMapForUS.Lookup(number));
        }

        [Fact]
        public void TestLookupNumberNotFound_TX()
        {
            var number = new PhoneNumber.Builder().SetCountryCode(1).SetNationalNumber(9724811234L).Build();
            Assert.Null(phonePrefixMapForUS.Lookup(number));
        }

        [Fact]
        public void TestLookupNumber_CH()
        {
            var number = new PhoneNumber.Builder().SetCountryCode(41).SetNationalNumber(446681300L).Build();
            Assert.Null(phonePrefixMapForUS.Lookup(number));
        }

        [Fact]
        public void TestLookupNumber_IT()
        {
            var number = new PhoneNumber.Builder().SetCountryCode(39).SetNationalNumber(212345678L).SetItalianLeadingZero(true)
                .Build();
            Assert.Equal("Milan", phonePrefixMapForIT.Lookup(number));

            number = new PhoneNumber.Builder().SetCountryCode(39).SetNationalNumber(612345678L).SetItalianLeadingZero(true)
                .Build();
            Assert.Equal("Rome", phonePrefixMapForIT.Lookup(number));

            number = new PhoneNumber.Builder().SetCountryCode(39).SetNationalNumber(3211234L).SetItalianLeadingZero(true)
                .Build();
            Assert.Equal("Novara", phonePrefixMapForIT.Lookup(number));

            // A mobile number
            number = new PhoneNumber.Builder().SetCountryCode(39).SetNationalNumber(321123456L).SetItalianLeadingZero(false)
                .Build();
            Assert.Null(phonePrefixMapForIT.Lookup(number));

            // An invalid number (too short)
            number = new PhoneNumber.Builder().SetCountryCode(39).SetNationalNumber(321123L).SetItalianLeadingZero(true)
                .Build();
            Assert.Equal("Novara", phonePrefixMapForIT.Lookup(number));
        }
    }
}
