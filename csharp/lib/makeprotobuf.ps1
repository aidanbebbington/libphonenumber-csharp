# This script is used to generate Phonemetadata.cs and Phonenumber.cs
# from their protocol buffers definitions.
# Pythonscript depends on Python v2.7

Invoke-WebRequest -Uri https://www.nuget.org/api/v2/package/Google.ProtocolBuffers/2.4.1.555 -OutFile google.protocolbuffers.2.4.1.555.nupkg
7z x google.protocolbuffers.2.4.1.555.nupkg
.\tools\ProtoGen.exe -namespace=PhoneNumbers -cls_compliance=false --proto_path=..\..\resources ..\..\resources\phonemetadata.proto ..\..\resources\phonenumber.proto
python cleanprotobuf.py PhoneMetadata.cs ..\PhoneNumbers\PhoneMetadata.cs
python cleanprotobuf.py PhoneNumber.cs ..\PhoneNumbers\PhoneNumber.cs
rm  PhoneMetadata.cs
rm PhoneNumber.cs