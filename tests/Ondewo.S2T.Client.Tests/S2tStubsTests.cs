using System;
using System.Linq;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using Ondewo.S2T;
using Xunit;

namespace Ondewo.S2T.Client.Tests
{
    /// <summary>
    /// The product-specific half of the suite: concrete assertions against the ONDEWO S2T API,
    /// spelled out with real message, field, enum and RPC names.
    /// <para>
    /// This is the only test file that has to be rewritten when the setup is replicated to another
    /// ONDEWO product - <see cref="GeneratedStubsTests"/> carries over unchanged.
    /// </para>
    /// </summary>
    public class S2tStubsTests
    {
        private const string DummyTarget = "http://localhost:50051";

        [Fact]
        public void TranscriptionRoundTripsEveryScalarFieldKind()
        {
            var transcription = new Transcription
            {
                // The proto field is `transcription`, which collides with the message name, so the
                // generator escapes the property with a trailing underscore.
                Transcription_ = "guten morgen",
                ConfidenceScore = 0.875f,
            };
            transcription.Words.Add(new WordDetail
            {
                Word = "guten",
                StartTime = 0.25f,
                EndTime = 0.75f,
                Confidence = 0.9f,
            });

            byte[] bytes = transcription.ToByteArray();
            Transcription parsed = Transcription.Parser.ParseFrom(bytes);

            Assert.NotEmpty(bytes);
            Assert.Equal(transcription, parsed);
            Assert.Equal("guten morgen", parsed.Transcription_);
            Assert.Equal(0.875f, parsed.ConfidenceScore);

            WordDetail word = Assert.Single(parsed.Words);
            Assert.Equal("guten", word.Word);
            Assert.Equal(0.25f, word.StartTime);
            Assert.Equal(0.75f, word.EndTime);
            Assert.Equal(0.9f, word.Confidence);
        }

        [Fact]
        public void TranscribeFileRequestRoundTripsBytesAndANestedConfig()
        {
            var request = new TranscribeFileRequest
            {
                AudioFile = ByteString.CopyFromUtf8("RIFF....WAVE"),
                Config = new TranscribeRequestConfig
                {
                    S2TPipelineId = "pipeline-de-1",
                    Decoding = Decoding.BeamSearchWithLm,
                },
            };

            TranscribeFileRequest parsed = TranscribeFileRequest.Parser.ParseFrom(request.ToByteArray());

            Assert.Equal(request, parsed);
            Assert.Equal(ByteString.CopyFromUtf8("RIFF....WAVE"), parsed.AudioFile);
            Assert.Equal("pipeline-de-1", parsed.Config.S2TPipelineId);
            Assert.Equal(Decoding.BeamSearchWithLm, parsed.Config.Decoding);
        }

        [Fact]
        public void RepeatedFieldRoundTripsThroughAListResponse()
        {
            var response = new ListS2tPipelinesResponse();
            response.PipelineConfigs.Add(new Speech2TextConfig
            {
                Id = "pipeline-1",
                Active = true,
                Description = new S2tDescription { Language = "de", Domain = "medical" },
            });
            response.PipelineConfigs.Add(new Speech2TextConfig { Id = "pipeline-2" });

            ListS2tPipelinesResponse parsed =
                ListS2tPipelinesResponse.Parser.ParseFrom(response.ToByteArray());

            Assert.Equal(response, parsed);
            Assert.Equal(
                new[] { "pipeline-1", "pipeline-2" },
                parsed.PipelineConfigs.Select(config => config.Id));
            Assert.True(parsed.PipelineConfigs[0].Active);
            Assert.Equal("de", parsed.PipelineConfigs[0].Description.Language);
        }

        [Fact]
        public void MapFieldsRoundTripWithBothTheirValueKinds()
        {
            var options = new OpenaiLlmOptions { Model = "gpt-4o-transcribe" };
            options.DefaultHeaders.Add("x-tenant", "ondewo");
            options.LogitBias.Add("1234", -50);

            OpenaiLlmOptions parsed = OpenaiLlmOptions.Parser.ParseFrom(options.ToByteArray());

            Assert.Equal(options, parsed);
            Assert.Equal("gpt-4o-transcribe", parsed.Model);
            Assert.Single(parsed.DefaultHeaders);
            Assert.Equal("ondewo", parsed.DefaultHeaders["x-tenant"]);
            Assert.Single(parsed.LogitBias);
            Assert.Equal(-50, parsed.LogitBias["1234"]);
        }

        [Fact]
        public void UnsetScalarFieldsCarryTheProto3DefaultsAndStayOffTheWire()
        {
            var transcription = new Transcription();

            Assert.Equal(string.Empty, transcription.Transcription_);
            Assert.Equal(0f, transcription.ConfidenceScore);
            Assert.Empty(transcription.Words);
            Assert.Empty(transcription.ToByteArray());
        }

