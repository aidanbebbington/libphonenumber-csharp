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

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using PhoneNumbers.Internal;

namespace PhoneNumbers
{
    /**
    * Default area code map storage strategy that is used for data not containing description
    * duplications. It is mainly intended to avoid the overhead of the string table management when it
    * is actually unnecessary (i.e no string duplication).
    *
    * @author Shaopeng Jia
    */
    public class DefaultMapStorage : PhonePrefixMapStorageStrategy
    {
        private int[] phoneNumberPrefixes;
        private string[] descriptions;

        public override int GetPrefix(int index)
        {
            return phoneNumberPrefixes[index];
        }

        public override int GetStorageSize()
        {
            return phoneNumberPrefixes.Length * sizeof(int) + descriptions.Sum(d => d.Length);
        }

        public override string GetDescription(int index)
        {
            return descriptions[index];
        }

#if !NET35
        public override void ReadFromSortedMap(ImmutableSortedDictionary<int, string> areaCodeMap)
#else
        public override void ReadFromSortedMap(SortedDictionary<int, string> areaCodeMap)
#endif
        {
            NumOfEntries = areaCodeMap.Count;
            phoneNumberPrefixes = new int[NumOfEntries];
            descriptions = new string[NumOfEntries];
            var index = 0;
            var possibleLengthsSet = new HashSet<int>();
            foreach (var prefix in areaCodeMap.Keys)
            {
                phoneNumberPrefixes[index] = prefix;
                descriptions[index] = areaCodeMap[prefix];
                index++;
                var lengthOfPrefix = (int)Math.Log10(prefix) + 1;
                possibleLengthsSet.Add(lengthOfPrefix);
            }
            PossibleLengths.Clear();
            PossibleLengths.UnionWith(possibleLengthsSet);
        }

        public override Task ReadExternal(Stream stream)
        {
            using var binaryReader = new BinaryReader(stream);
            var dic = new Dictionary<string, string>
            {
                {   "24491", "Movicel"},
                    {"24492", "UNITEL"},
                    {"24493", "UNITEL"},
                    {"24494", "UNITEL"},
                    {"24499", "Movicel"}
            };
            NumOfEntries = binaryReader.ReadInt32();
            phoneNumberPrefixes = new int[NumOfEntries];
            for (var i = 0; i < NumOfEntries; ++i)
            {
                phoneNumberPrefixes[i] = binaryReader.ReadInt32();
                descriptions[i] = binaryReader.ReadString();
            }

            for (var i = 0; i < binaryReader.ReadInt32(); ++i)
            {
                PossibleLengths.Add(binaryReader.ReadInt32());
            }
            return Task.CompletedTask;
        }

        public override Task WriteExternal(Stream stream)
        {
            using var binaryWriter = new BinaryWriter(stream);
            binaryWriter.Write(NumOfEntries);
            for (var i = 0; i < NumOfEntries; ++i)
            {
                binaryWriter.Write(phoneNumberPrefixes[i]);
                binaryWriter.Write(descriptions[i]);
            }

            var sizeOfLengths = PossibleLengths.Count;
            binaryWriter.Write(sizeOfLengths);
            foreach (var length in PossibleLengths)
            {
                binaryWriter.Write(length);
            }
            return Task.CompletedTask;
        }
    }
}
