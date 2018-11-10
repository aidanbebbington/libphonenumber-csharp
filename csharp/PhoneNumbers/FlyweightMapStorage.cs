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
    * Flyweight area code map storage strategy that uses a table to store unique strings and shorts to
    * store the prefix and description indexes when possible. It is particularly space-efficient when
    * the provided area code map contains a lot of description redundant descriptions.
    *
    * @author Philippe Liard
    */
    public class FlyweightMapStorage : PhonePrefixMapStorageStrategy
    {
        internal class ByteBuffer
        {
            private readonly MemoryStream stream;
            private readonly BinaryReader reader;
            private readonly BinaryWriter writer;

            internal ByteBuffer(int size)
            {
                stream = new MemoryStream(new byte[size]);
                reader = new BinaryReader(stream);
                writer = new BinaryWriter(stream);
            }

            internal void PutShort(int offset, short value)
            {
                stream.Seek(offset, SeekOrigin.Begin);
                writer.Write(value);
            }

            internal void PutInt(int offset, int value)
            {
                stream.Seek(offset, SeekOrigin.Begin);
                writer.Write(value);
            }

            internal short GetShort(int offset)
            {
                stream.Seek(offset, SeekOrigin.Begin);
                return reader.ReadInt16();
            }

            internal int GetInt(int offset)
            {
                stream.Seek(offset, SeekOrigin.Begin);
                return reader.ReadInt32();
            }

            internal int GetCapacity()
            {
                return stream.Capacity;
            }
        }

        // Size of short and integer types in bytes.
        private static readonly int ShortNumBytes = sizeof(short);
        private static readonly int IntNumBytes = sizeof(int);

        // The number of bytes used to store a phone number prefix.
        private int prefixSizeInBytes;
        // The number of bytes used to store a description index. It is computed from the size of the
        // description pool containing all the strings.
        private int descIndexSizeInBytes;

        private ByteBuffer phoneNumberPrefixes;
        private ByteBuffer descriptionIndexes;

        // Sorted string array of unique description strings.
        private string[] descriptionPool;

        public override int GetPrefix(int index)
        {
            return ReadWordFromBuffer(phoneNumberPrefixes, prefixSizeInBytes, index);
        }

        /**
        * This implementation returns the same string (same identity) when called for multiple indexes
        * corresponding to prefixes that have the same description.
        */
        public override string GetDescription(int index)
        {
            var indexInDescriptionPool =
                ReadWordFromBuffer(descriptionIndexes, descIndexSizeInBytes, index);
            return descriptionPool[indexInDescriptionPool];
        }

        public override void ReadFromSortedMap(SortedDictionary<int, string> areaCodeMap)
        {
            var descriptionsSet = new HashSet<string>();
            NumOfEntries = areaCodeMap.Count;
            prefixSizeInBytes = GetOptimalNumberOfBytesForValue(areaCodeMap.Keys.Last());
            phoneNumberPrefixes = new ByteBuffer(NumOfEntries * prefixSizeInBytes);

            // Fill the phone number prefixes byte buffer, the set of possible lengths of prefixes and the
            // description set.
            var index = 0;
            var possibleLengthsSet = new HashSet<int>();
            foreach (var entry in areaCodeMap)
            {
                var prefix = entry.Key;
                StoreWordInBuffer(phoneNumberPrefixes, prefixSizeInBytes, index, prefix);
                var lengthOfPrefixRef = (int)Math.Log10(prefix) + 1;
                possibleLengthsSet.Add(lengthOfPrefixRef);
                descriptionsSet.Add(entry.Value);
                index++;
            }
            PossibleLengths.Clear();
            foreach (var length in possibleLengthsSet)
            {
                PossibleLengths.Add(length);
            }
            CreateDescriptionPool(descriptionsSet, areaCodeMap);
        }

        /**
        * Creates the description pool from the provided set of string descriptions and area code map.
        */
        private void CreateDescriptionPool(HashSet<string> descriptionsSet, SortedDictionary<int, string> areaCodeMap)
        {
            // Create the description pool.
            descIndexSizeInBytes = GetOptimalNumberOfBytesForValue(descriptionsSet.Count - 1);
            descriptionIndexes = new ByteBuffer(NumOfEntries * descIndexSizeInBytes);
            descriptionPool = descriptionsSet.ToArray();
            Array.Sort(descriptionPool);

            // Map the phone number prefixes to the descriptions.
            var index = 0;
            for (var i = 0; i < NumOfEntries; i++)
            {
                var prefix = ReadWordFromBuffer(phoneNumberPrefixes, prefixSizeInBytes, i);
                var description = areaCodeMap[prefix];
                var positionInDescriptionPool = Array.BinarySearch(descriptionPool, description);
                StoreWordInBuffer(descriptionIndexes, descIndexSizeInBytes, index,
                                  positionInDescriptionPool);
                index++;
            }
        }

        public override void ReadExternal(BinaryReader objectInput)
        {
            // Read binary words sizes.
            prefixSizeInBytes = objectInput.ReadInt32();
            descIndexSizeInBytes = objectInput.ReadInt32();

            // Read possible lengths.
            var sizeOfLengths = objectInput.ReadInt32();
            PossibleLengths.Clear();
            for (var i = 0; i < sizeOfLengths; i++)
            {
                PossibleLengths.Add(objectInput.ReadInt32());
            }

            // Read description pool size.
            var descriptionPoolSize = objectInput.ReadInt32();
            // Read description pool.
            if (descriptionPool == null || descriptionPool.Length < descriptionPoolSize)
            {
                descriptionPool = new string[descriptionPoolSize];
            }
            for (var i = 0; i < descriptionPoolSize; i++)
            {
                var description = objectInput.ReadString();
                descriptionPool[i] = description;
            }
            ReadEntries(objectInput);
        }

        /**
        * Reads the phone prefix entries from the provided input stream and stores them to the internal
        * byte buffers.
        */
        private void ReadEntries(BinaryReader reader)
        {
            NumOfEntries = reader.ReadInt32();
            if (phoneNumberPrefixes == null || phoneNumberPrefixes.GetCapacity() < NumOfEntries)
            {
                phoneNumberPrefixes = new ByteBuffer(NumOfEntries * prefixSizeInBytes);
            }
            if (descriptionIndexes == null || descriptionIndexes.GetCapacity() < NumOfEntries)
            {
                descriptionIndexes = new ByteBuffer(NumOfEntries * descIndexSizeInBytes);
            }
            for (var i = 0; i < NumOfEntries; i++)
            {
                ReadExternalWord(reader, prefixSizeInBytes, phoneNumberPrefixes, i);
                ReadExternalWord(reader, descIndexSizeInBytes, descriptionIndexes, i);
            }
        }

        public override void WriteExternal(BinaryWriter writer)
        {
            // Write binary words sizes.
            writer.Write(prefixSizeInBytes);
            writer.Write(descIndexSizeInBytes);

            // Write possible lengths.
            var sizeOfLengths = PossibleLengths.Count;
            writer.Write(sizeOfLengths);
            foreach (var length in PossibleLengths)
            {
                writer.Write(length);
            }

            // Write description pool size.
            writer.Write(descriptionPool.Length);
            // Write description pool.
            foreach (var description in descriptionPool)
            {
                writer.Write(description);
            }

            // Write entries.
            writer.Write(NumOfEntries);
            for (var i = 0; i < NumOfEntries; i++)
            {
                WriteExternalWord(writer, prefixSizeInBytes, phoneNumberPrefixes, i);
                WriteExternalWord(writer, descIndexSizeInBytes, descriptionIndexes, i);
            }
        }

        /**
         * Gets the minimum number of bytes that can be used to store the provided {@code value}.
         */
        private static int GetOptimalNumberOfBytesForValue(int value)
        {
            return value <= short.MaxValue ? ShortNumBytes : IntNumBytes;
        }

        /**
        * Stores a value which is read from the provided {@code objectInput} to the provided byte {@code
        * buffer} at the specified {@code index}.
        *
        * @param objectInput  the object input stream from which the value is read
        * @param wordSize  the number of bytes used to store the value read from the stream
        * @param outputBuffer  the byte buffer to which the value is stored
        * @param index  the index where the value is stored in the buffer
        * @throws IOException  if an error occurred reading from the object input stream
        */
        private static void ReadExternalWord(BinaryReader objectInput, int wordSize, ByteBuffer outputBuffer, int index)
        {
            var wordIndex = index * wordSize;
            if (wordSize == ShortNumBytes)
            {
                outputBuffer.PutShort(wordIndex, objectInput.ReadInt16());
            }
            else
            {
                outputBuffer.PutInt(wordIndex, objectInput.ReadInt32());
            }
        }

        /**
        * Writes the value read from the provided byte {@code buffer} at the specified {@code index} to
        * the provided {@code objectOutput}.
        *
        * @param objectOutput  the object output stream to which the value is written
        * @param wordSize  the number of bytes used to store the value
        * @param inputBuffer  the byte buffer from which the value is read
        * @param index  the index of the value in the the byte buffer
        * @throws IOException if an error occurred writing to the provided object output stream
        */
        private static void WriteExternalWord(BinaryWriter objectOutput, int wordSize, ByteBuffer inputBuffer, int index)
        {
            var wordIndex = index * wordSize;
            if (wordSize == ShortNumBytes)
            {
                objectOutput.Write(inputBuffer.GetShort(wordIndex));
            }
            else
            {
                objectOutput.Write(inputBuffer.GetInt(wordIndex));
            }
        }

        /**
        * Reads the {@code value} at the specified {@code index} from the provided byte {@code buffer}.
        * Note that only integer and short sizes are supported.
        *
        * @param buffer  the byte buffer from which the value is read
        * @param wordSize  the number of bytes used to store the value
        * @param index  the index where the value is read from
        *
        * @return  the value read from the buffer
        */
        private static int ReadWordFromBuffer(ByteBuffer buffer, int wordSize, int index)
        {
            var wordIndex = index * wordSize;
            return wordSize == ShortNumBytes ? buffer.GetShort(wordIndex) : buffer.GetInt(wordIndex);
        }

        /**
         * Stores the provided {@code value} to the provided byte {@code buffer} at the specified {@code
         * index} using the provided {@code wordSize} in bytes. Note that only integer and short sizes are
         * supported.
         *
         * @param buffer  the byte buffer to which the value is stored
         * @param wordSize  the number of bytes used to store the provided value
         * @param index  the index to which the value is stored
         * @param value  the value that is stored assuming it does not require more than the specified
         *    number of bytes.
         */
        private static void StoreWordInBuffer(ByteBuffer buffer, int wordSize, int index, int value)
        {
            index *= wordSize;
            if (wordSize == ShortNumBytes)
            {
                buffer.PutShort(index, (short)value);
            }
            else
            {
                buffer.PutInt(index, value);
            }
        }
    }
}