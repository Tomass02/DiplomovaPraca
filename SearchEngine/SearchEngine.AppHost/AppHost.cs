string containerImage_OpenSearch = "opensearchproject/opensearch";
string containerImage_OpenSearch_Tag = "3";

//open search dashboard
string containerImage_OpenSearchDashboard = "opensearchproject/opensearch-dashboards";
string containerImage_OpenSearchDashboard_Tag = "3";
string aspire_container_runtime = "docker";

//logstash
var containerImage_LogStash_Image = "searchengine-logstash-custom";
var containerImage_LogStash_Tag = "9.0.1";

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

const string OPEN_SEARCH_CONTAINER_NAME = "opensearch";

const int OpenSearchHostPort = 9201;
const int OpenSearchApiContainerPort = 9200;
const int OpenSearchPerfAnalyzerPort = 9600;
const int OpenSearchDashboardsPort = 5601;

var builder = DistributedApplication.CreateBuilder(args);

// ***************** OpenSearch ***************************
var opensearchUsername = builder.AddParameter("opensearch-username");
var opensearchPassword = builder.AddParameter("opensearch-password", secret: true);

var openSearchInstance = builder.AddContainer(name: OPEN_SEARCH_CONTAINER_NAME, image: containerImage_OpenSearch, tag: containerImage_OpenSearch_Tag)
    .WithEnvironment(name: "OPENSEARCH_USERNAME", parameter: opensearchUsername)
    .WithEnvironment(name: "OPENSEARCH_PASSWORD", parameter: opensearchPassword)
    .WithEnvironment(name: "discovery.type", value: "single-node")
    .WithEnvironment(name: "network.host", value: "0.0.0.0") // Ensure the node is accessible
    .WithEnvironment(name: "DISABLE_SECURITY_PLUGIN", value: "true") // Disable security plugin for testing
    .WithEnvironment(name: "bootstrap.memory_lock", value: "true") // Disable JVM heap memory swapping
    .WithHttpEndpoint(port: OpenSearchHostPort, targetPort: OpenSearchApiContainerPort, name: $"{OPEN_SEARCH_CONTAINER_NAME}-api") // expose the REST API port
    .WithHttpEndpoint(port: OpenSearchPerfAnalyzerPort, targetPort: OpenSearchPerfAnalyzerPort, name: $"{OPEN_SEARCH_CONTAINER_NAME}-perfanalyzer")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithVolume(name: "opensearch-data", target: "/usr/share/opensearch/data");

var openSearchIngest = builder.AddProject<Projects.SearchEngine_OpenSearchIngest>(name: "opensearch-ingest")
    .WaitFor(openSearchInstance)
    .WithEnvironment(name: "OPENSEARCH_URI", value: "http://localhost:" + OpenSearchHostPort)
    .WithEnvironment(name: "OPENSEARCH_INDEX", value: "demo-documents")
    .WithEnvironment(name: "OPENSEARCH_USERNAME", parameter: opensearchUsername)
    .WithEnvironment(name: "OPENSEARCH_PASSWORD", parameter: opensearchPassword);

builder.AddContainer(name: "opensearch-dashboards", image: containerImage_OpenSearchDashboard, tag: containerImage_OpenSearchDashboard_Tag)
    .WaitFor(openSearchIngest)
    .WithEnvironment(name: "OPENSEARCH_USERNAME", parameter: opensearchUsername)
    .WithEnvironment(name: "OPENSEARCH_PASSWORD", parameter: opensearchPassword)
    .WithEnvironment(name: "DISABLE_SECURITY_DASHBOARDS_PLUGIN", value: "true")
    .WithEnvironment(name: "OPENSEARCH_HOSTS", "http://" + OPEN_SEARCH_CONTAINER_NAME + ":" + OpenSearchApiContainerPort)
    .WithHttpEndpoint(port: OpenSearchDashboardsPort, targetPort: OpenSearchDashboardsPort, name: "UI")
    .WithLifetime(ContainerLifetime.Persistent);

// ***************** SQL Server ***************************
var sqlPassword = builder.AddParameter("sql-password", secret: true);

var sqlServer = builder.AddSqlServer("sqlserver", password: sqlPassword)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume();

// ***************** Logstash ***************************
_ = builder.AddContainer(name: "logstash", image: containerImage_LogStash_Image, tag: containerImage_LogStash_Tag)
    .WithDockerfile("./logstash")
    .WaitFor(openSearchInstance)
    .WaitFor(sqlServer)
    .WithEnvironment(name: "MSSQL_USER", value: "sa")
    .WithEnvironment(name: "MSSQL_PASSWORD", parameter: sqlPassword)
    .WithEnvironment(name: "OPENSEARCH_URL", value: "http://" + OPEN_SEARCH_CONTAINER_NAME + ":" + OpenSearchApiContainerPort)
    .WithEnvironment(name: "XPACK_MONITORING_ENABLED", value: "false")
    .WithLifetime(ContainerLifetime.Persistent);

builder.AddProject<Projects.SearchEngine_DatabaseDeploy>(name: "database-deploy")
    .WithEnvironment(name: "DB_DEPLOY_SQL_SERVER", value: "sqlserver")
    .WithEnvironment(name: "DB_DEPLOY_SQL_DATABASE", value: "master")
    .WithEnvironment(name: "DB_DEPLOY_CONNECTION_STRING", sqlServer.Resource.ConnectionStringExpression)
    .WithReference(sqlServer)
    .WaitFor(sqlServer);

builder.Build().Run();