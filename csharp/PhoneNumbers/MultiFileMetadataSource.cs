/*
 * Copyright (C) 2015 The Libphonenumber Authors
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

using System.Collections.Concurrent;
using System.Collections.Generic;

namespace PhoneNumbers
{
/**
 * Implementation of {@link MetadataSource} that reads from multiple resource files.
 */
    sealed class MultiFileMetadataSource : IMetadataSource
    {
        // The prefix of the binary files containing phone number metadata for different regions.
        // This enables us to set up with different metadata, such as for testing.
        private readonly string phoneNumberMetadataFilePrefix;

        // The {@link MetadataLoader} used to inject alternative metadata sources.
        private readonly IMetadataLoader metadataLoader;

        // A mapping from a region code to the phone number metadata for that region code.
        // Unlike the mappings for alternate formats and short number metadata, the phone number metadata
        // is loaded from a non-statically determined file prefix; therefore this map is bound to the
        // instance and not static.
        private static readonly ConcurrentDictionary<string, PhoneMetadata> GeographicalRegions =
        new ConcurrentDictionary<string, PhoneMetadata>();

        // A mapping from a country calling code for a non-geographical entity to the phone number
        // metadata for that country calling code. Examples of the country calling codes include 800
        // (International Toll Free Service) and 808 (International Shared Cost Service).
        // Unlike the mappings for alternate formats and short number metadata, the phone number metadata
        // is loaded from a non-statically determined file prefix; therefore this map is bound to the
        // instance and not static.
        private static readonly ConcurrentDictionary<int, PhoneMetadata> NonGeographicalRegions =
        new ConcurrentDictionary<int, PhoneMetadata>();

        // It is assumed that metadataLoader is not null. Checks should happen before passing it in here.
        internal MultiFileMetadataSource(string phoneNumberMetadataFilePrefix, IMetadataLoader metadataLoader)
        {
            this.phoneNumberMetadataFilePrefix = phoneNumberMetadataFilePrefix;
            this.metadataLoader = metadataLoader;
        }

        // It is assumed that metadataLoader is not null. Checks should happen before passing it in here.
        internal MultiFileMetadataSource(IMetadataLoader metadataLoader) :
            this(MetadataManager.MULTI_FILE_PHONE_NUMBER_METADATA_FILE_PREFIX, metadataLoader)
        { }        

        public PhoneMetadata GetMetadataForRegion(string regionCode)
        {
            return MetadataManager.GetMetadataFromMultiFilePrefix(regionCode, GeographicalRegions,
                phoneNumberMetadataFilePrefix, metadataLoader);
        }

        public PhoneMetadata GetMetadataForNonGeographicalRegion(int countryCallingCode)
        {
            if (!IsNonGeographical(countryCallingCode))
            {
                // The given country calling code was for a geographical region.
                return null;
            }

            return MetadataManager.GetMetadataFromMultiFilePrefix(countryCallingCode, NonGeographicalRegions,
                phoneNumberMetadataFilePrefix, metadataLoader);
        }

        // A country calling code is non-geographical if it only maps to the non-geographical region code,
        // i.e. "001".
        private static bool IsNonGeographical(int countryCallingCode)
        {
            List<string> regionCodes =
                CountryCodeToRegionCodeMap.GetCountryCodeToRegionCodeMap()[countryCallingCode];
            return regionCodes.Count == 1
                   && PhoneNumberUtil.RegionCodeForNonGeoEntity.Equals(regionCodes[0]);
        }
    }
}