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

namespace PhoneNumbers
{

    /**
     * Implementation of {@link MetadataSource} that reads from a single resource file.
     */
    sealed class SingleFileMetadataSource : IMetadataSource
    {
        // The name of the binary file containing phone number metadata for different regions.
        // This enables us to set up with different metadata, such as for testing.
        private readonly string phoneNumberMetadataFileName;

        // The {@link MetadataLoader} used to inject alternative metadata sources.
        private readonly IMetadataLoader metadataLoader;

        private MetadataManager.SingleFileMetadataMaps phoneNumberMetadataAtomicRef;

        // It is assumed that metadataLoader is not null. Checks should happen before passing it in here.
        internal SingleFileMetadataSource(string phoneNumberMetadataFileName, IMetadataLoader metadataLoader)
        {
            this.phoneNumberMetadataFileName = phoneNumberMetadataFileName;
            this.metadataLoader = metadataLoader;
        }

        // It is assumed that metadataLoader is not null. Checks should happen before passing it in here.
        internal SingleFileMetadataSource(IMetadataLoader metadataLoader) : this(MetadataManager.SINGLE_FILE_PHONE_NUMBER_METADATA_FILE_NAME, metadataLoader)
        {
        }

        public PhoneMetadata GetMetadataForRegion(string regionCode)
        {
            return MetadataManager.GetSingleFileMetadataMaps(ref phoneNumberMetadataAtomicRef,
                phoneNumberMetadataFileName, metadataLoader)[regionCode];
        }

        public PhoneMetadata GetMetadataForNonGeographicalRegion(int countryCallingCode)
        {
            // A country calling code is non-geographical if it only maps to the non-geographical region
            // code, i.e. "001". If this is not true of the given country calling code, then we will return
            // null here. If not for the atomic reference, such as if we were loading in multiple stages, we
            // would check that the passed in country calling code was indeed non-geographical to avoid
            // loading costs for a null result. Here though we do not check this since the entire data must
            // be loaded anyway if any of it is needed at some point in the life cycle of this class.
            return MetadataManager.GetSingleFileMetadataMaps(ref phoneNumberMetadataAtomicRef,
                phoneNumberMetadataFileName, metadataLoader)[countryCallingCode];
        }
    }
}
