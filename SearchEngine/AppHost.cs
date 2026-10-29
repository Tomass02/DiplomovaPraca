string containerImage_OpenSearch = "opensearchproject/opensearch";
string containerImage_OpenSearch_Tag = "3";

//open search dashboard
string containerImage_OpenSearchDashboard = "opensearchproject/opensearch-dashboards";
string containerImage_OpenSearchDashboard_Tag = "3";
string aspire_container_runtime = "docker";

//logstash
//containerImage_LogStash_Image = "gitlab.pkb.sk:5050/prima/pbi/logstash";
//containerImage_LogStash_Tag = "9.0.1";

// Prima banka local development for getting container images from gitlab.pkb.sk private container registry
if (Environment.UserDomainName == "PKB")
{
    aspire_container_runtime = "podman";
    containerImage_OpenSearch = "gitlab.pkb.sk:5050/prima/pbi/opensearch";
    containerImage_OpenSearchDashboard = "gitlab.pkb.sk:5050/prima/pbi/opensearch-dashboards";
    containerImage_OpenSearch_Tag = "latest";
    containerImage_OpenSearchDashboard_Tag = "latest";
}

Environment.SetEnvironmentVariable("ASPIRE_CONTAINER_RUNTIME", aspire_container_runtime);

const string OPEN_SEARCH = "opensearch";

var builder = DistributedApplication.CreateBuilder(args);

// ***************** OpenSearch ***************************
var opensearchUsername = builder.AddParameter("opensearch-username");
var opensearchPassword = builder.AddParameter("opensearch-password", secret: true);

var openSearchInstance = builder.AddContainer(name: OPEN_SEARCH, image: containerImage_OpenSearch, tag: containerImage_OpenSearch_Tag)
    .WithEnvironment(name: "OPENSEARCH_USERNAME", parameter: opensearchUsername)
    .WithEnvironment(name: "OPENSEARCH_PASSWORD", parameter: opensearchPassword)
    .WithEnvironment(name: "discovery.type", value: "single-node")
    .WithEnvironment(name: "network.host", value: "0.0.0.0") // Ensure the node is accessible
    .WithEnvironment(name: "DISABLE_SECURITY_PLUGIN", value: "true") // Disable security plugin for testing
    .WithEnvironment(name: "bootstrap.memory_lock", value: "true") // Disable JVM heap memory swapping
    .WithHttpEndpoint(port: 9200, targetPort: 9200, name: $"{OPEN_SEARCH}-api") // expose the REST API port
    .WithHttpEndpoint(port: 9600, targetPort: 9600, name: $"{OPEN_SEARCH}-perfanalyzer")
    .WithLifetime(ContainerLifetime.Persistent);
    //.WithVolume(name: "opensearch-data", target: "/usr/share/opensearch/data");

var openSearchIngest = builder.AddProject<Projects.SearchEngine_OpenSearchIngest>(name: "opensearch-ingest")
    .WaitFor(openSearchInstance)
    .WithEnvironment(name: "OPENSEARCH__URI", value: "http://localhost:9200")
    .WithEnvironment(name: "OPENSEARCH__INDEX", value: "demo-documents")
    .WithEnvironment(name: "OPENSEARCH__USERNAME", parameter: opensearchUsername)
    .WithEnvironment(name: "OPENSEARCH__PASSWORD", parameter: opensearchPassword);

builder.AddContainer(name: "opensearch-dashboards", image: containerImage_OpenSearchDashboard, tag: containerImage_OpenSearchDashboard_Tag)
    .WaitFor(openSearchIngest)
    .WithEnvironment(name: "OPENSEARCH_USERNAME", parameter: opensearchUsername)
    .WithEnvironment(name: "OPENSEARCH_PASSWORD", parameter: opensearchPassword)
    .WithEnvironment(name: "DISABLE_SECURITY_DASHBOARDS_PLUGIN", value: "true")
    .WithEnvironment(name: "OPENSEARCH_HOSTS", $"http://{OPEN_SEARCH}:9200")
    .WithHttpEndpoint(port: 5601, targetPort: 5601, name: "UI")
    .WithLifetime(ContainerLifetime.Persistent);

builder.Build().Run();
