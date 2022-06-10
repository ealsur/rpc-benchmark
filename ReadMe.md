

# Tasks

## With pre-configured account routing info
- [ ] Http1 and Http2 E2E integration 
- [ ] Rntbd2 E2E integration (Use .NET Pipe abstraction if possible leverage on client as well)
- [ ] Grpc implementation
- [ ] Http3 endpoint and E2E integration 
- [ ] Http2 Comsos header HPAC possibility (.NET seems only supporting for default header's)
- [ ] Native Rntbd server (With Backend code)
- [ ] Native Http2 server (explore Http2 implementations)

## Benchmarking (Local core)
- [ ] Server AI integration (RPS, latency and scenario labeling/dimension)
- [ ] Azure stand-by runner (Router and proxy on same node)
- [ ] Azure stand-by runner (Router and proxy on different node's)

## E2E scenario with auto account routing
- [ ] Dotnet implementation 
- [ ] 


## E2E scenario flow (including session concept)
- [ ] Dotnet implementation 

Proposal


![image](https://user-images.githubusercontent.com/6880899/172974154-57e81c2a-80d3-4e0c-8fa7-c1091fbc116d.png)



To start gRPC service
```
cd GrpcService/
dotnet run -c retail -- -w gRPC
```

To start Dotnet Http2 service
```
cd GrpcService/
dotnet run -c retail -- -w http2
```

To start JAVA reactor Http2, Tcp (a.k.a. Rntbd) service
```
cd server-java/
mvn clean package -f pom.xml -DskipTests -Dgpg.skip -Ppackage-assembly
java -jar ./target/TestServer-1.0-SNAPSHOT-jar-with-dependencies.jar
```

How to run the client's
```
cd Client/
dotnet run -c retail -- -w [Http11|DotnetHttp2|ReactorHttp2|Grpc|Tcp]  -e [localhost/ipv4] -c 8 --mcpe 2
```
