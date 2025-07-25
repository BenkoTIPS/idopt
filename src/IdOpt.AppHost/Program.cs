var builder = DistributedApplication.CreateBuilder(args);

// Use LocalDB for development instead of containerized SQL Server
builder.AddConnectionString("identitydb", "Server=(localdb)\\mssqllocaldb;Database=IdOpt_IdentityDb;Trusted_Connection=true;MultipleActiveResultSets=true");

builder.AddProject<Projects.myApp_Simple>("myapp-simple")
    .WithHttpsEndpoint(name: "simple-https");

builder.AddProject<Projects.myApp_SqlIdentity>("myapp-sqlidentity")
    .WithHttpsEndpoint(name: "sqlidentity-https");

builder.AddProject<Projects.myApp_KeyCloak>("myapp-keycloak")
    .WithHttpsEndpoint(name: "keycloak-https");

builder.AddProject<Projects.myApp_EasyAuth>("myapp-easyauth")
    .WithHttpsEndpoint(name: "easyauth-https");

builder.AddProject<Projects.myApp_B2C>("myapp-b2c")
    .WithHttpsEndpoint(port: 7025, name: "b2c-https");

builder.Build().Run();
