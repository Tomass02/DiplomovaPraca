string containerImage_OpenSearch = "opensearchproject/opensearch";
string containerImage_OpenSearch_Tag = "3";

// Prima banka local development for getting container images from gitlab.pkb.sk private container registry
if (Environment.UserDomainName == "PKB")
{
    containerImage_OpenSearch = "gitlab.pkb.sk:5050/prima/pbi/opensearch";
    containerImage_OpenSearch_Tag = "latest";
}

Environment.SetEnvironmentVariable("ASPIRE_CONTAINER_RUNTIME", "podman");

const string OPEN_SEARCH_SINGLE_NODE = "opensearch";

var builder = DistributedApplication.CreateBuilder(args);

// ***************** OpenSearch (single node) ***************************
builder.AddContainer(name: OPEN_SEARCH_SINGLE_NODE, image: containerImage_OpenSearch, tag: containerImage_OpenSearch_Tag)
    .WithEnvironment(name: "discovery.type", value: "single-node")
    .WithEnvironment(name: "network.host", value: "0.0.0.0")
    .WithEnvironment(name: "DISABLE_SECURITY_PLUGIN", value: "true") // Disable security plugin for testing
    .WithEnvironment(name: "bootstrap.memory_lock", value: "true")
    .WithHttpEndpoint(port: 9200, targetPort: 9200, name: $"{OPEN_SEARCH_SINGLE_NODE}-api")
    .WithHttpEndpoint(port: 9600, targetPort: 9600, name: $"{OPEN_SEARCH_SINGLE_NODE}-perfanalyzer")
    .WithLifetime(ContainerLifetime.Persistent);

builder.Build().Run();


