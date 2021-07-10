// /*
//  * Copyright (C) 2011 The Libphonenumber Authors
//  *
//  * Licensed under the Apache License, Version 2.0 (the "License");
//  * you may not use this file except in compliance with the License.
//  * You may obtain a copy of the License at
//  *
//  * http://www.apache.org/licenses/LICENSE-2.0
//  *
//  * Unless required by applicable law or agreed to in writing, software
//  * distributed under the License is distributed on an "AS IS" BASIS,
//  * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  * See the License for the specific language governing permissions and
//  * limitations under the License.
//  */
//
// using System;
// using System.Collections.Generic;
// using System.Collections.Immutable;
// using System.IO;
// using System.Linq;
// using PhoneNumbers;
//
// /**
//  * A utility that maps phone number prefixes to a description string, which may be, for example,
//  * the geographical area the prefix covers.
//  *
//  * @author Shaopeng Jia
//  */
// public class PhonePrefixMap  {
//   private static readonly PhoneNumberUtil phoneUtil = PhoneNumberUtil.GetInstance();
//
//   private PhonePrefixMapStorageStrategy phonePrefixMapStorage;
//   
//   internal PhonePrefixMapStorageStrategy getPhonePrefixMapStorage() {
//     return phonePrefixMapStorage;
//   }
//
//   /**
//    * Creates an empty {@link PhonePrefixMap}. The default constructor is necessary for implementing
//    * {@link Externalizable}. The empty map could later be populated by
//    * {@link #readPhonePrefixMap(java.util.SortedMap)} or {@link #readExternal(java.io.ObjectInput)}.
//    */
//   public PhonePrefixMap() {}
//
//   /**
//    * Gets the size of the provided phone prefix map storage. The map storage passed-in will be
//    * filled as a result.
//    */
//   private static long getSizeOfPhonePrefixMapStorage(PhonePrefixMapStorageStrategy mapStorage,
//       ImmutableSortedDictionary<int, String> phonePrefixMap) {
//     mapStorage.ReadFromSortedMap(phonePrefixMap);
//     Stream stream = new Stream();
//     mapStorage.WriteExternal(stream);
//     stream.Flush();
//     var sizeOfStorage = stream.Length;
//     stream.Close();
//     return sizeOfStorage;
//   }
//
//   private PhonePrefixMapStorageStrategy createDefaultMapStorage() {
//     return new DefaultMapStorage();
//   }
//
//   private PhonePrefixMapStorageStrategy createFlyweightMapStorage() {
//     return new FlyweightMapStorage();
//   }
//
//   /**
//    * Gets the smaller phone prefix map storage strategy according to the provided phone prefix map.
//    * It actually uses (outputs the data to a stream) both strategies and retains the best one which
//    * make this method quite expensive.
//    */
//   // @VisibleForTesting
//   PhonePrefixMapStorageStrategy getSmallerMapStorage(ImmutableSortedDictionary<int, String> phonePrefixMap) {
//     try {
//       PhonePrefixMapStorageStrategy flyweightMapStorage = createFlyweightMapStorage();
//       var sizeOfFlyweightMapStorage = getSizeOfPhonePrefixMapStorage(flyweightMapStorage, phonePrefixMap);
//
//       PhonePrefixMapStorageStrategy defaultMapStorage = createDefaultMapStorage();
//       var sizeOfDefaultMapStorage = getSizeOfPhonePrefixMapStorage(defaultMapStorage, phonePrefixMap);
//
//       return sizeOfFlyweightMapStorage < sizeOfDefaultMapStorage
//           ? flyweightMapStorage : defaultMapStorage;
//     } catch (IOException) {
//       return createFlyweightMapStorage();
//     }
//   }
//
//   /**
//    * Creates an {@link PhonePrefixMap} initialized with {@code sortedPhonePrefixMap}.  Note that the
//    * underlying implementation of this method is expensive thus should not be called by
//    * time-critical applications.
//    *
//    * @param sortedPhonePrefixMap  a map from phone number prefixes to descriptions of those prefixes
//    * sorted in ascending order of the phone number prefixes as ints.
//    */
//   public void readPhonePrefixMap(ImmutableSortedDictionary<int, String> sortedPhonePrefixMap) {
//     phonePrefixMapStorage = getSmallerMapStorage(sortedPhonePrefixMap);
//   }
//
//   /**
//    * Supports Java Serialization.
//    */
//   public void readExternal(Stream stream) {
//     // Read the phone prefix map storage strategy flag.
//     using var reader = new BinaryReader(stream);
//     bool useFlyweightMapStorage = reader.ReadBoolean();
//     if (useFlyweightMapStorage) {
//       phonePrefixMapStorage = new FlyweightMapStorage();
//     } else {
//       phonePrefixMapStorage = new DefaultMapStorage();
//     }
//     phonePrefixMapStorage.ReadExternal(stream);
//   }
//
//   /**
//    * Supports Java Serialization.
//    */
//   public void writeExternal(Stream stream) {
//     using var writer = new BinaryWriter(stream);
//     writer.Write(phonePrefixMapStorage is FlyweightMapStorage);
//     phonePrefixMapStorage.WriteExternal(stream);
//   }
//
//   /**
//    * Returns the description of the {@code number}. This method distinguishes the case of an invalid
//    * prefix and a prefix for which the name is not available in the current language. If the
//    * description is not available in the current language an empty string is returned. If no
//    * description was found for the provided number, null is returned.
//    *
//    * @param number  the phone number to look up
//    * @return  the description of the number
//    */
//   string lookup(long number) {
//     int numOfEntries = phonePrefixMapStorage.NumOfEntries;
//     if (numOfEntries == 0) {
//       return null;
//     }
//     long phonePrefix = number;
//     int currentIndex = numOfEntries - 1;
//     ImmutableSortedSet<int> currentSetOfLengths = phonePrefixMapStorage.PossibleLengths;
//     while (currentSetOfLengths.Count > 0) {
//       int possibleLength = currentSetOfLengths.Last();
//       string phonePrefixStr = phonePrefix.ToString();
//       if (phonePrefixStr.Length > possibleLength) {
//         phonePrefix = long.Parse(phonePrefixStr.Substring(0, possibleLength));
//       }
//       currentIndex = binarySearch(0, currentIndex, phonePrefix);
//       if (currentIndex < 0) {
//         return null;
//       }
//       int currentPrefix = phonePrefixMapStorage.GetPrefix(currentIndex);
//       if (phonePrefix == currentPrefix) {
//         return phonePrefixMapStorage.GetDescription(currentIndex);
//       }
//       currentSetOfLengths = currentSetOfLengths.Take(possibleLength).ToImmutableSortedSet();
//     }
//     return null;
//   }
//
//   /**
//    * As per {@link #lookup(long)}, but receives the number as a PhoneNumber instead of a long.
//    *
//    * @param number  the phone number to look up
//    * @return  the description corresponding to the prefix that best matches this phone number
//    */
//   public string lookup(PhoneNumber number) {
//     long phonePrefix =
//         long.Parse(number.CountryCode + phoneUtil.GetNationalSignificantNumber(number));
//     return lookup(phonePrefix);
//   }
//
//   /**
//    * Does a binary search for {@code value} in the provided array from {@code start} to {@code end}
//    * (inclusive). Returns the position if {@code value} is found; otherwise, returns the
//    * position which has the largest value that is less than {@code value}. This means if
//    * {@code value} is the smallest, -1 will be returned.
//    */
//   private int binarySearch(int start, int end, long value) {
//     int current = 0;
//     while (start <= end) {
//       current = (start + end) >> 1;
//       int currentValue = phonePrefixMapStorage.GetPrefix(current);
//       if (currentValue == value) {
//         return current;
//       } else if (currentValue > value) {
//         current--;
//         end = current;
//       } else {
//         start = current + 1;
//       }
//     }
//     return current;
//   }
//
//   /**
//    * Dumps the mappings contained in the phone prefix map.
//    */
//   public override string ToString() {
//     return phonePrefixMapStorage.ToString();
//   }
// }