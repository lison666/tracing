using AutoMapper;
using Lwaay.Tracing.Elasticsearch.Data;
using Lwaay.Tracing.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lwaay.Tracing.Elasticsearch
{
    public class TracingElasticsearchProfile : Profile
    {
        public TracingElasticsearchProfile()
        {
            CreateMap<Model.Span, SpanModel>()
                .ForMember(s => s.ParentSpanID, m => m.MapFrom(f => f.GetParentSpanId()))
                .ForMember(s => s.ServiceName, m => m.MapFrom(f => f.GetServiceName())).ReverseMap();
            CreateMap<Model.SpanLog, SpanLogModel>().ReverseMap();
            CreateMap<Model.SpanProcess, SpanProcessModel>().ReverseMap();
            CreateMap<Model.SpanTag, SpanTagModel>();
            CreateMap<SpanTagModel, Model.SpanTag>()
                .ConvertUsing(model => Convert(model));
        }

        private static Model.SpanTag Convert(SpanTagModel model)
        {
            if (model == null)
                return null;
            var spanTag = new Model.SpanTag()
            {
                Key = model.Key,
                Type = model.Type,
            };
            switch (model.Type)
            {
                case "binary":
                case "string":
                    spanTag.Value = model.Value;
                    break;
                case "bool":
                    spanTag.Value = System.Convert.ToBoolean(model.Value);
                    break;
                case "int64":
                    spanTag.Value = System.Convert.ToInt64(model.Value);
                    break;
                case "float64":
                    spanTag.Value = System.Convert.ToDouble(model.Value);
                    break;             
                default:
                    break;
            }
            return spanTag;
        }
    }
}
