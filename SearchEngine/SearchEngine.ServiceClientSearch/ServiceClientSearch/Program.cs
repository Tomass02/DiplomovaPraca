using ServiceClientSearch;
using Microsoft.Extensions.Hosting;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServiceModelServices();
builder.Services.AddServiceModelMetadata();
builder.Services.AddSingleton<IServiceBehavior, UseRequestHeadersForMetadataAddressBehavior>();

builder.Services.AddTransient<ServiceClientSearch.ServiceClientSearch>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure();

var app = builder.Build();

app.UseServiceModel(serviceBuilder =>
{
    serviceBuilder.AddService<ServiceClientSearch.ServiceClientSearch>(serviceOptions =>
    {
        serviceOptions.DebugBehavior.IncludeExceptionDetailInFaults = app.Environment.IsDevelopment();
    });

    var basicHttpBinding = new BasicHttpBinding()
    {
        Name = "BasicHttpBinding_Http",
        MaxReceivedMessageSize = 30000000 - 1,
        MessageEncoding = WSMessageEncoding.Text,
        TextEncoding = Encoding.UTF8,
        Security = new BasicHttpSecurity()
        {
            Mode = BasicHttpSecurityMode.None
        }
    };

    var basicHttpsBinding = new BasicHttpBinding()
    {
        Name = "BasicHttpBinding_Http",
        MaxReceivedMessageSize = 30000000 - 1, //default for IIS 
        MessageEncoding = WSMessageEncoding.Text,
        TextEncoding = Encoding.UTF8,
        Security = new BasicHttpSecurity()
        {
            Mode = BasicHttpSecurityMode.Transport
        }
    };

    serviceBuilder.AddServiceEndpoint<ServiceClientSearch.ServiceClientSearch, IServiceClientSearch>(basicHttpBinding, "/SearchClient.svc");
    serviceBuilder.AddServiceEndpoint<ServiceClientSearch.ServiceClientSearch, IServiceClientSearch>(basicHttpsBinding, "/SearchClient.svc");

    var serviceMetadataBehavior = app.Services.GetRequiredService<ServiceMetadataBehavior>();
    serviceMetadataBehavior.HttpGetEnabled = true;
    serviceMetadataBehavior.HttpsGetEnabled = true;
});

app.Run();
