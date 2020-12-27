﻿//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace CosmosBenchmark
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;

    internal class Echo20ServerBenchmarkOperation : IBenchmarkOperation
    {
        private static  HttpClient client;
        private readonly string requestUri;

        private readonly string partitionKeyPath;
        private readonly Dictionary<string, object> sampleJObject;

        public Echo20ServerBenchmarkOperation(BenchmarkConfig config)
        {
            this.partitionKeyPath = config.PartitionKeyPath.Replace("/", "");

            this.requestUri = config.RequestBaseUri().ToString();

            this.sampleJObject = JsonHelper.Deserialize<Dictionary<string, object>>(config.ItemTemplatePayload());
            if (Echo20ServerBenchmarkOperation.client == null)
            {
                Echo20ServerBenchmarkOperation.client = Utility.CreateHttp2Client(config.MaxConnectionsPerServer());
            }
        }

        public async Task ExecuteOnceAsync()
        {
            using (var req = new HttpRequestMessage(HttpMethod.Get, this.requestUri))
            {
                req.Version = new Version(2, 0);

                using (HttpResponseMessage responseMessage = await Echo20ServerBenchmarkOperation.client
                                        .GetAsync(this.requestUri + Guid.NewGuid().ToString(), 
                                            HttpCompletionOption.ResponseHeadersRead))
                {
                    responseMessage.EnsureSuccessStatusCode();

                    // Drain the response
                    using (Stream payload = await responseMessage.Content.ReadAsStreamAsync())
                    {
                    }
                }
            }
        }

        public Task PrepareAsync()
        {
            string newPartitionKey = Guid.NewGuid().ToString();

            this.sampleJObject["id"] = Guid.NewGuid().ToString();
            this.sampleJObject[this.partitionKeyPath] = newPartitionKey;

            return Task.CompletedTask;
        }

    }
}
