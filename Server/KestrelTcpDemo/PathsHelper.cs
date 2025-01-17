﻿//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Documents
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;

    internal static class PathsHelper
    {
        public static void ParseDatabaseNameAndCollectionNameFromUrlSegments(
            string[] segments,
            out string databaseName,
            out string collectionName)
        {
            databaseName = string.Empty;
            collectionName = string.Empty;

            if (segments == null || segments.Length < 2)
            {
                return;
            }

            if (string.Equals(segments[0], Paths.DatabasesPathSegment, StringComparison.OrdinalIgnoreCase))
            {
                databaseName = Uri.UnescapeDataString(UrlUtility.RemoveTrailingSlashes(UrlUtility.RemoveLeadingSlashes(segments[1])));
                if (segments.Length >= 4 && string.Equals(segments[2], Paths.CollectionsPathSegment, StringComparison.OrdinalIgnoreCase))
                {
                    collectionName = Uri.UnescapeDataString(UrlUtility.RemoveTrailingSlashes(UrlUtility.RemoveLeadingSlashes(segments[3])));
                }
            }
        }

        private static bool TryParseNameSegments(
            string resourceUrl,
            string[] segments,
            out bool isFeed,
            out string resourcePath,
            out string resourceFullName,
            out string databaseName,
            out string collectionName,
            bool parseDatabaseAndCollectionNames)
        {
            isFeed = false;
            resourcePath = string.Empty;
            resourceFullName = string.Empty;
            databaseName = string.Empty;
            collectionName = string.Empty;

            if (segments == null || segments.Length < 1)
            {
                return false;
            }

            if (segments.Length % 2 == 0)
            {
                // even number, assume it is individual resource.
                if (PathsHelper.IsResourceType(segments[segments.Length - 2]))
                {
                    resourcePath = segments[segments.Length - 2];
                    resourceFullName = resourceUrl;
                    resourceFullName = Uri.UnescapeDataString(UrlUtility.RemoveTrailingSlashes(UrlUtility.RemoveLeadingSlashes(resourceFullName)));
                    if (parseDatabaseAndCollectionNames)
                    {
                        PathsHelper.ParseDatabaseNameAndCollectionNameFromUrlSegments(segments, out databaseName, out collectionName);
                    }

                    return true;
                }
            }
            else
            {
                // odd number, assume it is feed request
                if (PathsHelper.IsResourceType(segments[segments.Length - 1]))
                {
                    isFeed = true;
                    resourcePath = segments[segments.Length - 1];
                    // remove the trailing resource type
                    resourceFullName = resourceUrl.Substring(0, UrlUtility.RemoveTrailingSlashes(resourceUrl).LastIndexOf(Paths.Root, StringComparison.CurrentCultureIgnoreCase));
                    resourceFullName = Uri.UnescapeDataString(UrlUtility.RemoveTrailingSlashes(UrlUtility.RemoveLeadingSlashes(resourceFullName)));
                    if (parseDatabaseAndCollectionNames)
                    {
                        PathsHelper.ParseDatabaseNameAndCollectionNameFromUrlSegments(segments, out databaseName, out collectionName);
                    }

                    return true;
                }
            }

            return false;
        }

//        public static ResourceType GetResourcePathSegment(string resourcePathSegment)
//        {
//            if (string.IsNullOrEmpty(resourcePathSegment))
//            {
//                string message = string.Format(CultureInfo.CurrentUICulture, RMResources.StringArgumentNullOrEmpty, "resourcePathSegment");
//                Debug.Assert(false, message);
//                throw new BadRequestException(message);
//            }

//            switch (resourcePathSegment.ToLowerInvariant())
//            {
//                case Paths.AttachmentsPathSegment:
//                    return ResourceType.Attachment;

//                case Paths.CollectionsPathSegment:
//                    return ResourceType.Collection;

//                case Paths.DatabasesPathSegment:
//                    return ResourceType.Database;

//                case Paths.PermissionsPathSegment:
//                    return ResourceType.Permission;

//                case Paths.UsersPathSegment:
//                    return ResourceType.User;

//                case Paths.ClientEncryptionKeysPathSegment:
//                    return ResourceType.ClientEncryptionKey;

//                case Paths.UserDefinedTypesPathSegment:
//                    return ResourceType.UserDefinedType;

//                case Paths.DocumentsPathSegment:
//                    return ResourceType.Document;

//                case Paths.StoredProceduresPathSegment:
//                    return ResourceType.StoredProcedure;

//                case Paths.UserDefinedFunctionsPathSegment:
//                    return ResourceType.UserDefinedFunction;

//                case Paths.TriggersPathSegment:
//                    return ResourceType.Trigger;

//                case Paths.ConflictsPathSegment:
//                    return ResourceType.Conflict;

//                case Paths.OffersPathSegment:
//                    return ResourceType.Offer;

//                case Paths.SchemasPathSegment:
//                    return ResourceType.Schema;

//                case Paths.PartitionKeyRangesPathSegment:
//                    return ResourceType.PartitionKeyRange;

//                case Paths.MediaPathSegment:
//                    return ResourceType.Media;

//                case Paths.AddressPathSegment:
//                    return ResourceType.Address;

//                case Paths.SnapshotsPathSegment:
//                    return ResourceType.Snapshot;

//                case Paths.RoleDefinitionsPathSegment:
//                    return ResourceType.RoleDefinition;

//                case Paths.RoleAssignmentsPathSegment:
//                    return ResourceType.RoleAssignment;
//            }

//            string errorMessage = string.Format(CultureInfo.CurrentUICulture, RMResources.UnknownResourceType, resourcePathSegment);
//            Debug.Assert(false, errorMessage);
//            throw new BadRequestException(errorMessage);
//        }

//        public static string GetResourcePath(ResourceType resourceType)
//        {
//            switch (resourceType)
//            {
//                case ResourceType.Database:
//                    return Paths.DatabasesPathSegment;

//                case ResourceType.Collection:
//                case ResourceType.PartitionKey:
//                    return Paths.CollectionsPathSegment;

//                case ResourceType.Document:
//                    return Paths.DocumentsPathSegment;

//                case ResourceType.StoredProcedure:
//                    return Paths.StoredProceduresPathSegment;

//                case ResourceType.UserDefinedFunction:
//                    return Paths.UserDefinedFunctionsPathSegment;

//                case ResourceType.Trigger:
//                    return Paths.TriggersPathSegment;

//                case ResourceType.Conflict:
//                    return Paths.ConflictsPathSegment;

//                case ResourceType.Attachment:
//                    return Paths.AttachmentsPathSegment;

//                case ResourceType.User:
//                    return Paths.UsersPathSegment;

//                case ResourceType.ClientEncryptionKey:
//                    return Paths.ClientEncryptionKeysPathSegment;

//                case ResourceType.UserDefinedType:
//                    return Paths.UserDefinedTypesPathSegment;

//                case ResourceType.Permission:
//                    return Paths.PermissionsPathSegment;

//                case ResourceType.Offer:
//                    return Paths.OffersPathSegment;

//                case ResourceType.PartitionKeyRange:
//                    return Paths.PartitionKeyRangesPathSegment;

//                case ResourceType.Media:
//                    return Paths.Medias_Root;

//                case ResourceType.Schema:
//                    return Paths.SchemasPathSegment;

//                case ResourceType.Snapshot:
//                    return Paths.SnapshotsPathSegment;
                
//                case ResourceType.PartitionedSystemDocument:
//                    return Paths.PartitionedSystemDocumentsPathSegment;
                
//                case ResourceType.RoleDefinition:
//                    return Paths.RoleDefinitionsPathSegment;

//                case ResourceType.RoleAssignment:
//                    return Paths.RoleAssignmentsPathSegment;

//#if !COSMOSCLIENT
//                case ResourceType.MasterPartition:
//                case ResourceType.ServerPartition:
//                    return Paths.PartitionsPathSegment;

//                case ResourceType.RidRange:
//                    return Paths.RidRangePathSegment;

//                case ResourceType.VectorClock:
//                    return Paths.VectorClockPathSegment;

//                case ResourceType.PartitionSetInformation:
//                case ResourceType.XPReplicatorAddress:
//                case ResourceType.Topology:
//                case ResourceType.Replica:
//                case ResourceType.ServiceFabricService:
//                case ResourceType.RestoreMetadata:
//                case ResourceType.Module:
//                case ResourceType.ModuleCommand:
//#endif
//                case ResourceType.DatabaseAccount:
//                case ResourceType.Address:
//                case ResourceType.Record:
//                case ResourceType.BatchApply:
//                case ResourceType.ControllerService:
//                    return Paths.Root;

//                default:
//                    string errorMessage = string.Format(CultureInfo.CurrentUICulture, RMResources.UnknownResourceType, resourceType.ToString());
//                    Debug.Assert(false, errorMessage);
//                    throw new BadRequestException(errorMessage);
//            }
//        }

//        public static string GeneratePath(ResourceType resourceType, DocumentServiceRequest request, bool isFeed, bool notRequireValidation = false)
//        {
//            if (request.IsNameBased)
//            {
//                return PathsHelper.GeneratePathForNameBased(resourceType, request.ResourceAddress, isFeed, request.OperationType, notRequireValidation);
//            }
//            else
//            {
//                return PathsHelper.GeneratePath(resourceType, request.ResourceId, isFeed, request.OperationType);
//            }
//        }

        public static string GenerateUserDefinedTypePath(string databaseName, string typeName)
        {
            return Paths.DatabasesPathSegment + "/" + databaseName + "/" + Paths.UserDefinedTypesPathSegment + "/" + typeName;
        }

        public static string GetCollectionPath(string resourceFullName)
        {
            if (resourceFullName != null)
            {
                int index = resourceFullName.Length > 0 && resourceFullName[0] == '/' ? resourceFullName.IndexOfNth('/', 5) : resourceFullName.IndexOfNth('/', 4);
                if (index > 0)
                    return resourceFullName.Substring(0, index);
            }
            return resourceFullName;
        }

        public static string GetDatabasePath(string resourceFullName)
        {
            if (resourceFullName != null)
            {
                int index = resourceFullName.Length > 0 && resourceFullName[0] == '/' ? resourceFullName.IndexOfNth('/', 3) : resourceFullName.IndexOfNth('/', 2);
                if (index > 0)
                    return resourceFullName.Substring(0, index);
            }
            return resourceFullName;
        }

        public static string GetParentByIndex(string resourceFullName, int segmentIndex)
        {
            int index = resourceFullName.IndexOfNth('/', segmentIndex);
            if (index > 0)
                return resourceFullName.Substring(0, index);
            else
            {
                index = resourceFullName.IndexOfNth('/', segmentIndex - 1);
                if (index > 0)
                    return resourceFullName;
                else
                    return null;
            }
        }

        // for testing to set to verify server side validation
        private static bool isClientSideValidationEnabled = true;

        internal static void SetClientSidevalidation(bool validation)
        {
            isClientSideValidationEnabled = validation;
        }

//        private static string GeneratePathForNameBased(ResourceType resourceType, string resourceFullName, bool isFeed, OperationType operationType, bool notRequireValidation = false)
//        {
//            if (isFeed &&
//                string.IsNullOrEmpty(resourceFullName) &&
//                resourceType != ResourceType.Database &&
//                resourceType != ResourceType.Snapshot &&
//                resourceType != ResourceType.RoleDefinition &&
//                resourceType != ResourceType.RoleAssignment)
//            {
//                string errorMessage = string.Format(CultureInfo.InvariantCulture, RMResources.UnexpectedResourceType, resourceType);
//                throw new BadRequestException(errorMessage);
//            }

//            string resourcePath = null;
//            ResourceType resourceTypeToValidate;
//            // Validate resourceFullName comply with the intended resource type.

//            if (resourceType == ResourceType.PartitionKey && operationType == OperationType.Delete)
//            {
//                resourceTypeToValidate = resourceType;
//                resourceFullName = resourceFullName + "/" + Paths.OperationsPathSegment + "/" + Paths.PartitionKeyDeletePathSegment;
//                resourcePath = resourceFullName;
//            }
//            else if (!isFeed)
//            {
//                resourceTypeToValidate = resourceType;
//                resourcePath = resourceFullName;
//            }
//            else if (resourceType == ResourceType.Database)
//            {
//                return Paths.DatabasesPathSegment;
//            }
//            else if (resourceType == ResourceType.Collection)
//            {
//                resourceTypeToValidate = ResourceType.Database;
//                resourcePath = resourceFullName + "/" + Paths.CollectionsPathSegment;
//            }
//            else if (resourceType == ResourceType.ClientEncryptionKey)
//            {
//                resourceTypeToValidate = ResourceType.Database;
//                resourcePath = resourceFullName + "/" + Paths.ClientEncryptionKeysPathSegment;
//            }
//            else if (resourceType == ResourceType.StoredProcedure)
//            {
//                resourceTypeToValidate = ResourceType.Collection;
//                resourcePath = resourceFullName + "/" + Paths.StoredProceduresPathSegment;
//            }
//            else if (resourceType == ResourceType.UserDefinedFunction)
//            {
//                resourceTypeToValidate = ResourceType.Collection;
//                resourcePath = resourceFullName + "/" + Paths.UserDefinedFunctionsPathSegment;
//            }
//            else if (resourceType == ResourceType.Trigger)
//            {
//                resourceTypeToValidate = ResourceType.Collection;
//                resourcePath = resourceFullName + "/" + Paths.TriggersPathSegment;
//            }
//            else if (resourceType == ResourceType.Conflict)
//            {
//                resourceTypeToValidate = ResourceType.Collection;
//                resourcePath = resourceFullName + "/" + Paths.ConflictsPathSegment;
//            }
//            else if (resourceType == ResourceType.Attachment)
//            {
//                resourceTypeToValidate = ResourceType.Document;
//                resourcePath = resourceFullName + "/" + Paths.AttachmentsPathSegment;
//            }
//            else if (resourceType == ResourceType.User)
//            {
//                resourceTypeToValidate = ResourceType.Database;
//                resourcePath = resourceFullName + "/" + Paths.UsersPathSegment;
//            }
//            else if (resourceType == ResourceType.UserDefinedType)
//            {
//                resourceTypeToValidate = ResourceType.Database;
//                resourcePath = resourceFullName + "/" + Paths.UserDefinedTypesPathSegment;
//            }
//            else if (resourceType == ResourceType.Permission)
//            {
//                resourceTypeToValidate = ResourceType.User;
//                resourcePath = resourceFullName + "/" + Paths.PermissionsPathSegment;
//            }
//            else if (resourceType == ResourceType.Document)
//            {
//                resourceTypeToValidate = ResourceType.Collection;
//                resourcePath = resourceFullName + "/" + Paths.DocumentsPathSegment;
//            }
//            else if (resourceType == ResourceType.Offer)
//            {
//                return resourceFullName + "/" + Paths.OffersPathSegment;
//            }
//            else if (resourceType == ResourceType.PartitionKeyRange)
//            {
//                return resourceFullName + "/" + Paths.PartitionKeyRangesPathSegment;
//            }
//            else if (resourceType == ResourceType.Schema)
//            {
//                resourceTypeToValidate = ResourceType.Collection;
//                resourcePath = resourceFullName + "/" + Paths.SchemasPathSegment;
//            }
//            else if (resourceType == ResourceType.PartitionedSystemDocument)
//            {
//                resourceTypeToValidate = ResourceType.Collection;
//                resourcePath = resourceFullName + "/" + Paths.PartitionedSystemDocumentsPathSegment;
//            }
//            else if (resourceType == ResourceType.Snapshot)
//            {
//                return Paths.SnapshotsPathSegment;
//            }
//            else if (resourceType == ResourceType.RoleDefinition)
//            {
//                return Paths.RoleDefinitionsPathSegment;
//            }
//            else if (resourceType == ResourceType.RoleAssignment)
//            {
//                return Paths.RoleAssignmentsPathSegment;
//            }
//            else
//            {
//                string errorMessage = string.Format(CultureInfo.CurrentUICulture, RMResources.UnknownResourceType, resourceType.ToString());
//                Debug.Assert(false, errorMessage);
//                throw new BadRequestException(errorMessage);
//            }

//            if (!notRequireValidation && isClientSideValidationEnabled)
//            {
//                if (!ValidateResourceFullName(resourceTypeToValidate, resourceFullName))
//                {
//                    string errorMessage = string.Format(CultureInfo.InvariantCulture, RMResources.UnexpectedResourceType, resourceType);
//                    throw new BadRequestException(errorMessage);
//                }
//            }
//            return resourcePath;
//        }


//        public static string GeneratePath(ResourceType resourceType, string ownerOrResourceId, bool isFeed, OperationType operationType = default)
//        {
//            if (isFeed && string.IsNullOrEmpty(ownerOrResourceId) &&
//                resourceType != ResourceType.Database &&
//                resourceType != ResourceType.Offer &&
//                resourceType != ResourceType.DatabaseAccount &&
//                resourceType != ResourceType.Snapshot &&
//                resourceType != ResourceType.RoleAssignment &&
//                resourceType != ResourceType.RoleDefinition
//#if !COSMOSCLIENT
//                && resourceType != ResourceType.MasterPartition &&
//                resourceType != ResourceType.ServerPartition &&
//                resourceType != ResourceType.Topology &&
//                resourceType != ResourceType.RidRange &&
//                resourceType != ResourceType.VectorClock
//#endif
//                )
//            {
//                throw new BadRequestException(string.Format(CultureInfo.InvariantCulture, RMResources.UnexpectedResourceType, resourceType));
//            }

//            if (isFeed && resourceType == ResourceType.Database)
//            {
//                return Paths.DatabasesPathSegment;
//            }
//            else if (resourceType == ResourceType.Database)
//            {
//                return Paths.DatabasesPathSegment + "/" + ownerOrResourceId.ToString();
//            }
//            else if (isFeed && resourceType == ResourceType.Collection)
//            {
//                ResourceId documentCollectionId = ResourceId.Parse(ownerOrResourceId);

//                return Paths.DatabasesPathSegment + "/" + documentCollectionId.DatabaseId.ToString() + "/" +
//                    Paths.CollectionsPathSegment;
//            }
//            else if (resourceType == ResourceType.Collection)
//            {
//                ResourceId documentCollectionId = ResourceId.Parse(ownerOrResourceId);

//                return Paths.DatabasesPathSegment + "/" + documentCollectionId.DatabaseId.ToString() + "/" +
//                    Paths.CollectionsPathSegment + "/" + documentCollectionId.DocumentCollectionId.ToString();
//            }
//            else if (isFeed && resourceType == ResourceType.Offer)
//            {
//                return Paths.OffersPathSegment;
//            }
//            else if (resourceType == ResourceType.Offer)
//            {
//                return Paths.OffersPathSegment + "/" + ownerOrResourceId.ToString();
//            }
//            else if (isFeed && resourceType == ResourceType.StoredProcedure)
//            {
//                ResourceId documentCollectionId = ResourceId.Parse(ownerOrResourceId);

//                return
//                    Paths.DatabasesPathSegment + "/" + documentCollectionId.DatabaseId.ToString() + "/" +
//                    Paths.CollectionsPathSegment + "/" + documentCollectionId.DocumentCollectionId.ToString() + "/" +
//                    Paths.StoredProceduresPathSegment;
//            }
//            else if (resourceType == ResourceType.StoredProcedure)
//            {
//                ResourceId storedProcedureId = ResourceId.Parse(ownerOrResourceId);

//                return Paths.DatabasesPathSegment + "/" + storedProcedureId.DatabaseId.ToString() + "/" +
//                    Paths.CollectionsPathSegment + "/" + storedProcedureId.DocumentCollectionId.ToString() + "/" +
//                    Paths.StoredProceduresPathSegment + "/" + storedProcedureId.StoredProcedureId.ToString();
//            }
//            else if (isFeed && resourceType == ResourceType.UserDefinedFunction)
//            {
//                ResourceId documentCollectionId = ResourceId.Parse(ownerOrResourceId);

//                return
//                    Paths.DatabasesPathSegment + "/" + documentCollectionId.DatabaseId.ToString() + "/" +
//                    Paths.CollectionsPathSegment + "/" + documentCollectionId.DocumentCollectionId.ToString() + "/" +
//                    Paths.UserDefinedFunctionsPathSegment;
//            }
//            else if (resourceType == ResourceType.UserDefinedFunction)
//            {
//                ResourceId functionId = ResourceId.Parse(ownerOrResourceId);

//                return Paths.DatabasesPathSegment + "/" + functionId.DatabaseId.ToString() + "/" +
//                    Paths.CollectionsPathSegment + "/" + functionId.DocumentCollectionId.ToString() + "/" +
//                    Paths.UserDefinedFunctionsPathSegment + "/" + functionId.UserDefinedFunctionId.ToString();
//            }
//            else if (isFeed && resourceType == ResourceType.Trigger)
//            {
//                ResourceId documentCollectionId = ResourceId.Parse(ownerOrResourceId);

//                return
//                    Paths.DatabasesPathSegment + "/" + documentCollectionId.DatabaseId.ToString() + "/" +
//                    Paths.CollectionsPathSegment + "/" + documentCollectionId.DocumentCollectionId.ToString() + "/" +
//                    Paths.TriggersPathSegment;
//            }
//            else if (resourceType == ResourceType.Trigger)
//            {
//                ResourceId triggerId = ResourceId.Parse(ownerOrResourceId);

//                return Paths.DatabasesPathSegment + "/" + triggerId.DatabaseId.ToString() + "/" +
//                    Paths.CollectionsPathSegment + "/" + triggerId.DocumentCollectionId.ToString() + "/" +
//                    Paths.TriggersPathSegment + "/" + triggerId.TriggerId.ToString();
//            }
//            else if (isFeed && resourceType == ResourceType.Conflict)
//            {
//                ResourceId documentCollectionId = ResourceId.Parse(ownerOrResourceId);

//                return
//                    Paths.DatabasesPathSegment + "/" + documentCollectionId.DatabaseId.ToString() + "/" +
//                    Paths.CollectionsPathSegment + "/" + documentCollectionId.DocumentCollectionId.ToString() + "/" +
//                    Paths.ConflictsPathSegment;
//            }
//            else if (resourceType == ResourceType.Conflict)
//            {
//                ResourceId conflictId = ResourceId.Parse(ownerOrResourceId);

//                return Paths.DatabasesPathSegment + "/" + conflictId.DatabaseId.ToString() + "/" +
//                    Paths.CollectionsPathSegment + "/" + conflictId.DocumentCollectionId.ToString() + "/" +
//                    Paths.ConflictsPathSegment + "/" + conflictId.ConflictId.ToString();
//            }
//            else if (isFeed && resourceType == ResourceType.PartitionKeyRange)
//            {
//                ResourceId documentCollectionId = ResourceId.Parse(ownerOrResourceId);

//                return
//                    Paths.DatabasesPathSegment + "/" + documentCollectionId.DatabaseId.ToString() + "/" +
//                    Paths.CollectionsPathSegment + "/" + documentCollectionId.DocumentCollectionId.ToString() + "/" +
//                    Paths.PartitionKeyRangesPathSegment;
//            }
//            else if (resourceType == ResourceType.PartitionKeyRange)
//            {
//                ResourceId partitionKeyRangeId = ResourceId.Parse(ownerOrResourceId);

//                return Paths.DatabasesPathSegment + "/" + partitionKeyRangeId.DatabaseId.ToString() + "/" +
//                    Paths.CollectionsPathSegment + "/" + partitionKeyRangeId.DocumentCollectionId.ToString() + "/" +
//                    Paths.PartitionKeyRangesPathSegment + "/" + partitionKeyRangeId.PartitionKeyRangeId.ToString();
//            }
//            else if (isFeed && resourceType == ResourceType.Attachment)
//            {
//                ResourceId documentCollectionId = ResourceId.Parse(ownerOrResourceId);

//                return
//                    Paths.DatabasesPathSegment + "/" + documentCollectionId.DatabaseId.ToString() + "/" +
//                    Paths.CollectionsPathSegment + "/" + documentCollectionId.DocumentCollectionId.ToString() + "/" +
//                    Paths.DocumentsPathSegment + "/" + documentCollectionId.DocumentId.ToString() + "/" +
//                    Paths.AttachmentsPathSegment;
//            }
//            else if (resourceType == ResourceType.Attachment)
//            {
//                ResourceId attachmentId = ResourceId.Parse(ownerOrResourceId);

//                return Paths.DatabasesPathSegment + "/" + attachmentId.DatabaseId.ToString() + "/" +
//                    Paths.CollectionsPathSegment + "/" + attachmentId.DocumentCollectionId.ToString() + "/" +
//                    Paths.DocumentsPathSegment + "/" + attachmentId.DocumentId.ToString() + "/" +
//                    Paths.AttachmentsPathSegment + "/" + attachmentId.AttachmentId.ToString();
//            }
//            else if (isFeed && resourceType == ResourceType.User)
//            {
//                return
//                    Paths.DatabasesPathSegment + "/" + ownerOrResourceId + "/" +
//                    Paths.UsersPathSegment;
//            }
//            else if (resourceType == ResourceType.User)
//            {
//                ResourceId userId = ResourceId.Parse(ownerOrResourceId);

//                return Paths.DatabasesPathSegment + "/" + userId.DatabaseId.ToString() + "/" +
//                    Paths.UsersPathSegment + "/" + userId.UserId.ToString();
//            }
//            else if (isFeed && resourceType == ResourceType.ClientEncryptionKey)
//            {
//                return Paths.DatabasesPathSegment + "/" + ownerOrResourceId + "/" +
//                    Paths.ClientEncryptionKeysPathSegment;
//            }
//            else if (resourceType == ResourceType.ClientEncryptionKey)
//            {
//                ResourceId clientEncryptionKeyId = ResourceId.Parse(ownerOrResourceId);

//                return Paths.DatabasesPathSegment + "/" + clientEncryptionKeyId.DatabaseId.ToString() + "/" +
//                    Paths.ClientEncryptionKeysPathSegment + "/" + clientEncryptionKeyId.ClientEncryptionKeyId.ToString();
//            }
//            else if (isFeed && resourceType == ResourceType.UserDefinedType)
//            {
//                return
//                    Paths.DatabasesPathSegment + "/" + ownerOrResourceId + "/" +
//                    Paths.UserDefinedTypesPathSegment;
//            }
//            else if (resourceType == ResourceType.UserDefinedType)
//            {
//                ResourceId userDefinedTypeId = ResourceId.Parse(ownerOrResourceId);

//                return Paths.DatabasesPathSegment + "/" + userDefinedTypeId.DatabaseId.ToString() + "/" +
//                    Paths.UserDefinedTypesPathSegment + "/" + userDefinedTypeId.UserDefinedTypeId.ToString();
//            }
//            else if (isFeed && resourceType == ResourceType.Permission)
//            {
//                ResourceId userId = ResourceId.Parse(ownerOrResourceId);

//                return
//                    Paths.DatabasesPathSegment + "/" + userId.DatabaseId.ToString() + "/" +
//                    Paths.UsersPathSegment + "/" + userId.UserId.ToString() + "/" +
//                    Paths.PermissionsPathSegment;
//            }
//            else if (resourceType == ResourceType.Permission)
//            {
//                ResourceId permissionId = ResourceId.Parse(ownerOrResourceId);

//                return Paths.DatabasesPathSegment + "/" + permissionId.DatabaseId.ToString() + "/" +
//                    Paths.UsersPathSegment + "/" + permissionId.UserId.ToString() + "/" +
//                    Paths.PermissionsPathSegment + "/" + permissionId.PermissionId.ToString();
//            }
//            else if (isFeed && resourceType == ResourceType.Document)
//            {
//                ResourceId documentCollectionId = ResourceId.Parse(ownerOrResourceId);

//                return
//                    Paths.DatabasesPathSegment + "/" + documentCollectionId.DatabaseId.ToString() + "/" +
//                    Paths.CollectionsPathSegment + "/" + documentCollectionId.DocumentCollectionId.ToString() + "/" +
//                    Paths.DocumentsPathSegment;
//            }
//            else if (resourceType == ResourceType.Document)
//            {
//                ResourceId documentId = ResourceId.Parse(ownerOrResourceId);

//                return Paths.DatabasesPathSegment + "/" + documentId.DatabaseId.ToString() + "/" +
//                    Paths.CollectionsPathSegment + "/" + documentId.DocumentCollectionId.ToString() + "/" +
//                    Paths.DocumentsPathSegment + "/" + documentId.DocumentId.ToString();
//            }
//            else if (isFeed && resourceType == ResourceType.Schema)
//            {
//                ResourceId schemaCollectionId = ResourceId.Parse(ownerOrResourceId);

//                return
//                    Paths.DatabasesPathSegment + "/" + schemaCollectionId.DatabaseId.ToString() + "/" +
//                    Paths.CollectionsPathSegment + "/" + schemaCollectionId.DocumentCollectionId.ToString() + "/" +
//                    Paths.SchemasPathSegment;
//            }
//            else if (resourceType == ResourceType.Schema)
//            {
//                ResourceId schemaId = ResourceId.Parse(ownerOrResourceId);

//                return Paths.DatabasesPathSegment + "/" + schemaId.DatabaseId.ToString() + "/" +
//                    Paths.CollectionsPathSegment + "/" + schemaId.DocumentCollectionId.ToString() + "/" +
//                    Paths.SchemasPathSegment + "/" + schemaId.SchemaId.ToString();
//            }
//            else if (isFeed && resourceType == ResourceType.DatabaseAccount)
//            {
//                return Paths.DatabaseAccountSegment;
//            }
//            else if (resourceType == ResourceType.DatabaseAccount)
//            {
//                return Paths.DatabaseAccountSegment + "/" + ownerOrResourceId;
//            }
//            else if (isFeed && resourceType == ResourceType.Snapshot)
//            {
//                return Paths.SnapshotsPathSegment;
//            }
//            else if (resourceType == ResourceType.Snapshot)
//            {
//                return Paths.SnapshotsPathSegment + "/" + ownerOrResourceId.ToString();
//            }
//            else if(resourceType == ResourceType.PartitionKey && operationType == OperationType.Delete)
//            {
//                ResourceId documentCollectionId = ResourceId.Parse(ownerOrResourceId);

//                return Paths.DatabasesPathSegment + "/" + documentCollectionId.DatabaseId.ToString() + "/" +
//                    Paths.CollectionsPathSegment + "/" + documentCollectionId.DocumentCollectionId.ToString() + "/" + Paths.OperationsPathSegment + "/" + Paths.PartitionKeyDeletePathSegment;
//            }
//            else if (isFeed && resourceType == ResourceType.RoleAssignment)
//            {
//                return Paths.RoleAssignmentsPathSegment;
//            }
//            else if (isFeed && resourceType == ResourceType.RoleDefinition)
//            {
//                return Paths.RoleDefinitionsPathSegment;
//            }
//            else if (resourceType == ResourceType.RoleAssignment)
//            {
//                return Paths.RoleAssignmentsPathSegment + "/" + ownerOrResourceId.ToString();
//            }
//            else if (resourceType == ResourceType.RoleDefinition)
//            {
//                return Paths.RoleDefinitionsPathSegment + "/" + ownerOrResourceId.ToString();
//            }
//#if !COSMOSCLIENT
//            else if (isFeed && resourceType == ResourceType.MasterPartition)
//            {
//                return Paths.PartitionsPathSegment;
//            }
//            else if (resourceType == ResourceType.MasterPartition)
//            {
//                return Paths.PartitionsPathSegment + "/" + ownerOrResourceId;
//            }
//            else if (isFeed && resourceType == ResourceType.ServerPartition)
//            {
//                return Paths.PartitionsPathSegment;
//            }
//            else if (resourceType == ResourceType.ServerPartition)
//            {
//                return Paths.PartitionsPathSegment + "/" + ownerOrResourceId;
//            }
//            else if (isFeed && resourceType == ResourceType.Topology)
//            {
//                return Paths.TopologyPathSegment;
//            }
//            else if (resourceType == ResourceType.Topology)
//            {
//                return Paths.TopologyPathSegment + "/" + ownerOrResourceId;
//            }
//            else if (resourceType == ResourceType.RidRange)
//            {
//                return Paths.RidRangePathSegment + "/" + ownerOrResourceId;
//            }
//            else if (resourceType == ResourceType.VectorClock)
//            {
//                return Paths.VectorClockPathSegment + "/" + ownerOrResourceId;
//            }
//#endif

//            string errorMessage = string.Format(CultureInfo.CurrentUICulture, RMResources.UnknownResourceType, resourceType.ToString());
//            Debug.Assert(false, errorMessage);
//            throw new BadRequestException(errorMessage);
//        }

//        public static string GenerateRootOperationPath(OperationType operationType)
//        {
//            switch (operationType)
//            {
//#if !COSMOSCLIENT
//                case OperationType.Pause:
//                    return Paths.OperationsPathSegment + "/" + Paths.ReplicaOperations_Pause;
//                case OperationType.Recycle:
//                    return Paths.OperationsPathSegment + "/" + Paths.ReplicaOperations_Recycle;
//                case OperationType.Resume:
//                    return Paths.OperationsPathSegment + "/" + Paths.ReplicaOperations_Resume;
//                case OperationType.Stop:
//                    return Paths.OperationsPathSegment + "/" + Paths.ReplicaOperations_Stop;
//                case OperationType.Crash:
//                    return Paths.OperationsPathSegment + "/" + Paths.ReplicaOperations_Crash;
//                case OperationType.ForceConfigRefresh:
//                    return Paths.OperationsPathSegment + "/" + Paths.ReplicaOperations_ForceConfigRefresh;
//                case OperationType.ReportThroughputUtilization:
//                    return Paths.OperationsPathSegment + "/" + Paths.ReplicaOperations_ReportThroughputUtilization;
//                case OperationType.BatchReportThroughputUtilization:
//                    return Paths.OperationsPathSegment + "/" + Paths.ReplicaOperations_BatchReportThroughputUtilization;
//                case OperationType.ControllerBatchGetOutput:
//                    return Paths.OperationsPathSegment + "/" + Paths.ControllerOperations_BatchGetOutput;
//                case OperationType.ControllerBatchReportCharges:
//                    return Paths.OperationsPathSegment + "/" + Paths.ControllerOperations_BatchReportCharges;
//                case OperationType.GetConfiguration:
//                    return Paths.OperationsPathSegment + "/" + Paths.Operations_GetConfiguration;
//                case OperationType.GetFederationConfigurations:
//                    return Paths.OperationsPathSegment + "/" + Paths.Operations_GetFederationConfigurations;
//                case OperationType.GetDatabaseAccountConfigurations:
//                    return Paths.OperationsPathSegment + "/" + Paths.Operations_GetDatabaseAccountConfigurations;
//                case OperationType.GetStorageAccountKey:
//                    return Paths.OperationsPathSegment + "/" + Paths.Operations_GetStorageAccountKey;
//                case OperationType.GetUnwrappedDek:
//                    return Paths.OperationsPathSegment + "/" + Paths.Operations_GetUnwrappedDek;
//                case OperationType.ReadReplicaFromMasterPartition:
//                    return Paths.OperationsPathSegment + "/" + Paths.Operations_ReadReplicaFromMasterPartition;
//                case OperationType.ReadReplicaFromServerPartition:
//                    return Paths.OperationsPathSegment + "/" + Paths.Operations_ReadReplicaFromServerPartition;
//#endif

//                default:
//                    Debug.Assert(false, "Unsupported operation type for replica");
//                    throw new NotFoundException();
//            }
//        }

        private static bool IsResourceType(string resourcePathSegment)
        {
            if (string.IsNullOrEmpty(resourcePathSegment))
            {
                return false;
            }

            switch (resourcePathSegment.ToLowerInvariant())
            {
                case Paths.AttachmentsPathSegment:
                case Paths.CollectionsPathSegment:
                case Paths.DatabasesPathSegment:
                case Paths.PermissionsPathSegment:
                case Paths.UsersPathSegment:
                case Paths.ClientEncryptionKeysPathSegment:
                case Paths.UserDefinedTypesPathSegment:
                case Paths.DocumentsPathSegment:
                case Paths.StoredProceduresPathSegment:
                case Paths.TriggersPathSegment:
                case Paths.UserDefinedFunctionsPathSegment:
                case Paths.ConflictsPathSegment:
                case Paths.MediaPathSegment:
                case Paths.OffersPathSegment:
                case Paths.PartitionsPathSegment:
                case Paths.DatabaseAccountSegment:
                case Paths.TopologyPathSegment:
                case Paths.PartitionKeyRangesPathSegment:
                case Paths.PartitionKeyRangePreSplitSegment:
                case Paths.PartitionKeyRangePostSplitSegment:
                case Paths.SchemasPathSegment:
                case Paths.RidRangePathSegment:
                case Paths.VectorClockPathSegment:
                case Paths.AddressPathSegment:
                case Paths.SnapshotsPathSegment:
                case Paths.PartitionedSystemDocumentsPathSegment:
                case Paths.RoleDefinitionsPathSegment:
                case Paths.RoleAssignmentsPathSegment:
                    return true;

                default:
                    return false;
            }
        }

        private static bool IsRootOperation(string operationSegment, string operationTypeSegment)
        {
            if (string.IsNullOrEmpty(operationSegment))
            {
                return false;
            }

            if (string.IsNullOrEmpty(operationTypeSegment))
            {
                return false;
            }

            if (string.Compare(operationSegment, Paths.OperationsPathSegment, StringComparison.OrdinalIgnoreCase) != 0)
            {
                return false;
            }

            switch (operationTypeSegment.ToLowerInvariant())
            {
                case Paths.ReplicaOperations_Pause:
                case Paths.ReplicaOperations_Resume:
                case Paths.ReplicaOperations_Stop:
                case Paths.ReplicaOperations_Recycle:
                case Paths.ReplicaOperations_Crash:
                case Paths.ReplicaOperations_ReportThroughputUtilization:
                case Paths.ReplicaOperations_BatchReportThroughputUtilization:
                case Paths.ControllerOperations_BatchGetOutput:
                case Paths.ControllerOperations_BatchReportCharges:
                case Paths.Operations_GetFederationConfigurations:
                case Paths.Operations_GetConfiguration:
                case Paths.Operations_GetStorageAccountKey:
                case Paths.Operations_GetDatabaseAccountConfigurations:
                case Paths.Operations_GetUnwrappedDek:
                case Paths.Operations_ReadReplicaFromMasterPartition:
                case Paths.Operations_ReadReplicaFromServerPartition:
                    return true;

                default:
                    return false;
            }
        }

        private static bool IsTopLevelOperationOperation(string replicaSegment, string addressSegment)
        {
            if (string.IsNullOrEmpty(replicaSegment) && // replica part should be empty
                (string.Compare(addressSegment, Paths.XPReplicatorAddressPathSegment, StringComparison.OrdinalIgnoreCase) == 0 ||
                 string.Compare(addressSegment, Paths.ComputeGatewayChargePathSegment, StringComparison.OrdinalIgnoreCase) == 0 ||
                 string.Compare(addressSegment, Paths.ServiceReservationPathSegment, StringComparison.OrdinalIgnoreCase) == 0))
            {
                return true;
            }

            return false;
        }

        internal static bool IsNameBased(string resourceIdOrFullName)
        {
            // quick way to tell whether it is resourceId nor not, non conclusively.
            if (!string.IsNullOrEmpty(resourceIdOrFullName) && resourceIdOrFullName.Length > 4 && resourceIdOrFullName[3] == '/')
            {
                return true;
            }
            return false;
        }

        internal static int IndexOfNth(this string str, char value, int n)
        {
            if (string.IsNullOrEmpty(str) || n <= 0 || n > str.Length)
            {
                return -1;
            }

            int remaining = n;
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == value)
                {
                    if (--remaining == 0)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }
    }
}
