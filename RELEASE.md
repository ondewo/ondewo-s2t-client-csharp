# Release History

*****************

## Release ONDEWO S2T Csharp Client 7.6.2

### New Features

* [[OND236-120]](https://ondewo.atlassian.net/browse/OND236-120) Keycloak authentication with offline-token start-up, matching ondewo-s2t-client-python 7.6.2 (this release carries the python SDK's 7.6.1 and 7.6.2 Keycloak work at once, so every language shares one version for one feature set): `OndewoClientConfig` gains `keycloakUrl`, `realm`, `clientId`, `username`, `password`, `tokenExpirationInS`, `verifySsl` and `refreshToken` (appended after the existing parameters, `refreshToken` last), and `OndewoChannelFactory` attaches an auto-refreshed bearer token from the new `KeycloakTokenProvider` to every call.
* [[OND236-120]](https://ondewo.atlassian.net/browse/OND236-120) A handed-off Keycloak *offline* refresh token starts the client with one `grant_type=refresh_token` exchange, and the client then never sends a password grant, not even as a fallback; a refused token is never answered with a password login. A Keycloak config needs `password` OR `refresh_token`, all Keycloak fields are validated all-or-nothing, and the password and the token are redacted in every rendering.
* [[OND236-120]](https://ondewo.atlassian.net/browse/OND236-120) Resilient refresh: a failed refresh never ends the refresh loop; the proactive refresh and the per-call (lazy) refresh share one full-jitter backoff (5 s doubling to 300 s, measured from the recorded failure) and send no request while it runs; concurrent callers share one in-flight refresh. A still-valid token keeps being served while a refresh fails, and only an expired token raises `KeycloakAuthenticationException` (carrying the cause).
* [[OND236-120]](https://ondewo.atlassian.net/browse/OND236-120) One shared provider per credential set, keyed by a SHA-256 of the credentials, so two configs of different tenants never share one. The token request has a 10 s timeout, quoted Keycloak error bodies are cut at 500 characters, and a token response is reported by its field names only, never its values.

### Bug Fixes

* [[OND236-120]](https://ondewo.atlassian.net/browse/OND236-120) A NaN backoff jitter counts as 0 (a 5 s delay) instead of producing a NaN delay; +Infinity clamps to the 300 s ceiling and -Infinity to the 5 s base.

### Improvements

* [[OND236-120]](https://ondewo.atlassian.net/browse/OND236-120) Tests: the T01-T62 catalogue with a fake token endpoint and monotonic clock, plus the real HTTP transport against an in-process Keycloak (plain, self-signed TLS, timeout) and the channel factory over TLS and plaintext; 100 % line, branch and method coverage.

API unchanged: tracking API Version [7.6.0](https://github.com/ondewo/ondewo-s2t-api/releases/tag/7.6.0) ( [Documentation](https://ondewo.github.io/ondewo-s2t-api/) )

*****************

## Release ONDEWO S2T Csharp Client 7.6.0

### Improvements

* Tracking API Version [7.6.0](https://github.com/ondewo/ondewo-s2t-api/releases/tag/7.6.0) ( [Documentation](https://ondewo.github.io/ondewo-s2t-api/) )
* Generated with ondewo-proto-compiler 5.17.0 (protoc 35.1, `grpc_csharp_plugin` from Grpc.Tools 2.85.0). New runtime floors: Google.Protobuf 3.35.1, Grpc.Core.Api / Grpc.Net.Client 2.85.0 and Google.Api.CommonProtos 2.17.0.
* The build and the release run in docker, as in ondewo-nlu-client-csharp: every `dotnet` and `gh` call runs in the utils image built from `Dockerfile.utils`, the compiler container runs as the invoking user (no `sudo`), the GitHub token is validated before anything is pushed, NuGet is published from the release host and the GitHub release is created last. The tag-triggered `release.yml` workflow is removed: CI only tests.

*****************

## Release ONDEWO S2T Csharp Client 7.5.1

### New Features

* TLS and mutual TLS: `OndewoClientConfig` and `OndewoChannelFactory.Create` (namespace
  `Ondewo.S2T.Client.Connection`) build the `GrpcChannel` the generated service clients take, with the rules of the
  ONDEWO Python client. **They exist only in the new `net10.0` target of the package**; the `netstandard2.0`
  target (.NET Framework, older .NET) does not contain them, because `SocketsHttpHandler`,
  `SslClientAuthenticationOptions` and `X509Certificate2.CreateFromPem` do not exist there.
  * `grpcCert`, `grpcClientCert` and `grpcClientKey` hold PEM content, not file paths; a certificate field
    without a PEM certificate in it is refused with `ArgumentException`.
  * With `grpcCert` set only those CAs are trusted, otherwise the platform trust store; the host is always
    checked against the certificate's SAN.
  * `grpcClientCert` / `grpcClientKey` are both-or-neither (refused in the constructor); empty strings on both
    mean plain TLS. The key must be an unencrypted PEM key (PKCS#8, PKCS#1 or SEC1).
  * `useSecureChannel: false` with a client identity is refused; a plaintext channel logs a warning naming
    `host:port` through `GrpcChannelOptions.LoggerFactory`.
  * No exception message renders a PEM or the key; `OndewoClientConfig.ToString()` redacts the key.
  * Bare IPv6 literal hosts are bracketed; CRLF PEMs work.
  * Channel defaults: keepalive ping every 30 s with a 20 s ping timeout, only while calls are active; 5 s maximum
    reconnect backoff; 2^31-1 maximum message size. Not configured (documented in the README): a per-method retry
    policy, `max_pings_without_data` and `TCP_USER_TIMEOUT`, which `Grpc.Net.Client` does not expose.

### Improvements

* The package now targets `netstandard2.0` and `net10.0`. The `netstandard2.0` target is unchanged: the
  generated stubs and `OndewoAuth`.
* README: new section "TLS, mutual TLS and certificates" (modes, rules, example, channel defaults, a test PKI
  with openssl, security notes, troubleshooting).
* Tests: real TLS and mutual TLS handshakes against an in-process Kestrel server with a PKI built at test time
  (custom CA, client chain, CRLF, IPv6, missing / unrelated client identity, wrong CA, wrong host name, system
  trust store, plaintext), and a test that pins the release-notes slice of `RELEASE.md` the GitHub release is
  cut from.
* The stubs are generated by [ondewo-proto-compiler 5.15.2](https://github.com/ondewo/ondewo-proto-compiler/releases/tag/5.15.2)
  (previously 5.15.1).
* Tracking API Version [7.5.0](https://github.com/ondewo/ondewo-s2t-api/releases/tag/7.5.0) ( [Documentation](https://ondewo.github.io/ondewo-s2t-api/) )

*****************

## Release ONDEWO S2T Csharp Client 7.5.0

### New Features

* Initial release of the ONDEWO S2T C# client. The package `Ondewo.S2T.Client` ships the
  gRPC message and client stubs generated from the [ONDEWO S2T API](https://github.com/ondewo/ondewo-s2t-api)
  by the `ondewo-csharp-proto-compiler` image of
  [ondewo-proto-compiler 5.15.1](https://github.com/ondewo/ondewo-proto-compiler): one
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
