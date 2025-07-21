# BNK25 - Identity Session

In the today's crazy world of exploits, hacker tricks and rapidly changing landscape of technology tools and techniques, choosing the right identity and access management solution is critical for securing applications and providing seamless user experiences. With so many options to choose from, like Azure Active Directory B2C, Azure Easy Auth, Keycloak, IdentityServer, Okta, among others, it can be challenging to determine the best fit for your application's needs. How do they work? How are they different? Where did they come from and how do I pick the right one? In this session we will explore the options, demo how they work, and discuss how to make the right choice for your application.

## Development Setup

### Prerequisites

- .NET 9.0 SDK
- Node.js and npm (for Azurite storage emulator)
- Visual Studio Code

### Quick Start

1. Press **F5** in VS Code to start debugging
2. The Azurite storage emulator will start automatically if not already running
3. Your application will launch at `https://localhost:7260`

### How It Works

When you press F5:

1. VS Code builds your project
2. Starts the Azurite storage emulator in the background (using `.azurite` folder)
3. Launches your myVideos application
4. Opens your browser automatically

The Azurite emulator will continue running in the background for subsequent debugging sessions.

## Demos

- Simple Identity App
- SQL Auth
- EasyAuth
- Azure AD B2C
- others

## Questions

- Identity vs Security
- The difference between Authentication vs Authorization
- What are tokens? SAML and JWT? How do I get them and work with them? Testing & Debugging approaches?
- Role based vs Claims based authentication?
- What is the Ambassador Pattern?
- Identity Store
- Identity Provider
- Microsoft's attempt at Identity, a historical look at the evolution of things
- What impact does my storage solution have on my identity approach?
- Monolith & Microservice differences in auth requirements?
What options are there?
- Roll your own Auth - Security by obscurity is not safe
- SQL Auth - built into .net since early days...simply check the box and it works
- AAD & B2C - Use a PaaS
- EasyAuth - Ambassador pattern, Let the host do the work
- Keycloak
- Identity Server
Demo each
Comparison
Next Steps

