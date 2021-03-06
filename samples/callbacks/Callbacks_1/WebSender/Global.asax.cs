﻿using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using NServiceBus;

public class MvcApplication : HttpApplication
{
    public static IEndpointInstance Endpoint;

    protected void Application_Start()
    {
        StartBus().GetAwaiter().GetResult();
        AreaRegistration.RegisterAllAreas();
        RouteTable.Routes.MapRoute(
            name: "Default",
            url: "{controller}/{action}/{id}",
            defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
    }

    protected void Application_End()
    {
        Endpoint?.Stop().GetAwaiter().GetResult();
    }

    async Task StartBus()
    {
        EndpointConfiguration endpointConfiguration = new EndpointConfiguration("Samples.Callbacks.WebSender");
        endpointConfiguration.UseSerialization<JsonSerializer>();
        endpointConfiguration.UsePersistence<InMemoryPersistence>();
        endpointConfiguration.EnableInstallers();
        endpointConfiguration.SendFailedMessagesTo("error");
        endpointConfiguration.ScaleOut()
            .InstanceDiscriminator("1");

        Endpoint = await NServiceBus.Endpoint.Start(endpointConfiguration);
    }

}