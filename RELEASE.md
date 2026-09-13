# Release History

*****************

## Release ONDEWO S2T Csharp Client 7.5.0

### New Features

* Initial release of the ONDEWO S2T C# client. The package `Ondewo.S2T.Client` ships the
  gRPC message and client stubs generated from the [ONDEWO S2T API](https://github.com/ondewo/ondewo-s2t-api)
  by the `ondewo-csharp-proto-compiler` image of
  [ondewo-proto-compiler 5.15.0](https://github.com/ondewo/ondewo-proto-compiler): one
  `<Message>.cs` per proto plus one `<Service>Grpc.cs` per proto that declares a service, nested by C# namespace
  under `api/`.
* The package targets `netstandard2.0`, so it is consumable from .NET Framework 4.6.1+, .NET Core 2.0+ and every
  modern .NET, and carries `Google.Protobuf`, `Grpc.Net.Client`, `Grpc.Core.Api` and `Google.Api.CommonProtos` as
  its only dependencies. The `google/api`, `google/rpc` and `google/type` descriptors the API imports come from
  `Google.Api.CommonProtos` rather than being generated a second time into this assembly.
* `make build` is the whole pipeline — submodule checkout at the pinned versions, compiler image build, stub
  generation and `dotnet pack` — and `make ondewo_release` cuts the GitHub and NuGet release from the same
  version number.

*****************
