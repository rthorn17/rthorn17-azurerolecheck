using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Authorization;
using Azure.ResourceManager.ManagementGroups;
using Azure.ResourceManager.Resources;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        string currentManagementGroupId = "eportales";
        string targetManagementGroupId = "ContosoRootManagementgroup";
        string subscriptionId = "e6b1f24d-85ce-4fe2-8a32-3e9d38ad9a05";
        string scope = $"/subscriptions/{subscriptionId}";

        var credential = new DefaultAzureCredential();
        var client = new ArmClient(credential);

        Console.WriteLine("\nFetching current role assignments at the subscription level...");
        var currentRoles = await GetRoleAssignmentsAsync(client, scope);

        Console.WriteLine("\nFetching inherited role assignments from current management group...");
        var inheritedRolesCurrent = await GetRoleAssignmentsAsync(client, $"/providers/Microsoft.Management/managementGroups/{currentManagementGroupId}");

        Console.WriteLine("\nFetching inherited role assignments from target management group...");
        var inheritedRolesTarget = await GetRoleAssignmentsAsync(client, $"/providers/Microsoft.Management/managementGroups/{targetManagementGroupId}");

        Console.WriteLine("\nFetching current policy assignments at the subscription level...");
        var currentPolicies = await GetPolicyAssignmentsAsync(client, scope);

        Console.WriteLine("\nFetching inherited policy assignments from current management group...");
        var inheritedPoliciesCurrent = await GetPolicyAssignmentsAsync(client, $"/providers/Microsoft.Management/managementGroups/{currentManagementGroupId}");

        Console.WriteLine("\nFetching inherited policy assignments from target management group...");
        var inheritedPoliciesTarget = await GetPolicyAssignmentsAsync(client, $"/providers/Microsoft.Management/managementGroups/{targetManagementGroupId}");

        var rolesLost = inheritedRolesCurrent.Except(inheritedRolesTarget).ToList();
        var rolesGained = inheritedRolesTarget.Except(inheritedRolesCurrent).ToList();
        var policiesLost = inheritedPoliciesCurrent.Except(inheritedPoliciesTarget).ToList();
        var policiesGained = inheritedPoliciesTarget.Except(inheritedPoliciesCurrent).ToList();

        Console.WriteLine("\n=== Role and Policy Changes Preview ===");
        if (rolesLost.Count > 0)
        {
            Console.WriteLine("\nRoles that will be LOST:");
            foreach (var role in rolesLost)
            {
                Console.WriteLine($"- {role}");
            }
        }
        else
        {
            Console.WriteLine("\nNo roles will be lost.");
        }

        if (rolesGained.Count > 0)
        {
            Console.WriteLine("\nRoles that will be GAINED:");
            foreach (var role in rolesGained)
            {
                Console.WriteLine($"- {role}");
            }
        }
        else
        {
            Console.WriteLine("\nNo new roles will be gained.");
        }

        if (policiesLost.Count > 0)
        {
            Console.WriteLine("\nPolicies that will be LOST:");
            foreach (var policy in policiesLost)
            {
                Console.WriteLine($"- {policy}");
            }
        }
        else
        {
            Console.WriteLine("\nNo policies will be lost.");
        }

        if (policiesGained.Count > 0)
        {
            Console.WriteLine("\nPolicies that will be GAINED:");
            foreach (var policy in policiesGained)
            {
                Console.WriteLine($"- {policy}");
            }
        }
        else
        {
            Console.WriteLine("\nNo new policies will be gained.");
        }
    }

    static async Task<List<string>> GetRoleAssignmentsAsync(ArmClient client, string scope)
    {
        var roleAssignments = new List<string>();
        try
        {
            RoleAssignmentCollection roleAssignmentsCollection;

            if (scope.StartsWith("/subscriptions/"))
            {
                var subscription = client.GetSubscriptionResource(new ResourceIdentifier(scope));
                roleAssignmentsCollection = subscription.GetRoleAssignments();
            }
            else if (scope.StartsWith("/providers/Microsoft.Management/managementGroups/"))
            {
                var managementGroup = client.GetManagementGroupResource(new ResourceIdentifier(scope));
                roleAssignmentsCollection = managementGroup.GetRoleAssignments();
            }
            else
            {
                throw new ArgumentException("Invalid scope. Must be a Subscription or Management Group.");
            }

            await foreach (var roleAssignment in roleAssignmentsCollection.GetAllAsync())
            {
                string roleInfo = $"Role Assignment: {roleAssignment.Data.RoleDefinitionId}, PrincipalId: {roleAssignment.Data.PrincipalId}, Scope: {roleAssignment.Data.Scope}";
                Console.WriteLine(roleInfo);
                roleAssignments.Add(roleInfo);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching role assignments for {scope}: {ex.Message}");
        }
        return roleAssignments;
    }

    static async Task<List<string>> GetPolicyAssignmentsAsync(ArmClient client, string scope)
    {
        var policyAssignments = new List<string>();
        try
        {
            PolicyAssignmentCollection policyAssignmentCollection;

            if (scope.StartsWith("/subscriptions/"))
            {
                var subscription = client.GetSubscriptionResource(new ResourceIdentifier(scope));
                policyAssignmentCollection = subscription.GetPolicyAssignments();
            }
            else if (scope.StartsWith("/providers/Microsoft.Management/managementGroups/"))
            {
                var managementGroup = client.GetManagementGroupResource(new ResourceIdentifier(scope));

                // ✅ Apply the required filter "atScope()"
                policyAssignmentCollection = managementGroup.GetPolicyAssignments();
            }
            else
            {
                throw new ArgumentException("Invalid scope. Must be a Subscription or Management Group.");
            }

            await foreach (var policyAssignment in policyAssignmentCollection.GetAllAsync())
            {
                string policyInfo = $"Policy Assignment: {policyAssignment.Data.DisplayName}, PolicyDefinitionId: {policyAssignment.Data.PolicyDefinitionId}, Scope: {policyAssignment.Data.Scope}";
                Console.WriteLine(policyInfo);
                policyAssignments.Add(policyInfo);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching policy assignments for {scope}: {ex.Message}");
        }
        return policyAssignments;
    }

}
