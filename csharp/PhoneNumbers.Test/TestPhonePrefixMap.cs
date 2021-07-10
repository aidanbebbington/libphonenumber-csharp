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

using System.Collections.Generic;
using System.Collections.Immutable;
using PhoneNumbers.Carrier;
using Xunit;
// ReSharper disable xUnit1004

namespace PhoneNumbers.Test
{
/**
 * Unittests for PhonePrefixMap.java
 *
 * @author Shaopeng Jia
 */
    public class TestPhonePrefixMap
    {
        private readonly PhonePrefixMap phonePrefixMapForUS = new PhonePrefixMap();
        private readonly PhonePrefixMap phonePrefixMapForIT = new PhonePrefixMap();
        private readonly PhoneNumber.Builder number = new PhoneNumber.Builder();

        public TestPhonePrefixMap()
        {
            var sortedMapForUS = new Dictionary<int, string>()
            {
                {1212, "New York"},
                {
                    1480, "Arizona"
                },
                {1650, "California"},
                {
                    1907, "Alaska"
                },
                {1201664, "Westwood, NJ"},
                {1480893, "Phoenix, AZ"},
                {1501372, "Little Rock, AR"},
                {1626308, "Alhambra, CA"},
                {1650345, "San Mateo, CA"},
                {1867993, "Dawson, YT"},
                {1972480, "Richardson, TX"}
            }.ToImmutableSortedDictionary();

            phonePrefixMapForUS.ReadPhonePrefixMap(sortedMapForUS);

            var sortedMapForIT = new Dictionary<int, string>
            {
                {3902, "Milan"},
                {3906, "Rome"},
                {39010, "Genoa"},
                {390131, "Alessandria"},
                {390321, "Novara"},
                {390975, "Potenza"}
            }.ToImmutableSortedDictionary();

            phonePrefixMapForIT.ReadPhonePrefixMap(sortedMapForIT);
        }

        private static ImmutableSortedDictionary<int, string> CreateDefaultStorageMapCandidate()
            // Make the phone prefixes bigger to store them using integer.
            => new Dictionary<int, string>
            {
                {121212345, "New York"},
                {148034434, "Arizona"
                }
            }.ToImmutableSortedDictionary();

        private static ImmutableSortedDictionary<int, string> CreateFlyweightStorageMapCandidate()
            => new Dictionary<int, string>
            {
                {1212, "New York"},
                {1213, "New York"},
                {1214, "New York"},
                {1480, "Arizona"}
            }.ToImmutableSortedDictionary();

        [Fact]
        public void GetSmallerMapStorageChoosesDefaultImpl()
        {
            var mapStorage =
                new PhonePrefixMap().GetSmallerMapStorage(CreateDefaultStorageMapCandidate());
            Assert.False(mapStorage is FlyweightMapStorage);
        }

        [Fact]
        public void GetSmallerMapStorageChoosesFlyweightImpl()
        {
            var mapStorage =
                new PhonePrefixMap().GetSmallerMapStorage(CreateFlyweightStorageMapCandidate());
            Assert.True(mapStorage is FlyweightMapStorage);
        }

        [Fact]
        public void LookupInvalidNumber_US()
        {
            // central office code cannot start with 1.
            number.SetCountryCode(1).SetNationalNumber(2121234567L);
            Assert.Equal("New York", phonePrefixMapForUS.Lookup(number.Build()));
        }

        [Fact]
        public void LookupNumber_NJ()
        {
            number.SetCountryCode(1).SetNationalNumber(2016641234L);
            Assert.Equal("Westwood, NJ", phonePrefixMapForUS.Lookup(number.Build()));
        }

        [Fact]
        public void LookupNumber_NY()
        {
            number.SetCountryCode(1).SetNationalNumber(2126641234L);
            Assert.Equal("New York", phonePrefixMapForUS.Lookup(number.Build()));
        }

        [Fact]
        public void LookupNumber_CA_1()
        {
            number.SetCountryCode(1).SetNationalNumber(6503451234L);
            Assert.Equal("San Mateo, CA", phonePrefixMapForUS.Lookup(number.Build()));
        }

