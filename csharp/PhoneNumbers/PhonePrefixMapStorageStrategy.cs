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
 * Abstracts the way phone prefix data is stored into memory and serialized to a stream. It is used
 * by {@link PhonePrefixMap} to support the most space-efficient storage strategy according to the
 * provided data.
 *
 * @author Philippe Liard
 */

using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PhoneNumbers
{
    public abstract class PhonePrefixMapStorageStrategy
    {
        protected int NumOfEntries = 0;
        protected readonly SortedSet<int> PossibleLengths = new SortedSet<int>();

        /**
        * Gets the phone number prefix located at the provided {@code index}.
        *
        * @param index  the index of the prefix that needs to be returned
        * @return  the phone number prefix at the provided index
        */
        public abstract int GetPrefix(int index);

        /**
        * Gets the description corresponding to the phone number prefix located at the provided {@code
        * index}. If the description is not available in the current language an empty string is
        * returned.
        *
        * @param index  the index of the phone number prefix that needs to be returned
        * @return  the description corresponding to the phone number prefix at the provided index
        */
        public abstract string GetDescription(int index);

        /**
        * Sets the internal state of the underlying storage implementation from the provided {@code
        * sortedPhonePrefixMap} that maps phone number prefixes to description strings.
        *
        * @param sortedPhonePrefixMap  a sorted map that maps phone number prefixes including country
        *    calling code to description strings
        */
        public abstract void ReadFromSortedMap(SortedDictionary<int, string> sortedPhonePrefixMap);

        /**
        * Sets the internal state of the underlying storage implementation reading the provided {@code
        * objectInput}.
        *
        * @param objectInput  the object input stream from which the phone prefix map is read
        * @throws IOException  if an error occurred reading the provided input stream
        */
        public abstract void ReadExternal(BinaryReader objectInput);

        /**
        * Writes the internal state of the underlying storage implementation to the provided {@code
        * objectOutput}.
        *
        * @param objectOutput  the object output stream to which the phone prefix map is written
        * @throws IOException  if an error occurred writing to the provided output stream
        */
        public abstract void WriteExternal(BinaryWriter objectOutput);

        /**
        * @return  the number of entries contained in the phone prefix map
        */
        public int GetNumOfEntries()
        {
            return NumOfEntries;
        }

        /**
        * @return  the set containing the possible lengths of prefixes
        */
        public SortedSet<int> GetPossibleLengths()
        {
            return PossibleLengths;
        }

        public override string ToString()
        {
            var output = new StringBuilder();
            var numOfEntries = GetNumOfEntries();

            for (var i = 0; i < numOfEntries; i++)
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
