# Azure Role Check Prototype 

## Goal
The goal of this code hack is to show one way we can create an API that would help users understand move actions for MGs. 

## Issue
This is a common issue for MG customers where they do not know what changes will happen from a move of a MG or subscription until this is done.  This has caused issues even within our own MSFT environment where a change has happened within Service Tree and the orchestration service moves the Sub/Mg to its new parent causing outages of Policy.

## Test Scenario

In this example, a subscription is moving from a MG called "eportales" to a target MG "ContosoRootManagementGroup".  

The MGs and subscriptions are hard coded in the example but it is making api calls to get current info.  

## To Run

1. Log into Azure via terminal CLI in Tenant b94c53c6-da6f-4696-9e51-27756ae7d38a

```azurecli
az login -tenant b94c53c6-da6f-4696-9e51-27756ae7d38a
```

1. Start with a clean build (only needed after any code changes )

```csharp

dotnet clean
```

1. Run a build 

```csharp

dotnet build

```

1. Then run

```csharp
dotnet run
```

## Output will show

Section one is Role Assignment info

Fetching current role assignments at the subscription level...
Fetching inherited role assignments from current management group...
Fetching inherited role assignments from target management group...

Section two is Policy Assignments 

Fetching current policy assignments at the subscription level...
Fetching inherited policy assignments from current management group...
Fetching inherited policy assignments from target management group...

Section three will show the diff 

=== Role and Policy Changes Preview ===

Roles that will be LOST:
Roles that will be GAINED:

Policies that will be LOST:
Policies that will be GAINED:

## Future enhancements

Further changes that I would like to do is:

* Pull assignment names
* Pull assignment actions from the Definitions to show what is really changing in case of Custom Roles
* Pull more details or link to Policy Definitions
* Provide link or some way of handling an exemption.