        [Fact]
        public void LookupNumber_CA_2()
        {
            number.SetCountryCode(1).SetNationalNumber(6502531234L);
            Assert.Equal("California", phonePrefixMapForUS.Lookup(number.Build()));
        }

        [Fact]
        public void LookupNumberFound_TX()
        {
            number.SetCountryCode(1).SetNationalNumber(9724801234L);
            Assert.Equal("Richardson, TX", phonePrefixMapForUS.Lookup(number.Build()));
        }

        [Fact]
        public void LookupNumberNotFound_TX()
        {
            number.SetCountryCode(1).SetNationalNumber(9724811234L);
            Assert.Null(phonePrefixMapForUS.Lookup(number.Build()));
        }

        [Fact]
        public void LookupNumber_CH()
        {
            number.SetCountryCode(41).SetNationalNumber(446681300L);
            Assert.Null(phonePrefixMapForUS.Lookup(number.Build()));
        }

        [Fact]
        public void LookupNumber_IT()
        {
            number.SetCountryCode(39).SetNationalNumber(212345678L).SetItalianLeadingZero(true);
            Assert.Equal("Milan", phonePrefixMapForIT.Lookup(number.Build()));

            number.SetNationalNumber(612345678L);
            Assert.Equal("Rome", phonePrefixMapForIT.Lookup(number.Build()));

            number.SetNationalNumber(3211234L);
            Assert.Equal("Novara", phonePrefixMapForIT.Lookup(number.Build()));

            // A mobile number
            number.SetNationalNumber(321123456L).SetItalianLeadingZero(false);
            Assert.Null(phonePrefixMapForIT.Lookup(number.Build()));

            // An invalid number (too short)
            number.SetNationalNumber(321123L).SetItalianLeadingZero(true);
            Assert.Equal("Novara", phonePrefixMapForIT.Lookup(number.Build()));
        }

        /**
     * Creates a new phone prefix map serializing the provided phone prefix map to a stream and then
     * reading this stream. The resulting phone prefix map is expected to be strictly equal to the
     * provided one from which it was generated.
     */
        private static PhonePrefixMap CreateNewPhonePrefixMap(
            PhonePrefixMap phonePrefixMap)
        {
            //var byteArrayOutputStream = new ByteArrayOutputStream();
            //var objectOutputStream = new ObjectOutputStream(byteArrayOutputStream);
            //phonePrefixMap.WriteExternal(objectOutputStream);
            //objectOutputStream.flush();

            var newPhonePrefixMap = new PhonePrefixMap();
            //newPhonePrefixMap.ReadExternal(
            //        new ObjectInputStream(new ByteArrayInputStream(byteArrayOutputStream.toByteArray())));
            return newPhonePrefixMap;
        }

        [Fact]
        public void ReadWriteExternalWithDefaultStrategy()
        {
            var localPhonePrefixMap = new PhonePrefixMap();
            localPhonePrefixMap.ReadPhonePrefixMap(CreateDefaultStorageMapCandidate());
            Assert.False(localPhonePrefixMap.GetPhonePrefixMapStorage() is FlyweightMapStorage);

            var newPhonePrefixMap = CreateNewPhonePrefixMap(localPhonePrefixMap);
            Assert.Equal(localPhonePrefixMap.ToString(), newPhonePrefixMap.ToString());
        }

        [Fact]
        public void ReadWriteExternalWithFlyweightStrategy()
        {
            var localPhonePrefixMap = new PhonePrefixMap();
            localPhonePrefixMap.ReadPhonePrefixMap(CreateFlyweightStorageMapCandidate());
            Assert.True(localPhonePrefixMap.GetPhonePrefixMapStorage() is FlyweightMapStorage);

            var newPhonePrefixMap = CreateNewPhonePrefixMap(localPhonePrefixMap);
            Assert.Equal(localPhonePrefixMap.ToString(), newPhonePrefixMap.ToString());
        }
    }
}