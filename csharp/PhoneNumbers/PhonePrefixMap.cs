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
 * A utility that maps phone number prefixes to a description string, which may be, for example,
 * the geographical area the prefix covers.
 *
 * @author Shaopeng Jia
 */

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PhoneNumbers
{
    public class PhonePrefixMap
    {
        private readonly PhoneNumberUtil phoneUtil = PhoneNumberUtil.GetInstance();

        private PhonePrefixMapStorageStrategy phonePrefixMapStorage;

        internal PhonePrefixMapStorageStrategy GetPhonePrefixMapStorage()
        {
            return phonePrefixMapStorage;
        }

        /**
         * Gets the size of the provided phone prefix map storage. The map storage passed-in will be
         * filled as a result.
         */
        private static int GetSizeOfPhonePrefixMapStorage(PhonePrefixMapStorageStrategy mapStorage,
            SortedDictionary<int, string> phonePrefixMap)
        {
            mapStorage.ReadFromSortedMap(phonePrefixMap);
            var byteArrayOutputStream = new MemoryStream();
            using (var writer = new BinaryWriter(byteArrayOutputStream))
            {
                mapStorage.WriteExternal(writer);
                writer.Flush();
                return (int) writer.BaseStream.Length;
            }
        }

        private PhonePrefixMapStorageStrategy CreateDefaultMapStorage()
        {
            return new DefaultMapStorage();
        }

        private PhonePrefixMapStorageStrategy CreateFlyweightMapStorage()
        {
            return new FlyweightMapStorage();
        }

        /**
         * Gets the smaller phone prefix map storage strategy according to the provided phone prefix map.
         * It actually uses (outputs the data to a stream) both strategies and retains the best one which
         * make this method quite expensive.
         */
        internal PhonePrefixMapStorageStrategy GetSmallerMapStorage(SortedDictionary<int, string> phonePrefixMap)
        {
            try
            {
                var flyweightMapStorage = CreateFlyweightMapStorage();
                var sizeOfFlyweightMapStorage = GetSizeOfPhonePrefixMapStorage(flyweightMapStorage,
                    phonePrefixMap);

                var defaultMapStorage = CreateDefaultMapStorage();
                var sizeOfDefaultMapStorage = GetSizeOfPhonePrefixMapStorage(defaultMapStorage,
                    phonePrefixMap);

                return sizeOfFlyweightMapStorage < sizeOfDefaultMapStorage
                    ? flyweightMapStorage
                    : defaultMapStorage;
            }
            catch (IOException)
            {
                return CreateFlyweightMapStorage();
            }
        }

        /**
         * Creates an {@link PhonePrefixMap} initialized with {@code sortedPhonePrefixMap}.  Note that the
         * underlying implementation of this method is expensive thus should not be called by
         * time-critical applications.
         *
         * @param sortedPhonePrefixMap  a map from phone number prefixes to descriptions of those prefixes
         * sorted in ascending order of the phone number prefixes as integers.
         */
        public void ReadPhonePrefixMap(SortedDictionary<int, string> sortedPhonePrefixMap)
        {
            phonePrefixMapStorage = GetSmallerMapStorage(sortedPhonePrefixMap);
        }

        /**
         * Supports Java Serialization.
         */
        public void ReadExternal(BinaryReader objectInput)
        {
            // Read the phone prefix map storage strategy flag.
            var useFlyweightMapStorage = objectInput.ReadBoolean();
            if (useFlyweightMapStorage)
            {
                phonePrefixMapStorage = new FlyweightMapStorage();
            }
            else
            {
                phonePrefixMapStorage = new DefaultMapStorage();
            }
            phonePrefixMapStorage.ReadExternal(objectInput);
        }

        /**
         * Supports Java Serialization.
         */
        public void WriteExternal(BinaryWriter objectOutput)
        {
            objectOutput.Write(phonePrefixMapStorage is FlyweightMapStorage);
            phonePrefixMapStorage.WriteExternal(objectOutput);
        }

        /**
         * Returns the description of the {@code number}. This method distinguishes the case of an invalid
         * prefix and a prefix for which the name is not available in the current language. If the
         * description is not available in the current language an empty string is returned. If no
         * description was found for the provided number, null is returned.
         *
         * @param number  the phone number to look up
         * @return  the description of the number
         */
        internal string Lookup(long number)
        {
            var numOfEntries = phonePrefixMapStorage.GetNumOfEntries();
            if (numOfEntries == 0)
            {
                return null;
            }
            var phonePrefix = number;
            var currentIndex = numOfEntries - 1;
            var currentSetOfLengths = phonePrefixMapStorage.GetPossibleLengths();
            while (currentSetOfLengths.Count > 0)
            {
                var possibleLength = currentSetOfLengths.Last();
                var phonePrefixStr = phonePrefix.ToString();
                if (phonePrefixStr.Length > possibleLength)
                {
                    phonePrefix = long.Parse(phonePrefixStr.Substring(0, possibleLength));
                }
                currentIndex = BinarySearch(0, currentIndex, phonePrefix);
                if (currentIndex < 0)
                {
                    return null;
                }
                var currentPrefix = phonePrefixMapStorage.GetPrefix(currentIndex);
                if (phonePrefix == currentPrefix)
                {
                    return phonePrefixMapStorage.GetDescription(currentIndex);
                }
                currentSetOfLengths = new SortedSet<int>(currentSetOfLengths.Where(l => l < possibleLength));
            }
            return null;
        }

        /**
         * As per {@link #lookup(long)}, but receives the number as a PhoneNumber instead of a long.
         *
         * @param number  the phone number to look up
         * @return  the description corresponding to the prefix that best matches this phone number
         */
        public string Lookup(PhoneNumber number)
        {
            var phonePrefix = long.Parse(number.CountryCode + phoneUtil.GetNationalSignificantNumber(number));
            return Lookup(phonePrefix);
        }

        /**
         * Does a binary search for {@code value} in the provided array from {@code start} to {@code end}
         * (inclusive). Returns the position if {@code value} is found; otherwise, returns the
         * position which has the largest value that is less than {@code value}. This means if
         * {@code value} is the smallest, -1 will be returned.
         */
        private int BinarySearch(int start, int end, long value)
        {
            var current = 0;
            while (start <= end)
            {
                current = (start + end) >> 1;
                var currentValue = phonePrefixMapStorage.GetPrefix(current);
                if (currentValue == value)
                {
                    return current;
                }
                else if (currentValue > value)
                {
                    current--;
                    end = current;
                }
                else
                {
                    start = current + 1;
                }
            }
            return current;
        }

        /**
         * Dumps the mappings contained in the phone prefix map.
         */
        public override string ToString()
        {
            return phonePrefixMapStorage.ToString();
        }
    }
}
