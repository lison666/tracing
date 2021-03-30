using AutoMapper;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Jaeger.ApiV2;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lwaay.Tracing.Grpc
{
    public class GrpcProfile : Profile
    {
        public GrpcProfile()
        {

            this.CreateMap<Jaeger.ApiV2.Span, Lwaay.Tracing.Model.Span>();
            this.CreateMap<Jaeger.ApiV2.Process, Lwaay.Tracing.Model.SpanProcess>().ReverseMap();
            this.CreateMap<Jaeger.ApiV2.SpanRef, Lwaay.Tracing.Model.SpanReference>()
                .ForMember(f => f.RefType, m =>
                    m.MapFrom(c => c.RefType == Jaeger.ApiV2.SpanRefType.ChildOf ? Model.SpanRefType.ChildOf : Model.SpanRefType.FollowsFrom)
                );
            this.CreateMap<Jaeger.ApiV2.Batch, Lwaay.Tracing.Model.TracingBatch>();
            this.CreateMap<Jaeger.ApiV2.KeyValue, Lwaay.Tracing.Model.SpanTag>()
                .ConvertUsing(kv => ConvertKeyValue(kv));
            this.CreateMap<Jaeger.ApiV2.Log, Lwaay.Tracing.Model.SpanLog>().ReverseMap();

            this.CreateMap<Timestamp, long>()
                .ConstructUsing(c => GetUnixTimeSecondByTimestamp(c));
            this.CreateMap<Duration, int>()
                .ConstructUsing(d => GetlDuration(d));

            this.CreateMap<Google.Protobuf.ByteString, string>()
                .ConvertUsing(d => ByteStringToString(d));

            this.CreateMap(typeof(RepeatedField<>), typeof(List<>))
                .ConvertUsing(typeof(RepeatedFieldToListTypeConverter<,>));

            this.CreateMap<Lwaay.Tracing.Model.SpanReference, Jaeger.ApiV2.SpanRef>()
                .ForMember(f => f.RefType, m => m.MapFrom(c => c.RefType == Model.SpanRefType.ChildOf ? Jaeger.ApiV2.SpanRefType.ChildOf : Jaeger.ApiV2.SpanRefType.FollowsFrom));

            this.CreateMap<long, Timestamp>()
                 .ConstructUsing(d => GetTimestamp(d));
            this.CreateMap<int, Duration>()
                .ConstructUsing(d => Google.Protobuf.WellKnownTypes.Duration.FromTimeSpan(TimeSpan.FromMilliseconds(d)));
            this.CreateMap<Lwaay.Tracing.Model.SpanTag, Jaeger.ApiV2.KeyValue>()
                .ConvertUsing(d => ConvertSpanTag(d));

            this.CreateMap(typeof(IEnumerable<>), typeof(RepeatedField<>))
               .ConvertUsing(typeof(ListToRepeatedFieldTypeConverter<,>));

            this.CreateMap<Lwaay.Tracing.Model.Span, Jaeger.ApiV2.Span>()
                .ForMember(d => d.SpanId, m => m.MapFrom(c => ByteString.CopyFrom(Jaeger.SpanId.FromString(c.SpanId).ToByteArray())))
                .ForMember(d => d.TraceId, m => m.MapFrom(c => ByteString.CopyFrom(Jaeger.TraceId.FromString(c.TraceId).ToByteArray())));

            this.CreateMap<Jaeger.ApiV2.TraceQueryParameters, Model.Dto.TracingQueryParameter>()
                .ForMember(f => f.StartTimeMax, m => m.MapFrom(c => c.StartTimeMax == null ? (long?)null : GetUnixTimeSecondByTimestamp(c.StartTimeMax)))
                .ForMember(f => f.StartTimeMin, m => m.MapFrom(c => c.StartTimeMin == null ? (long?)null : GetUnixTimeSecondByTimestamp(c.StartTimeMin)))
                .ForMember(f => f.DurationMin, m => m.MapFrom(c => c.DurationMin == null ? (int?)null : GetlDuration(c.DurationMin)))
                .ForMember(f => f.DurationMax, m => m.MapFrom(c => c.DurationMax == null ? (int?)null : GetlDuration(c.DurationMax)))
                .ForMember(f => f.OperationName, m => m.MapFrom(c => c.OperationName))
                .ForMember(f => f.ServiceName, m => m.MapFrom(c => c.ServiceName))
                .ForMember(f => f.Tags, m => m.MapFrom(c => c.Tags));

            this.CreateMap<Model.Dto.TracingDto, Jaeger.ApiV2.Trace>();
            this.CreateMap<Model.SpanProcess, Jaeger.ApiV2.Process>();
            this.CreateMap<Dictionary<string, Model.SpanProcess>, RepeatedField<Jaeger.ApiV2.Trace.Types.ProcessMapping>>()
                .ConvertUsing((spanProcess, s, content) =>
                {
                    var processMapping = s ?? new RepeatedField<Jaeger.ApiV2.Trace.Types.ProcessMapping>();
                    foreach (var item in spanProcess)
                    {
                        processMapping.Add(new Jaeger.ApiV2.Trace.Types.ProcessMapping()
                        {
                            ProcessId = item.Key,
                            Process = content.Mapper.Map<Process>(item.Value),
                        });
                    }
                    return processMapping;
                });

            this.CreateMap<Model.Dto.SpanDto, Jaeger.ApiV2.Span>()
                 .ForMember(d => d.SpanId, m => m.MapFrom(c => ByteString.CopyFrom(Jaeger.SpanId.FromString(c.SpanId).ToByteArray())))
                .ForMember(d => d.TraceId, m => m.MapFrom(c => ByteString.CopyFrom(Jaeger.TraceId.FromString(c.TraceId).ToByteArray())));

            this.CreateMap<Jaeger.ApiV2.Span, Model.Dto.SpanDto>();

            this.CreateMap<Model.SpanReference, Jaeger.ApiV2.SpanRef>()
                  .ForMember(d => d.SpanId, m => m.MapFrom(c => ByteString.CopyFrom(Jaeger.SpanId.FromString(c.SpanId).ToByteArray())))
                .ForMember(d => d.TraceId, m => m.MapFrom(c => ByteString.CopyFrom(Jaeger.TraceId.FromString(c.TraceId).ToByteArray())))
                .ForMember(d => d.RefType, m => m.MapFrom(c => c.RefType == Model.SpanRefType.ChildOf ? SpanRefType.ChildOf : SpanRefType.FollowsFrom));
            this.CreateMap<Dictionary<string, string>, MapField<string, string>>()
                .ConvertUsing((d, m) =>
                {
                    m.Add(d);
                    return m;
                });
        }
        public static int GetlDuration(Duration duration)
        {
            return Convert.ToInt32(duration.ToTimeSpan().TotalMilliseconds / 10);
        }

        public static long GetUnixTimeSecondByTimestamp(Timestamp timestamp)
        {
            return timestamp.ToDateTimeOffset().ToUnixTimeSeconds();
        }

        public static Timestamp GetTimestamp(long time)
        {
            var newTime = DateTimeOffset.FromUnixTimeSeconds(time);
            return Timestamp.FromDateTimeOffset(newTime);
        }
        public static string ByteStringToString(ByteString byteString)
        {
            if (byteString.Length == 16)
            {
                var traceId = GetTracingId(byteString);
                return traceId.Low.ToString("x016");
            }
            var spanId = GetSpanId(byteString);
            return spanId.ToString("x016");
        }

        static Jaeger.ApiV2.KeyValue ConvertSpanTag(Lwaay.Tracing.Model.SpanTag spanTag)
        {
            var kv = new Jaeger.ApiV2.KeyValue()
            {
                Key = spanTag.Key,
            };
            switch (spanTag.Type)
            {
                case "bool":
                    kv.VBool = Convert.ToBoolean(spanTag.Value);
                    kv.VType = Jaeger.ApiV2.ValueType.Bool;
                    break;
                case "String":
                    kv.VStr = spanTag.Value.ToString();
                    kv.VType = Jaeger.ApiV2.ValueType.String;
                    break;
                case "int64":
                    kv.VInt64 = Convert.ToInt64(spanTag.Value);
                    kv.VType = Jaeger.ApiV2.ValueType.Int64;
                    break;
                case "float64":
                    kv.VFloat64 = Convert.ToDouble(spanTag.Value);
                    kv.VType = Jaeger.ApiV2.ValueType.Float64;
                    break;
                case "binary":
                    kv.VBinary = ByteString.CopyFrom(Encoding.Unicode.GetBytes(spanTag.Value.ToString()));
                    kv.VType = Jaeger.ApiV2.ValueType.Binary;
                    break;
                default:
                    break;
            }
            return kv;
        }

        static Lwaay.Tracing.Model.SpanTag ConvertKeyValue(Jaeger.ApiV2.KeyValue key)
        {
            Lwaay.Tracing.Model.SpanTag spanTag = new Model.SpanTag();
            spanTag.Key = key.Key;
            spanTag.Type = key.VType.ToString().ToLower();
            switch (key.VType)
            {
                case Jaeger.ApiV2.ValueType.String:
                    spanTag.Value = key.VStr;
                    break;
                case Jaeger.ApiV2.ValueType.Bool:
                    spanTag.Value = key.VBool;
                    break;
                case Jaeger.ApiV2.ValueType.Int64:
                    spanTag.Value = key.VInt64;
                    break;
                case Jaeger.ApiV2.ValueType.Float64:
                    spanTag.Value = key.VFloat64;
                    break;
                case Jaeger.ApiV2.ValueType.Binary:
                    spanTag.Value = ByteStringToString(key.VBinary);
                    break;
                default:
                    break;
            }
            return spanTag;
        }

        public static long GetSpanId(ByteString byteString)
        {
            if (byteString.Length != 8)
                return 0;

            byte[] bytes = byteString.ToArray();
            long low = BytesToLong(bytes);
            return low;
        }

        public static (long High, long Low) GetTracingId(ByteString byteString)
        {
            if (byteString.Length != 16)
                return (0, 0);

            byte[] bytes = byteString.ToArray();
            long high = BytesToLong(bytes[0..8]);
            long low = BytesToLong(bytes[8..16]);
            return (high, low);
        }

        public static long BytesToLong(byte[] bytes)
        {
            if (BitConverter.IsLittleEndian)
            {
                return BinaryPrimitives.ReadInt64BigEndian(bytes);
            }
            return BinaryPrimitives.ReadInt64LittleEndian(bytes);
        }


        private class RepeatedFieldToListTypeConverter<TITemSource, TITemDest> : ITypeConverter<RepeatedField<TITemSource>, List<TITemDest>>
        {
            public List<TITemDest> Convert(RepeatedField<TITemSource> source, List<TITemDest> destination, ResolutionContext context)
            {
                if (source == null)
                    return destination ?? new List<TITemDest>();
                destination = destination ?? new List<TITemDest>(source.Count);
                foreach (var item in source)
                {
                    destination.Add(context.Mapper.Map<TITemDest>(item));
                }
                return destination;
            }
        }

        private class ListToRepeatedFieldTypeConverter<TITemDest, TITemSource> : ITypeConverter<IEnumerable<TITemDest>, RepeatedField<TITemSource>>
        {
            public RepeatedField<TITemSource> Convert(IEnumerable<TITemDest> source, RepeatedField<TITemSource> destination, ResolutionContext context)
            {
                destination = destination ?? new RepeatedField<TITemSource>();
                if (source != null && source.Count() > 0)
                {
                    destination.Add(context.Mapper.Map<IEnumerable<TITemSource>>(source));
                }
                return destination;
            }
        }
    }
}
