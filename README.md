# ReRe

This quick start provides instructions on how to integrate ReRe into your application.

## Adding ReRe to the Service Collection and IApplicationBuilder

To add ReRe to your projects do the following.

1. **Install the ReRe Package**: You can install it by cloning the source and reference the .csproj file.

2. **Configure Services in `Startup.cs`**: In your `Startup.cs` file, configure the services in the `ConfigureServices` method. Use the `AddReRe` extension method to add ReRe services to the service collection.

    AddReRe takes an `params Assembly[]` array. 
    The provided assemblies are scanned for classes implementing `IRequestHandler<,>`. Each detected `IRequestHandler<,>` implementation will be added to the servicecollection as `Singleton` instances.
   ```csharp
   public void ConfigureServices(IServiceCollection services)
   {
       
       services.AddReRe((factory, options) =>
       {
           // Configure your RabbitMQ connection and options here
           factory.HostName = "your_rabbitmq_host";
           factory.UserName = "your_username";
           factory.Password = "your_password";

           // options are currently empty, but will contain RabbitMQ specific options affecting the behavior of ReRe
       }, Assembly.GetExecutingAssembly()); 
        


       // Other service configurations...
   }
   ```

3. **Configure the Application Builder**: In the `Configure` method of your `Startup.cs` file, use the `UseReRe` extension method to configure the application builder.
This will startup the RabbitMQ consumers for the `IRequestHandler<,>`'s found upon scanning the provided assemblies in `AddReRe`.
   ```csharp
   public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
   {
       // Other middleware configurations...

       app.UseReRe();
   }
   ```


## Using `IRequestClient<TReq>`

The `IRequestClient<TReq>` interface is used to send requests and receive responses asynchronously. Here is an example of how to use it:

1. **Inject `IRequestClient<TReq>`**: Inject the `IRequestClient<TReq>` interface into your service or controller.
   ```csharp
   public class MyService
   {
        private readonly IRequestClient<MyRequest> _requestClient;

        public MyService(IRequestClient<MyRequest> requestClient)
        {
            _requestClient = requestClient;
        }
   }
   ```

2. **Send a Request**: Use the `GetResponse` method to send a request and receive a response.
   ```csharp
   public class MyService
   {
        private readonly IRequestClient<MyRequest> _requestClient;
        
        public MyService(IRequestClient<MyRequest> requestClient) { ... }


        public async Task<MyResponse> SendRequestAsync(MyRequest request)
        {
            return await _requestClient.GetResponse<MyResponse>(request);
        }

   }
   ```

## Using `IRequestHandler<TReq, TRes>`

The `IRequestHandler<TReq, TRes>` interface is used to handle requests and produce responses asynchronously. Here is an example of how to implement and use it:

1. **Implement `IRequestHandler<TReq, TRes>`**: Create a class that implements the `IRequestHandler<TReq, TRes>` interface.
   ```csharp
   public class MyRequestHandler : IRequestHandler<MyRequest, MyResponse>
   {
       public async Task<MyResponse> Handle(MessageContext<MyRequest> context)
       {

            // Do something with `context.Payload` - or not. 

           // Handle the request and produce a response
           var response = new MyResponse
           {
               // Set response properties based on the request
           };
           return await Task.FromResult(response);
       }
   }
   ```

