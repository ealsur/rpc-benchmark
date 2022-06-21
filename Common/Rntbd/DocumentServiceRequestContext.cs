﻿//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Documents
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    internal sealed class DocumentServiceRequestContext
    {
        public TimeoutHelper TimeoutHelper { get; set; }

        public bool ForceRefreshAddressCache { get; set; }

        /// <summary>
        /// Cache the string representation of last returned store responses when exercising QuorumReader logic
        /// At the time of introducing this, this is purely for logging purposes and
        /// has not effect on correctness.
        /// </summary>
        public List<string> StoreResponses { get; set; }

        public long QuorumSelectedLSN { get; set; }

        public long GlobalCommittedSelectedLSN { get; set; }

        /// <summary>
        /// If the StoreReader should perform the local refresh for GoneException instead of 
        /// throwing is back to retry policy. This is done to avoid losing the state (response + LSN)
        /// while executing quorum read logic
        /// </summary>
        public bool PerformLocalRefreshOnGoneException { get; set; }

        /// <summary>
        /// If the background refresh has been performed for this request to eliminate the 
        /// extra replica that is not participating in quorum but causes Gone
        /// </summary>
        public bool PerformedBackgroundAddressRefresh { get; set; }

        public IClientSideRequestStatistics ClientRequestStatistics
        {
            get;
            set;
        }

        public string ResolvedCollectionRid { get; set; }

        /// <summary>
        /// Region which is going to serve the DocumentServiceRequest.
        /// Populated during address resolution for the request.
        /// </summary>
        public string RegionName { get; set; }

        /// <summary>
        /// Sets routing directive for <see cref="GlobalEndpointManager"/> to resolve
        /// the request to endpoint based on location index
        /// </summary>
        /// <param name="locationIndex">Index of the location to which the request should be routed</param>
        /// <param name="usePreferredLocations">Use preferred locations to route request</param>
        public void RouteToLocation(int locationIndex, bool usePreferredLocations)
        {
            this.LocationIndexToRoute = locationIndex;
            this.UsePreferredLocations = usePreferredLocations;
            this.LocationEndpointToRoute = null;
        }

        /// <summary>
        /// Sets location-based routing directive for <see cref="GlobalEndpointManager"/> to resolve
        /// the request to given <paramref name="locationEndpoint"/>
        /// </summary>
        /// <param name="locationEndpoint">Location endpoint to which the request should be routed</param>
        public void RouteToLocation(Uri locationEndpoint)
        {
            this.LocationEndpointToRoute = locationEndpoint;
            this.LocationIndexToRoute = null;
            this.UsePreferredLocations = null;
        }

        /// <summary>
        /// Clears location-based routing directive
        /// </summary>
        public void ClearRouteToLocation()
        {
            this.LocationIndexToRoute = null;
            this.LocationEndpointToRoute = null;
            this.UsePreferredLocations = null;
        }

        public bool? UsePreferredLocations { get; private set; }

        public int? LocationIndexToRoute { get; private set; }

        public Uri LocationEndpointToRoute { get; private set; }

        public bool EnsureCollectionExistsCheck { get; set; }

        /// <summary>
        /// Flag that enables ConnectionStateListener to trigger an address cache refresh
        /// on connection reset notification
        /// </summary>
        public bool EnableConnectionStateListener { get; set; }

        public DocumentServiceRequestContext Clone()
        {
            DocumentServiceRequestContext requestContext = new DocumentServiceRequestContext();

            requestContext.TimeoutHelper = this.TimeoutHelper;
            requestContext.ForceRefreshAddressCache = this.ForceRefreshAddressCache;
            requestContext.PerformLocalRefreshOnGoneException = this.PerformLocalRefreshOnGoneException;
            requestContext.PerformedBackgroundAddressRefresh = this.PerformedBackgroundAddressRefresh;
            requestContext.ResolvedCollectionRid = this.ResolvedCollectionRid;
            requestContext.ClientRequestStatistics = this.ClientRequestStatistics;
            requestContext.UsePreferredLocations = this.UsePreferredLocations;
            requestContext.LocationIndexToRoute = this.LocationIndexToRoute;
            requestContext.LocationEndpointToRoute = this.LocationEndpointToRoute;
            requestContext.EnsureCollectionExistsCheck = this.EnsureCollectionExistsCheck;
            requestContext.EnableConnectionStateListener = this.EnableConnectionStateListener;

            return requestContext;
        }
    }
}