/*
 * Copyright (C) 2012 The Libphonenumber Authors
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace PhoneNumbers
{
    /**
    * Class encapsulating loading of PhoneNumber Metadata information. Currently this is used only for
    * additional data files such as PhoneNumberAlternateFormats, but in the future it is envisaged it
    * would handle the main metadata file (PhoneNumberMetaData.xml) as well.
    *
    * @author Lara Rennie
    */
    public class MetadataManager
    {
        internal const string MULTI_FILE_PHONE_NUMBER_METADATA_FILE_PREFIX =
            "/com/google/i18n/phonenumbers/data/PhoneNumberMetadataProto";

        internal const string SINGLE_FILE_PHONE_NUMBER_METADATA_FILE_NAME =
            "/com/google/i18n/phonenumbers/data/SingleFilePhoneNumberMetadataProto";

        internal const string AlternateFormatsFilePrefix = "PhoneNumberAlternateFormats.xml";

        internal const string ShortNumberMetadataFilePrefix = "ShortNumberMetadata.xml";

        private static readonly Dictionary<int, PhoneMetadata> CallingCodeToAlternateFormatsMap =
            new Dictionary<int, PhoneMetadata>();
        const string SHORT_NUMBER_METADATA_FILE_PREFIX =
            "/com/google/i18n/phonenumbers/data/ShortNumberMetadataProto";

        private sealed class MetadataLoader : IMetadataLoader
        {
            public Stream LoadMetadata(string metadataFileName)
            {
#if NET35 || PORTABLE
                return null;
#else
                return new FileStream(metadataFileName, FileMode.Open);
#endif
            }
        }

        private static readonly MetadataLoader DefaultMetadataLoader = new MetadataLoader();

        // A mapping from a country calling code to the alternate formats for that country calling code.
        private static readonly ConcurrentDictionary<int, PhoneMetadata> AlternateFormatsMap =
            new ConcurrentDictionary<int, PhoneMetadata>();

        // A mapping from a region code to the short number metadata for that region code.
        private static readonly ConcurrentDictionary<string, PhoneMetadata> ShortNumberMetadataMap =
            new ConcurrentDictionary<string, PhoneMetadata>();

        // The set of country calling codes for which there are alternate formats. For every country
        // calling code in this set there should be metadata linked into the resources.
        private static readonly HashSet<int> AlternateFormatsCountryCodes =
            AlternateFormatsCountryCodeSet.CountryCodeSet;

        // The set of region codes for which there are short number metadata. For every region code in
        // this set there should be metadata linked into the resources.
        private static readonly HashSet<string> ShortNumberMetadataRegionCodes =
            ShortNumbersRegionCodeSet.RegionCodeSet;

        private MetadataManager()
        {
        }

        private static void LoadAlternateFormatsMedataFromFile(string filePrefix)
        {
#if (NET35 || NET40)
            var asm = Assembly.GetExecutingAssembly();
#else
            var asm = typeof(MetadataManager).GetTypeInfo().Assembly;
#endif
            var name = asm.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith(filePrefix)) ?? "missing";
            var meta = BuildMetadataFromXml.BuildPhoneMetadataCollection(name, false, false, false, true); // todo lite/special build
            foreach (var m in meta.MetadataList)
            {
                CallingCodeToAlternateFormatsMap[m.CountryCode] = m;
            }
        }

        private static void LoadShortNumberMedataFromFile(string filePrefix)
        {
#if (NET35 || NET40)
            var asm = Assembly.GetExecutingAssembly();
#else
            var asm = typeof(MetadataManager).GetTypeInfo().Assembly;
#endif
            var name = asm.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith(filePrefix)) ?? "missing";
            var meta = BuildMetadataFromXml.BuildPhoneMetadataCollection(name, false, false, true, false); // todo lite/special build
            foreach (var m in meta.MetadataList)
            {
                ShortNumberMetadataMap[m.Id] = m;
            }
        }
        
        internal static PhoneMetadata GetAlternateFormatsForCountry(int countryCallingCode)
        {
            if (!AlternateFormatsCountryCodes.Contains(countryCallingCode))
            {
                return null;
            }

            return GetMetadataFromMultiFilePrefix(countryCallingCode, AlternateFormatsMap, AlternateFormatsFilePrefix,
                DefaultMetadataLoader);
        }

        internal static PhoneMetadata GetShortNumberMetadataForRegion(string regionCode)
        {
            if (!ShortNumberMetadataRegionCodes.Contains(regionCode))
            {
                return null;
            }

            return GetMetadataFromMultiFilePrefix(regionCode, ShortNumberMetadataMap,
                SHORT_NUMBER_METADATA_FILE_PREFIX, DefaultMetadataLoader);
        }

        internal static HashSet<string> GetSupportedShortNumberRegions()
        {
            return ShortNumberMetadataRegionCodes;
        }

        /**
         * @param key  the lookup key for the provided map, typically a region code or a country calling
         *     code
         * @param map  the map containing mappings of already loaded metadata from their {@code key}. If
         *     this {@code key}'s metadata isn't already loaded, it will be added to this map after
         *     loading
         * @param filePrefix  the prefix of the file to load metadata from
         * @param metadataLoader  the metadata loader used to inject alternative metadata sources
         */
        internal static PhoneMetadata GetMetadataFromMultiFilePrefix<T>(T key,
            ConcurrentDictionary<T, PhoneMetadata> map, string filePrefix, IMetadataLoader metadataLoader)
        {
            map.TryGetValue(key, out var metadata);
            if (metadata != null)
            {
                return metadata;
            }

            // We assume key.toString() is well-defined.
            var fileName = filePrefix + "_" + key;
            var metadataList = GetMetadataFromSingleFileName(fileName, metadataLoader);
            metadata = metadataList[0];
            var oldValue = map.GetOrAdd(key, metadata);
            return oldValue ?? metadata;
        }

        // Loader and holder for the metadata maps loaded from a single file.
        internal class SingleFileMetadataMaps
        {
            internal static SingleFileMetadataMaps Load(string fileName, IMetadataLoader metadataLoader)
            {
                var metadataList = GetMetadataFromSingleFileName(fileName, metadataLoader);
                var regionCodeToMetadata = new Dictionary<string, PhoneMetadata>();
                var countryCallingCodeToMetadata =
                    new Dictionary<int, PhoneMetadata>();
                foreach (var metadata in metadataList)
                {
                    var regionCode = metadata.Id;
                    if (PhoneNumberUtil.RegionCodeForNonGeoEntity.Equals(regionCode))
                    {
                        // regionCode belongs to a non-geographical entity.
                        countryCallingCodeToMetadata.Add(metadata.CountryCode, metadata);
                    }
                    else
                    {
                        regionCodeToMetadata.Add(regionCode, metadata);
                    }
                }

                return new SingleFileMetadataMaps(regionCodeToMetadata, countryCallingCodeToMetadata);
            }

            // A map from a region code to the PhoneMetadata for that region.
            // For phone number metadata, the region code "001" is excluded, since that is used for the
            // non-geographical phone number entities.
            private readonly Dictionary<string, PhoneMetadata> regionCodeToMetadata;

            // A map from a country calling code to the PhoneMetadata for that country calling code.
            // Examples of the country calling codes include 800 (International Toll Free Service) and 808
            // (International Shared Cost Service).
            // For phone number metadata, only the non-geographical phone number entities' country calling
            // codes are present.
            private readonly Dictionary<int, PhoneMetadata> countryCallingCodeToMetadata;

            internal SingleFileMetadataMaps(Dictionary<string, PhoneMetadata> regionCodeToMetadata,
                Dictionary<int, PhoneMetadata> countryCallingCodeToMetadata)
            {
                this.regionCodeToMetadata = regionCodeToMetadata;
                this.countryCallingCodeToMetadata = countryCallingCodeToMetadata;
            }

            public PhoneMetadata this[string regionCode] => regionCodeToMetadata[regionCode];

            public PhoneMetadata this[int countryCallingCode] => countryCallingCodeToMetadata[countryCallingCode];
        }


        // Manages the atomic reference lifecycle of a SingleFileMetadataMaps encapsulation.
        internal static SingleFileMetadataMaps GetSingleFileMetadataMaps(
            ref SingleFileMetadataMaps atomicReference, string fileName, IMetadataLoader metadataLoader)
        {
            var maps = atomicReference;
            if (maps != null)
            {
                return maps;
            }
            maps = SingleFileMetadataMaps.Load(fileName, metadataLoader);
            Interlocked.CompareExchange(ref atomicReference, maps, null);
            return atomicReference;
        }

        private static IList<PhoneMetadata> GetMetadataFromSingleFileName(string fileName,
            IMetadataLoader metadataLoader)
        {
            var source = metadataLoader.LoadMetadata(fileName);
            if (source == null)
            {
                // Sanity check; this would only happen if we packaged jars incorrectly.
                throw new FileNotFoundException("missing metadata: " + fileName);
            }

            var metadataCollection = LoadMetadataAndCloseInput(source);
            var metadataList = metadataCollection.MetadataList;
            if (!metadataList.Any())
            {
                // Sanity check; this should not happen since we build with non-empty metadata.
                throw new InvalidDataException("empty metadata: " + fileName);
            }

            return metadataList;
        }

        /**
         * Loads and returns the metadata from the given stream and closes the stream.
         *
         * @param source  the non-null stream from which metadata is to be read
         * @return  the loaded metadata
         */
        private static PhoneMetadataCollection LoadMetadataAndCloseInput(Stream source)
        {
            BinaryReader ois = null;
            try
            {
                try
                {
                    ois = new BinaryReader(source);
                }
                catch (IOException e)
                {
                    throw new InvalidDataException("cannot load/parse metadata", e);
                }

                var metadataCollection = new PhoneMetadataCollection.Builder();
                try
                {
                    metadataCollection.AddRangeMetadata(null); //todo.MergeFrom(ConvertStreamToByteBuffer(ois, bufferSize));
                }
                catch (IOException e)
                {
                    throw new InvalidDataException("cannot load/parse metadata", e);
                }

                return metadataCollection.Build();
            }
            finally
            {
                try
                {
                    if (ois != null)
                    {
                        // This will close all underlying streams as well, including source.
#if !NET35
                        ois.Dispose();
#endif
                    }
                    else
                    {
                        source.Dispose();
                    }
                }
                catch (IOException)
                {
                }
            }
        }
    }
}
