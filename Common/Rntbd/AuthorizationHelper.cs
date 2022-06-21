//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------
namespace Microsoft.Azure.Cosmos
{
    using System;
    using System.Buffers;
    using System.Buffers.Text;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;
    using Microsoft.Azure.Cosmos.Core.Trace;
    using Microsoft.Azure.Documents;

    // This class is used by both client (for generating the auth header with master/system key) and 
    // by the G/W when verifying the auth header. Some additional logic is also used by management service.
    internal static class AuthorizationHelper
    {
        public const int MaxAuthorizationHeaderSize = 1024;
        public const int DefaultAllowedClockSkewInSeconds = 900;
        public const int DefaultMasterTokenExpiryInSeconds = 900;
        private const int MaxAadAuthorizationHeaderSize = 16 * 1024;
        private const int MaxResourceTokenAuthorizationHeaderSize = 8 * 1024;
        private static readonly string AuthorizationFormatPrefixUrlEncoded = HttpUtility.UrlEncode(string.Format(CultureInfo.InvariantCulture, Constants.Properties.AuthorizationFormat,
                Constants.Properties.MasterToken,
                Constants.Properties.TokenVersion,
                string.Empty));

        private static readonly Encoding AuthorizationEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        public static void ValidateInputRequestTime<T>(
            T requestHeaders,
            Func<T, string, string> headerGetter,
            int masterTokenExpiryInSeconds,
            int allowedClockSkewInSeconds)
        {
            if (requestHeaders == null)
            {
                DefaultTrace.TraceError("Null request headers for validating auth time");
                throw new Exception(RMResources.MissingDateForAuthorization);
            }

            // Fetch the date in the headers to compare against the correct time.
            // Since Date header is overridden by some proxies/http client libraries, we support
            // an additional date header 'x-ms-date' and prefer that to the regular 'date' header.
            string dateToCompare = headerGetter(requestHeaders, HttpConstants.HttpHeaders.XDate);
            if (string.IsNullOrEmpty(dateToCompare))
            {
                dateToCompare = headerGetter(requestHeaders, HttpConstants.HttpHeaders.HttpDate);
            }

            ValidateInputRequestTime(dateToCompare, masterTokenExpiryInSeconds, allowedClockSkewInSeconds);
        }

        public static void CheckTimeRangeIsCurrent(
            int allowedClockSkewInSeconds,
            DateTime startDateTime,
            DateTime expiryDateTime)
        {
            // Check if time ranges provided are beyond DateTime.MinValue or DateTime.MaxValue
            bool outOfRange = startDateTime <= DateTime.MinValue.AddSeconds(allowedClockSkewInSeconds)
                || expiryDateTime >= DateTime.MaxValue.AddSeconds(-allowedClockSkewInSeconds);

            // Adjust for a time lag between various instances upto 5 minutes i.e. allow [start-5, end+5]
            if (outOfRange ||
                startDateTime.AddSeconds(-allowedClockSkewInSeconds) > DateTime.UtcNow ||
                expiryDateTime.AddSeconds(allowedClockSkewInSeconds) < DateTime.UtcNow)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    RMResources.InvalidTokenTimeRange,
                    startDateTime.ToString("r", CultureInfo.InvariantCulture),
                    expiryDateTime.ToString("r", CultureInfo.InvariantCulture),
                    DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture));

                DefaultTrace.TraceError(message);

