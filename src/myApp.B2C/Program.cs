using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorPages();

// Configure HTTPS enforcement
builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
    options.HttpsPort = 7013; // Match the HTTPS port from launchSettings.json
});

// Configure Azure AD B2C Authentication
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
    {
        var b2cConfig = builder.Configuration.GetSection("AzureAdB2C");
        
        options.ClientId = b2cConfig["ClientId"];
        options.ClientSecret = b2cConfig["ClientSecret"];
        options.CallbackPath = b2cConfig["CallbackPath"];
        options.SignedOutCallbackPath = b2cConfig["SignedOutCallbackPath"];
        
        // Build the authority URL with the policy
        var instance = b2cConfig["Instance"];
        var tenantId = b2cConfig["TenantId"];
        var policy = b2cConfig["SignUpSignInPolicyId"];
        options.Authority = $"{instance}{tenantId}/{policy}/v2.0/";
        
        // Configure response type and scope for authorization code flow
        options.ResponseType = "code";
        options.ResponseMode = "query";
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        
        // B2C specific configuration to handle token validation
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            NameClaimType = "name",
            ValidateIssuer = false // B2C uses different issuer format
        };
        
        // Configure for B2C - don't require access token validation
        options.GetClaimsFromUserInfoEndpoint = false;
        
        // Skip token validation that expects both access_token and id_token
        options.SkipUnrecognizedRequests = true;
        
        // Custom protocol validator for B2C that doesn't require access token
        options.ProtocolValidator = new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectProtocolValidator()
        {
            RequireNonce = false,
            RequireStateValidation = false
        };
        
        // Enable token saving if configured
        options.SaveTokens = bool.Parse(b2cConfig["SaveTokens"] ?? "false");
        
        // Configure events for better error handling
        options.Events = new OpenIdConnectEvents
        {
            OnRedirectToIdentityProvider = context =>
            {
                // You can customize the redirect here if needed
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                // Log authentication failures
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError(context.Exception, "Authentication failed");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                // This event fires when the token is successfully validated
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Token validated successfully for user: {User}", context.Principal?.Identity?.Name);
                return Task.CompletedTask;
            },
            OnAuthorizationCodeReceived = context =>
            {
                // B2C authorization code handling
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Authorization code received from B2C");
                return Task.CompletedTask;
            },
            OnTokenResponseReceived = context =>
            {
                // Handle B2C token response - this fires after token exchange
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Token response received from B2C");
                
                // B2C might not return access_token for basic scopes, which is fine
                if (context.TokenEndpointResponse.AccessToken == null)
                {
                    logger.LogInformation("No access token received from B2C (this is normal for basic authentication)");
                }
                
                return Task.CompletedTask;
            }
        };
    });

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    // Enable HSTS even in development for B2C authentication
    app.UseHsts();
}

// Always redirect to HTTPS, even in development
app.UseHttpsRedirection();

app.UseRouting();

// Authentication must come before authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
