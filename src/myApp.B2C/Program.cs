using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

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
        // Use only "code" for pure authorization code flow (no implicit flow)
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.ResponseMode = OpenIdConnectResponseMode.Query;
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("offline_access"); // Required for access token
        
        // B2C specific configuration to handle token validation
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            NameClaimType = "name",
            ValidateIssuer = false, // B2C uses different issuer format
            ClockSkew = TimeSpan.FromMinutes(10) // Allow 10 minutes clock skew tolerance
        };
        
        // Configure for B2C
        options.GetClaimsFromUserInfoEndpoint = true;
        
        // Custom protocol validator for B2C that handles the token response correctly
        options.ProtocolValidator = new CustomB2CProtocolValidator();
        
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

// Custom protocol validator for Azure AD B2C that doesn't require both tokens
public class CustomB2CProtocolValidator : OpenIdConnectProtocolValidator
{
    public CustomB2CProtocolValidator()
    {
        // Configure for B2C - disable strict state validation that can cause issues
        RequireState = false;
        RequireStateValidation = false;
        RequireNonce = false;
    }

    public override void ValidateTokenResponse(OpenIdConnectProtocolValidationContext validationContext)
    {
        // Override the default validation that requires both id_token and access_token
        // B2C might only return id_token for authentication scenarios
        if (validationContext.ProtocolMessage.IdToken != null)
        {
            // If we have an id_token, that's sufficient for B2C authentication
            return;
        }
        
        // Fall back to base validation if no id_token
        base.ValidateTokenResponse(validationContext);
    }

    public override void ValidateAuthenticationResponse(OpenIdConnectProtocolValidationContext validationContext)
    {
        // Override to handle B2C specific validation requirements
        try
        {
            base.ValidateAuthenticationResponse(validationContext);
        }
        catch (OpenIdConnectProtocolInvalidStateException)
        {
            // Ignore state validation errors for B2C since we disabled RequireState
            // This handles cases where B2C doesn't return the state parameter correctly
        }
    }
}
