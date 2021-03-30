using System;
using System.Collections.Generic;
using System.Text;

namespace Lwaay.Tracing.Elasticsearch
{
    public class ElasticsearchOption
    {
        public string Host { get; set; }

        public int IndexRefreshInterrval { get; set; } = 10;

        public int TracingNumberOfReplicas { get; set; } = 1;

        public int TracingIndexNumberOfShards { get; set; } = 3;

    }
}