        /// <summary>
        /// <c>TranscribeRequestConfig.language</c> is a proto3 <c>optional</c> field, so it has
        /// explicit presence: the empty string is a value the client can actually send, and it is
        /// distinguishable from "not set" on the wire.
        /// </summary>
        [Fact]
        public void Proto3OptionalFieldKeepsExplicitPresence()
        {
            var unset = new TranscribeRequestConfig { S2TPipelineId = "pipeline-de-1" };

            Assert.False(unset.HasLanguage);
            Assert.False(unset.HasTask);
            Assert.Equal(string.Empty, unset.Language);

            var explicitlyEmpty = new TranscribeRequestConfig
            {
                S2TPipelineId = "pipeline-de-1",
                Language = string.Empty,
            };

            Assert.True(explicitlyEmpty.HasLanguage);

            // The generated Equals compares VALUES, not presence, so the two compare equal. The
            // distinction lives on the wire and in HasLanguage - which is the whole point of an
            // explicit-presence field: the empty string is a value the client can actually send.
            Assert.Equal(unset, explicitlyEmpty);
            Assert.True(
                explicitlyEmpty.ToByteArray().Length > unset.ToByteArray().Length,
                "an explicitly-set empty optional field has to reach the wire");

            TranscribeRequestConfig parsed =
                TranscribeRequestConfig.Parser.ParseFrom(explicitlyEmpty.ToByteArray());

            Assert.True(parsed.HasLanguage);
            Assert.Equal(string.Empty, parsed.Language);

            parsed.ClearLanguage();
            Assert.False(parsed.HasLanguage);
            Assert.Equal(unset.ToByteArray(), parsed.ToByteArray());
        }

        [Fact]
        public void EnumsStartAtTheirZeroValue()
        {
            Assert.Equal(0, (int)Decoding.Default);
            Assert.Equal(Decoding.Default, default(Decoding));
            Assert.Equal(0, (int)InferenceBackend.Unknown);
            Assert.Equal(InferenceBackend.Unknown, default(InferenceBackend));

            // The C# name is PascalCased and loses the enum-name prefix; the wire/JSON name is the
            // one the server speaks.
            Assert.Equal(
                "INFERENCE_BACKEND_UNKNOWN",
                InferenceBackend.Unknown.GetType()
                    .GetField(nameof(InferenceBackend.Unknown))
                    .GetCustomAttributes(typeof(Google.Protobuf.Reflection.OriginalNameAttribute), false)
                    .Cast<Google.Protobuf.Reflection.OriginalNameAttribute>()
                    .Single()
                    .Name);
        }

        [Fact]
        public void EnumFieldRoundTripsANonDefaultValue()
        {
            var config = new TranscribeRequestConfig { Decoding = Decoding.Greedy };

            TranscribeRequestConfig parsed =
                TranscribeRequestConfig.Parser.ParseFrom(config.ToByteArray());

            Assert.Equal(Decoding.Greedy, parsed.Decoding);
            Assert.NotEmpty(config.ToByteArray());
        }

        [Fact]
        public void Speech2TextClientBindsToAChannelAndExposesTheDeclaredRpcs()
        {
            using GrpcChannel channel = GrpcChannel.ForAddress(DummyTarget);

            var client = new Speech2Text.Speech2TextClient(channel);

            Assert.NotNull(client);
            Assert.Equal("ondewo.s2t.Speech2Text", Speech2Text.Descriptor.FullName);
            Assert.Contains(Speech2Text.Descriptor.Methods, method => method.Name == "TranscribeFile");

            string[] clientMethods = typeof(Speech2Text.Speech2TextClient)
                .GetMethods()
                .Select(method => method.Name)
                .Distinct()
                .ToArray();

            foreach (string rpc in new[]
                     {
                         "TranscribeFile", "GetS2tPipeline", "CreateS2tPipeline", "DeleteS2tPipeline",
                         "UpdateS2tPipeline", "ListS2tPipelines", "ListS2tLanguages", "ListS2tDomains",
                         "GetServiceInfo", "ListS2tLanguageModels", "CreateUserLanguageModel",
                         "DeleteUserLanguageModel", "AddDataToUserLanguageModel",
                         "TrainUserLanguageModel", "ListS2tNormalizationPipelines",
                     })
            {
                Assert.Contains(rpc, clientMethods);
                Assert.Contains(rpc + "Async", clientMethods);
            }
        }

        /// <summary>
        /// <c>TranscribeStream</c> is the one bidirectional-streaming RPC of this API. A streaming
        /// RPC is generated without the unary/Async pair - every overload returns the duplex call
        /// object directly - so getting the stream kind wrong is a compile-time break for
        /// consumers, not a runtime one.
        /// </summary>
        [Fact]
        public void TranscribeStreamIsGeneratedAsABidirectionalStreamingCall()
        {
            Google.Protobuf.Reflection.MethodDescriptor rpc = Assert.Single(
                Speech2Text.Descriptor.Methods, method => method.IsClientStreaming);

            Assert.Equal("TranscribeStream", rpc.Name);
            Assert.True(rpc.IsServerStreaming);
            Assert.Equal(TranscribeStreamRequest.Descriptor, rpc.InputType);
            Assert.Equal(TranscribeStreamResponse.Descriptor, rpc.OutputType);

            System.Reflection.MethodInfo[] overloads = typeof(Speech2Text.Speech2TextClient)
                .GetMethods()
                .Where(method => method.Name == "TranscribeStream")
                .ToArray();

            Assert.NotEmpty(overloads);
            Assert.All(
                overloads,
                method => Assert.Equal(
                    typeof(AsyncDuplexStreamingCall<TranscribeStreamRequest, TranscribeStreamResponse>),
                    method.ReturnType));
            Assert.DoesNotContain(
                "TranscribeStreamAsync",
                typeof(Speech2Text.Speech2TextClient).GetMethods().Select(method => method.Name));
        }
    }
}