                throw new Exception(message);
            }
        }

        public static bool IsUserRequest(string resourceType)
        {
            if (string.Compare(resourceType, Paths.Root, StringComparison.OrdinalIgnoreCase) == 0
                || string.Compare(resourceType, Paths.PartitionKeyRangePreSplitSegment, StringComparison.OrdinalIgnoreCase) == 0
                || string.Compare(resourceType, Paths.PartitionKeyRangePostSplitSegment, StringComparison.OrdinalIgnoreCase) == 0
                || string.Compare(resourceType, Paths.ControllerOperations_BatchGetOutput, StringComparison.OrdinalIgnoreCase) == 0
                || string.Compare(resourceType, Paths.ControllerOperations_BatchReportCharges, StringComparison.OrdinalIgnoreCase) == 0
                || string.Compare(resourceType, Paths.Operations_GetStorageAccountKey, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return false;
            }

            return true;
        }

        public static AuthorizationTokenType GetSystemOperationType(bool readOnlyRequest, string resourceType)
        {
            if (!AuthorizationHelper.IsUserRequest(resourceType))
            {
                if (readOnlyRequest)
                {
                    return AuthorizationTokenType.SystemReadOnly;
                }
                else
                {
                    return AuthorizationTokenType.SystemAll;
                }
            }

            // operations on user resources
            if (readOnlyRequest)
            {
                return AuthorizationTokenType.SystemReadOnly;
            }
            else
            {
                return AuthorizationTokenType.SystemReadWrite;
            }

        }

        public static int SerializeMessagePayload(
               Span<byte> stream,
               string verb,
               string resourceId,
               string resourceType,
               string xDate)
        {
            // for name based, it is case sensitive, we won't use the lower case
            if (!PathsHelper.IsNameBased(resourceId))
            {
                resourceId = resourceId.ToLowerInvariant();
            }

            int totalLength = 0;
            int length = stream.Write(verb.ToLowerInvariant());
            totalLength += length;
            stream = stream.Slice(length);
            length = stream.Write("\n");
            totalLength += length;
            stream = stream.Slice(length);
            length = stream.Write(resourceType.ToLowerInvariant());
            totalLength += length;
            stream = stream.Slice(length);
            length = stream.Write("\n");
            totalLength += length;
            stream = stream.Slice(length);
            length = stream.Write(resourceId);
            totalLength += length;
            stream = stream.Slice(length);
            length = stream.Write("\n");
            totalLength += length;
            stream = stream.Slice(length);
            length = stream.Write(xDate.ToLowerInvariant());
            totalLength += length;
            stream = stream.Slice(length);
            length = stream.Write("\n");
            totalLength += length;
            stream = stream.Slice(length);
            length = stream.Write(string.Empty);
            totalLength += length;
            stream = stream.Slice(length);
            length = stream.Write("\n");
            totalLength += length;
            return totalLength;
        }

        public static bool IsResourceToken(string token)
        {
            int typeSeparatorPosition = token.IndexOf('&');
            if (typeSeparatorPosition == -1)
            {
                return false;
            }
            string authType = token.Substring(0, typeSeparatorPosition);

            int typeKeyValueSepartorPosition = authType.IndexOf('=');
            if (typeKeyValueSepartorPosition == -1 ||
                !authType.Substring(0, typeKeyValueSepartorPosition).Equals(Constants.Properties.AuthSchemaType, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            string authTypeValue = authType.Substring(typeKeyValueSepartorPosition + 1);

            return authTypeValue.Equals(Constants.Properties.ResourceToken, StringComparison.OrdinalIgnoreCase);
        }

        internal static string GetHeaderValue(IDictionary<string, string> headerValues, string key)
        {
            if (headerValues == null)
            {
                return string.Empty;
            }

            headerValues.TryGetValue(key, out string value);
            return value;
        }

        //internal static string GetAuthorizationResourceIdOrFullName(string resourceType, string resourceIdOrFullName)
        //{
        //    if (string.IsNullOrEmpty(resourceType) || string.IsNullOrEmpty(resourceIdOrFullName))
        //    {
        //        return resourceIdOrFullName;
        //    }

        //    if (PathsHelper.IsNameBased(resourceIdOrFullName))
        //    {
        //        // resource fullname is always end with name (not type segment like docs/colls).
        //        return resourceIdOrFullName;
        //    }

        //    if (resourceType.Equals(Paths.OffersPathSegment, StringComparison.OrdinalIgnoreCase) ||
        //        resourceType.Equals(Paths.PartitionsPathSegment, StringComparison.OrdinalIgnoreCase) ||
        //        resourceType.Equals(Paths.TopologyPathSegment, StringComparison.OrdinalIgnoreCase) ||
        //        resourceType.Equals(Paths.RidRangePathSegment, StringComparison.OrdinalIgnoreCase) ||
        //        resourceType.Equals(Paths.SnapshotsPathSegment, StringComparison.OrdinalIgnoreCase))
        //    {
        //        return resourceIdOrFullName;
        //    }

        //    ResourceId parsedRId = ResourceId.Parse(resourceIdOrFullName);
        //    if (resourceType.Equals(Paths.DatabasesPathSegment, StringComparison.OrdinalIgnoreCase))
        //    {
        //        return parsedRId.DatabaseId.ToString();
        //    }
        //    else if (resourceType.Equals(Paths.UsersPathSegment, StringComparison.OrdinalIgnoreCase))
        //    {
        //        return parsedRId.UserId.ToString();
        //    }
        //    else if (resourceType.Equals(Paths.UserDefinedTypesPathSegment, StringComparison.OrdinalIgnoreCase))
        //    {
        //        return parsedRId.UserDefinedTypeId.ToString();
        //    }
        //    else if (resourceType.Equals(Paths.CollectionsPathSegment, StringComparison.OrdinalIgnoreCase))
        //    {
        //        return parsedRId.DocumentCollectionId.ToString();
        //    }
        //    else if (resourceType.Equals(Paths.ClientEncryptionKeysPathSegment, StringComparison.OrdinalIgnoreCase))
        //    {
        //        return parsedRId.ClientEncryptionKeyId.ToString();
        //    }
        //    else if (resourceType.Equals(Paths.DocumentsPathSegment, StringComparison.OrdinalIgnoreCase))
        //    {
        //        return parsedRId.DocumentId.ToString();
        //    }
        //    else
        //    {
        //        // leaf node 
        //        return resourceIdOrFullName;
        //    }
        //}

        public static Uri GenerateUriFromAddressRequestUri(Uri uri)
        {
            // Address request has the URI fragment (dbs/dbid/colls/colId...) as part of
            // either $resolveFor 'or' $generate queries of the context.RequestUri.
            // Extracting out the URI in the form https://localhost/dbs/dbid/colls/colId/docs to generate the signature.
            // Authorizer uses the same URI to verify signature.
            string addressFeedUri = UrlUtility.ParseQuery(uri.Query)[HttpConstants.QueryStrings.Url]
                ?? UrlUtility.ParseQuery(uri.Query)[HttpConstants.QueryStrings.GenerateId]
                ?? UrlUtility.ParseQuery(uri.Query)[HttpConstants.QueryStrings.GetChildResourcePartitions];

            if (string.IsNullOrEmpty(addressFeedUri))
            {
                throw new Exception(RMResources.BadUrl);
            }

            return new Uri(uri.Scheme + "://" + uri.Host + "/" + HttpUtility.UrlDecode(addressFeedUri).Trim('/'));
        }

        private static void ValidateInputRequestTime(
            string dateToCompare,
            int masterTokenExpiryInSeconds,
            int allowedClockSkewInSeconds)
        {
            if (string.IsNullOrEmpty(dateToCompare))
            {
                throw new Exception(RMResources.MissingDateForAuthorization);
            }

            if (!DateTime.TryParse(dateToCompare, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal | DateTimeStyles.AllowWhiteSpaces, out DateTime utcStartTime))
            {
                throw new Exception(RMResources.InvalidDateHeader);
            }

            // Check if time range is beyond DateTime.MaxValue
            bool outOfRange = utcStartTime >= DateTime.MaxValue.AddSeconds(-masterTokenExpiryInSeconds);

            if (outOfRange)
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    RMResources.InvalidTokenTimeRange,
                    utcStartTime.ToString("r", CultureInfo.InvariantCulture),
                    DateTime.MaxValue.ToString("r", CultureInfo.InvariantCulture),
                    DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture));

                DefaultTrace.TraceError(message);

                throw new Exception(message);
            }

            DateTime utcEndTime = utcStartTime + TimeSpan.FromSeconds(masterTokenExpiryInSeconds);

            AuthorizationHelper.CheckTimeRangeIsCurrent(allowedClockSkewInSeconds, utcStartTime, utcEndTime);
        }

        /// <summary>
        /// This an optimized version of doing Convert.ToBase64String(hashPayLoad) with an optional wrapping HttpUtility.UrlEncode.
        /// This avoids the over head of converting it to a string and back to a byte[].
        /// </summary>
        private static unsafe string OptimizedConvertToBase64string(byte[] hashPayLoad, bool urlEncode)
        {
            // Create a large enough buffer that URL encode can use it.
            // Increase the buffer by 3x so it can be used for the URL encoding
            int capacity = Base64.GetMaxEncodedToUtf8Length(hashPayLoad.Length) * 3;
            byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(capacity);

            try
            {
                Span<byte> encodingBuffer = rentedBuffer;
                // This replaces the Convert.ToBase64String
                OperationStatus status = Base64.EncodeToUtf8(
                    hashPayLoad,
                    encodingBuffer,
                    out int _,
                    out int bytesWritten);

                if (status != OperationStatus.Done)
                {
                    throw new ArgumentException($"Authorization key payload is invalid. {status}");
                }

                return urlEncode 
                    ? AuthorizationHelper.UrlEncodeBase64SpanInPlace(encodingBuffer, bytesWritten)
                    : Encoding.UTF8.GetString(encodingBuffer.Slice(0, bytesWritten));
            }
            finally
            {
                if (rentedBuffer != null)
                {
                    ArrayPool<byte>.Shared.Return(rentedBuffer);
                }
            }
        }

        // This function is used by Compute
        internal static int ComputeMemoryCapacity(string verbInput, string authResourceId, string resourceTypeInput)
        {
            return
                verbInput.Length
                + AuthorizationHelper.AuthorizationEncoding.GetMaxByteCount(authResourceId.Length)
                + resourceTypeInput.Length
                + 5 // new line characters
                + 30; // date header length;
        }

        public static string GenerateKeyAuthorizationCore(
            string verb,
            string resourceId,
            string resourceType,
            string date,
            IComputeHash computeHash)
        {
            // resourceId can be null for feed-read of /dbs
            if (string.IsNullOrEmpty(verb))
            {
                throw new ArgumentException(RMResources.StringArgumentNullOrEmpty, nameof(verb));
            }

            if (resourceType == null)
            {
                throw new ArgumentNullException(nameof(resourceType)); // can be empty
            }

            {
                // Order of the values included in the message payload is a protocol that clients/BE need to follow exactly.
                // More headers can be added in the future.
                // If any of the value is optional, it should still have the placeholder value of ""
                // OperationType -> ResourceType -> ResourceId/OwnerId -> XDate -> Date
                string verbInput = verb ?? string.Empty;
                string resourceIdInput = resourceId ?? string.Empty;
                string resourceTypeInput = resourceType ?? string.Empty;

                // string authResourceId = AuthorizationHelper.GetAuthorizationResourceIdOrFullName(resourceTypeInput, resourceIdInput);
                // int memoryStreamCapacity = AuthorizationHelper.ComputeMemoryCapacity(verbInput, authResourceId, resourceTypeInput);
                int memoryStreamCapacity = AuthorizationHelper.ComputeMemoryCapacity(verbInput, resourceIdInput, resourceTypeInput);
                byte[] arrayPoolBuffer = ArrayPool<byte>.Shared.Rent(memoryStreamCapacity);

                try
                {
                    int length = AuthorizationHelper.SerializeMessagePayload(
                        arrayPoolBuffer,
                        verbInput,
                        resourceIdInput,
                        resourceTypeInput,
                        date);

                    byte[] hashPayLoad = computeHash.ComputeHash(new ArraySegment<byte>(arrayPoolBuffer, 0, length));
                    return Convert.ToBase64String(hashPayLoad);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(arrayPoolBuffer);
                }
            }
        }

        /// <summary>
        /// This does HttpUtility.UrlEncode functionality with Span buffer. It does an in place update to avoid
        /// creating the new buffer.
        /// </summary>
        /// <param name="base64Bytes">The buffer that include the bytes to url encode.</param>
        /// <param name="length">The length of bytes used in the buffer</param>
        /// <returns>The URLEncoded string of the bytes in the buffer</returns>
        public unsafe static string UrlEncodeBase64SpanInPlace(Span<byte> base64Bytes, int length)
        {
            if (base64Bytes == default)
            {
                throw new ArgumentNullException(nameof(base64Bytes));
            }

            if (base64Bytes.Length < length * 3)
            {
                throw new ArgumentException($"{nameof(base64Bytes)} should be 3x to avoid running out of space in worst case scenario where all characters are special");
            }

            if (length == 0)
            {
                return string.Empty;
            }

            int escapeBufferPosition = base64Bytes.Length - 1;
            for (int i = length - 1; i >= 0; i--)
            { 
                byte curr = base64Bytes[i];
                // Base64 is limited to Alphanumeric characters and '/' '=' '+'
                switch (curr)
                {
                    case (byte)'/':
                        base64Bytes[escapeBufferPosition--] = (byte)'f';
                        base64Bytes[escapeBufferPosition--] = (byte)'2';
                        base64Bytes[escapeBufferPosition--] = (byte)'%';
                        break;
                    case (byte)'=':
                        base64Bytes[escapeBufferPosition--] = (byte)'d';
                        base64Bytes[escapeBufferPosition--] = (byte)'3';
                        base64Bytes[escapeBufferPosition--] = (byte)'%';
                        break;
                    case (byte)'+':
                        base64Bytes[escapeBufferPosition--] = (byte)'b';
                        base64Bytes[escapeBufferPosition--] = (byte)'2';
                        base64Bytes[escapeBufferPosition--] = (byte)'%';
                        break;
                    default:
                        base64Bytes[escapeBufferPosition--] = curr;
                        break;
                }
            }

            Span<byte> endSlice = base64Bytes.Slice(escapeBufferPosition + 1);
            fixed (byte* bp = endSlice)
            {
                return Encoding.UTF8.GetString(bp, endSlice.Length);
            }
        }

        private static int Write(this Span<byte> stream, string contentToWrite)
        {
            int actualByteCount = AuthorizationHelper.AuthorizationEncoding.GetBytes(
                contentToWrite,
                stream);
            return actualByteCount;
        }

        public struct ArrayOwner : IDisposable
        {
            private readonly ArrayPool<byte> pool;

            public ArrayOwner(ArrayPool<byte> pool, ArraySegment<byte> buffer)
            {
                this.pool = pool;
                this.Buffer = buffer;
            }

            public ArraySegment<byte> Buffer { get; private set; }

            public void Dispose()
            {
                if (this.Buffer.Array != null)
                {
                    this.pool?.Return(this.Buffer.Array);
                    this.Buffer = default;
                }
            }
        }
    }
}
