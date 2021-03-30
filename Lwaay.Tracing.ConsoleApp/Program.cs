using Google.Protobuf;
using Grpc.Net.Client;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Google.Protobuf.WellKnownTypes;

namespace Lwaay.Tracing.ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            //Console.WriteLine("Hello World!");
            var channel = GrpcChannel.ForAddress("http://localhost:6003", new GrpcChannelOptions()
            {
                //DisposeHttpClient = true,
                ThrowOperationCanceledOnCancellation = true,

            });




            var client = new Jaeger.ApiV2.QueryService.QueryServiceClient(channel);
            var response = client.GetServices(new Jaeger.ApiV2.GetServicesRequest()
            {
            }, new Grpc.Core.CallOptions());
            Console.WriteLine(string.Join(",", response.Services));

            var response1 = client.GetOperations(new Jaeger.ApiV2.GetOperationsRequest()
            {
                Service = response.Services[0],
            }, new Grpc.Core.CallOptions());
            Console.WriteLine(string.Join(",", response1.OperationNames));

            var response2 = client.GetTrace(new Jaeger.ApiV2.GetTraceRequest()
            {
                TraceId = ByteString.CopyFrom(Jaeger.TraceId.FromString("4399add89a1b8a02").ToByteArray()),
            }, new Grpc.Core.CallOptions());
            while (await response2.ResponseStream.MoveNext(CancellationToken.None))
            {
                Console.WriteLine(string.Join("|", response2.ResponseStream.Current.Spans.Select(s => s.SpanId.ToString())));
            }
            var ps= new Jaeger.ApiV2.TraceQueryParameters()
            {
                ServiceName = response.Services[0],
                StartTimeMin = Timestamp.FromDateTimeOffset(DateTimeOffset.Now.AddDays(-7)),
                //OperationName= response1.OperationNames[0],
                StartTimeMax = Timestamp.FromDateTimeOffset(DateTimeOffset.Now),
                OperationName = "HTTP GET",
                //DurationMin=Duration.FromTimeSpan(TimeSpan.Zero)
            };
            //ps.Tags.Add("http.method", "GET");
            var response3 = client.FindTraces(new Jaeger.ApiV2.FindTracesRequest()
            {
                Query = ps
            }, new Grpc.Core.CallOptions());
            while (await response3.ResponseStream.MoveNext(CancellationToken.None))
            {
                Console.WriteLine(string.Join("|", response3.ResponseStream.Current.Spans.Select(s => s.SpanId.ToString())));
            }


        }
    }
}
