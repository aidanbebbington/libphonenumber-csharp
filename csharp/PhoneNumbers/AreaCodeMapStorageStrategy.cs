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
using System.Text;

namespace PhoneNumbers
{
    /// <summary>
    /// Abstracts the way area code data is stored into memory and serialized to a stream. It is used by
    /// <see cref="AreaCodeMap"/> to support the most space-efficient storage strategy according to the
    /// provided data.
    /// <!-- @author Philippe Liard -->
    /// </summary>
    public abstract class AreaCodeMapStorageStrategy
    {
        protected int NumOfEntries = 0;
        protected readonly List<int> PossibleLengths = new();

        /// <summary>
        /// Gets the phone number prefix located at the provided index.
        /// </summary>
        /// <param name="index">The index of the prefix that needs to be returned.</param>
        /// <returns>The phone number prefix at the provided index.</returns>
        public abstract int GetPrefix(int index);

        public abstract int GetStorageSize();

        /// <summary>
        /// Gets the description corresponding to the phone number prefix located at the provided
        /// index. If the description is not available in the current language an empty string is
        /// returned.
        /// </summary>
        /// <param name="index">The index of the phone number prefix that needs to be returned.</param>
        /// <returns>The description corresponding to the phone number prefix at the provided index.</returns>
        public abstract string GetDescription(int index);

        /// <summary>
        /// Sets the internal state of the underlying storage implementation from the provided
        /// sortedAreaCodeMap that maps phone number prefixes to description strings.
        /// </summary>
        /// <param name="sortedAreaCodeMap">A sorted map that maps phone number prefixes including country
        /// calling code to description strings.</param>
        public abstract void ReadFromSortedMap(SortedDictionary<int, string> sortedAreaCodeMap);

        /// <summary>
        /// The number of entries contained in the area code map.
        /// </summary>
        /// <returns>The number of entries contained in the area code map.</returns>
        public int GetNumOfEntries()
        {
            return NumOfEntries;
        }

        /// <summary>
        /// The set containing the possible lengths of prefixes.
        /// </summary>
        /// <returns>The set containing the possible lengths of prefixes.</returns>
        public List<int> GetPossibleLengths()
        {
            return PossibleLengths;
        }

        public override string ToString()
        {
            var output = new StringBuilder();
            for (var i = 0; i < GetNumOfEntries(); i++)
            {
                output.Append(GetPrefix(i))
                    .Append("|")
                    .Append(GetDescription(i))
                    .Append("\n");
            }
            return output.ToString();
        }

    }
}
