using AutoMapper;
using Lwaay.Tracing.Elasticsearch.Data;
using Lwaay.Tracing.Model.Dto;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lwaay.Tracing.Elasticsearch
{
    public class ElasticsearchProfile: Profile
    {
        public ElasticsearchProfile()
        {
            this.CreateMap<SpanModel,SpanDto>();
        }
    }
}
