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
using System.IO;
using System.Linq;

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

        public override string GetDescription(int index)
        {
            return descriptions[index];
        }

        public override void ReadFromSortedMap(SortedDictionary<int, string> sortedAreaCodeMap)
        {
            NumOfEntries = sortedAreaCodeMap.Count;
            phoneNumberPrefixes = new int[NumOfEntries];
            descriptions = new string[NumOfEntries];
            var index = 0;
            foreach (var prefix in sortedAreaCodeMap.Keys)
            {
                phoneNumberPrefixes[index] = prefix;
                descriptions[index++] = sortedAreaCodeMap[prefix];
                PossibleLengths.Add((int)Math.Log10(prefix) + 1);
            }
        }

        public override void ReadExternal(BinaryReader objectInput)
        {
            NumOfEntries = objectInput.ReadInt32();
            if (phoneNumberPrefixes == null || phoneNumberPrefixes.Length < NumOfEntries)
            {
                phoneNumberPrefixes = new int[NumOfEntries];
            }
            if (descriptions == null || descriptions.Length < NumOfEntries)
            {
                descriptions = new string[NumOfEntries];
            }
            for (var i = 0; i < NumOfEntries; i++)
            {
                phoneNumberPrefixes[i] = objectInput.ReadInt32();
                descriptions[i] = objectInput.ReadString();
            }
            var sizeOfLengths = objectInput.ReadInt32();
            PossibleLengths.Clear();
            for (var i = 0; i < sizeOfLengths; i++)
            {
                PossibleLengths.Add(objectInput.ReadInt32());
            }
        }

        public override void WriteExternal(BinaryWriter objectOutput)
        {
            objectOutput.Write(NumOfEntries);
            for (var i = 0; i < NumOfEntries; i++)
            {
                objectOutput.Write(phoneNumberPrefixes[i]);
                objectOutput.Write(descriptions[i]);
            }
            var sizeOfLengths = PossibleLengths.Count;
            objectOutput.Write(sizeOfLengths);
            foreach (var length in PossibleLengths)
            {
                objectOutput.Write(length);
            }
        }
    }
}