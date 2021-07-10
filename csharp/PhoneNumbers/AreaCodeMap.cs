/*/*
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
 #1#

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace PhoneNumbers
{
    /// <summary>
    /// A utility that maps phone number prefixes to a string describing the geographical area the prefix
    /// covers.
    /// <!-- @author Shaopeng Jia -->
    /// </summary>
    public class AreaCodeMap
    {
        private readonly PhoneNumberUtil phoneUtil = PhoneNumberUtil.GetInstance();

        private PhonePrefixMapStorageStrategy phonePrefixMapStorage;

        public PhonePrefixMapStorageStrategy GetAreaCodeMapStorage()
        {
            return phonePrefixMapStorage;
        }

        /**
         * Creates an empty {@link AreaCodeMap}. The default constructor is necessary for implementing
         * {@link Externalizable}. The empty map could later be populated by
         * {@link #readAreaCodeMap(java.util.SortedMap)} or {@link #readExternal(java.io.ObjectInput)}.
         #1#

        /// <summary>
        /// Gets the size of the provided area code map storage. The map storage passed-in will be filled
        /// as a result.
        /// </summary>
        /// <param name="mapStorage"></param>
        /// <param name="areaCodeMap"></param>
        /// <returns></returns>
        private static int GetSizeOfAreaCodeMapStorage(PhonePrefixMapStorageStrategy mapStorage,
#if !NET35
            ImmutableSortedDictionary<int, string> areaCodeMap)
#else
            SortedDictionary<int, string> areaCodeMap)
#endif
        {
            mapStorage.ReadFromSortedMap(areaCodeMap);
            return mapStorage.GetStorageSize();
        }

        private static PhonePrefixMapStorageStrategy CreateDefaultMapStorage()
        {
            return new DefaultMapStorage();
        }

        private static PhonePrefixMapStorageStrategy CreateFlyweightMapStorage()
        {
            return new FlyweightMapStorage();
        }

        /// <summary>
        /// Gets the smaller area code map storage strategy according to the provided area code map.
        /// It actually uses (outputs the data to a stream) both strategies and retains the best one
        /// which make this method quite expensive.
        /// </summary>
        /// <param name="areaCodeMap"></param>
        /// <returns></returns>
        [Obsolete("This method was public to be @VisibleForTesting, it will be moved to internal in a future release.")]
        public PhonePrefixMapStorageStrategy GetSmallerMapStorage(SortedDictionary<int, string> areaCodeMap)
            => GetSmallerMapStorageInternal(areaCodeMap.ToImmutableSortedDictionary());

#if !NET35
        internal PhonePrefixMapStorageStrategy GetSmallerMapStorageInternal(ImmutableSortedDictionary<int, string> areaCodeMap)
#else
        internal PhonePrefixMapStorageStrategy GetSmallerMapStorageInternal(SortedDictionary<int, string> areaCodeMap)
#endif
        {
            var flyweightMapStorage = CreateFlyweightMapStorage();
            var sizeOfFlyweightMapStorage = GetSizeOfAreaCodeMapStorage(flyweightMapStorage, areaCodeMap);

            var defaultMapStorage = CreateDefaultMapStorage();
            var sizeOfDefaultMapStorage = GetSizeOfAreaCodeMapStorage(defaultMapStorage, areaCodeMap);

            return sizeOfFlyweightMapStorage < sizeOfDefaultMapStorage
                ? flyweightMapStorage : defaultMapStorage;
        }

        /// <summary>
        /// Creates an <see cref="AreaCodeMap"/> initialized with sortedAreaCodeMap.  Note that the
        /// underlying implementation of this method is expensive thus should not be called by
        /// time-critical applications.
        /// </summary>
        /// <param name="sortedAreaCodeMap">A map from phone number prefixes to descriptions of corresponding
        /// geographical areas, sorted in ascending order of the phone number prefixes as integers.</param>
        public void ReadAreaCodeMap(SortedDictionary<int, string> sortedAreaCodeMap)
        {
            phonePrefixMapStorage = GetSmallerMapStorageInternal(sortedAreaCodeMap);
        }

        /// <summary>
        /// Returns the description of the geographical area the number corresponds to. This method
        /// distinguishes the case of an invalid prefix and a prefix for which the name is not available in
        /// the current language. If the description is not available in the current language an empty
        /// string is returned. If no description was found for the provided number, null is returned.
        /// </summary>
        /// <param name="number">The phone number to look up.</param>
        /// <returns>The description of the geographical area.</returns>
        public string Lookup(PhoneNumber number)
        {
            var numOfEntries = phonePrefixMapStorage.GetNumOfEntries();
            if (numOfEntries == 0)
            {
                return null;
            }
            var phonePrefix =
                long.Parse(number.CountryCode + phoneUtil.GetNationalSignificantNumber(number));
            var currentIndex = numOfEntries - 1;
            var currentSetOfLengths = phonePrefixMapStorage.GetPossibleLengths();
            var length = currentSetOfLengths.Count;
            while (length > 0)
            {
                var possibleLength = currentSetOfLengths.ElementAt(length - 1);
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
                while (length > 0 && currentSetOfLengths.ElementAt(length - 1) >= possibleLength)
                    length--;
            }
            return null;
        }

        /// <summary>
        /// Does a binary search for value in the provided array from start to end
        /// (inclusive). Returns the position if value is found; otherwise, returns the
        /// position which has the largest value that is less than value. This means if
        /// value is the smallest, -1 will be returned.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private int BinarySearch(int start, int end, long value)
        {
            var current = 0;
            while (start <= end)
            {
                current = (start + end) / 2;
                var currentValue = phonePrefixMapStorage.GetPrefix(current);
                if (currentValue == value)
                {
                    return current;
                }
                if (currentValue > value)
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

        /// <summary>
        /// Dumps the mappings contained in the area code map.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return phonePrefixMapStorage.ToString();
        }
    }
}*/