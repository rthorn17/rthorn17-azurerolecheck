using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Authorization;
using Azure.ResourceManager.ManagementGroups;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Authorization.Models;

class Program
{
    static async Task Main(string[] args)
    {
        Console.Write("Enter the Subscription ID: ");
        string subscriptionId = Console.ReadLine();

        Console.Write("Enter Current Management Group ID: ");
        string currentManagementGroupId = Console.ReadLine();

        Console.Write("Enter Target Management Group ID: ");
        string targetManagementGroupId = Console.ReadLine();

        // ✅ Authenticate using DefaultAzureCredential
        var credential = new DefaultAzureCredential();
        var client = new ArmClient(credential);

        // ✅ Get Subscription Resource
        SubscriptionResource subscription = client.GetSubscriptionResource(new Azure.Core.ResourceIdentifier($"/subscriptions/{subscriptionId}"));

        // ✅ Get Management Group Resources
        ManagementGroupResource currentManagementGroup = client.GetManagementGroupResource(ManagementGroupResource.CreateResourceIdentifier(currentManagementGroupId));
        ManagementGroupResource targetManagementGroup = client.GetManagementGroupResource(ManagementGroupResource.CreateResourceIdentifier(targetManagementGroupId));

        // ✅ Fetch Role Assignments
        Console.WriteLine("\nFetching current role assignments at the subscription level...");
        var currentRoles = await GetRoleAssignmentsAsync(credential, $"/subscriptions/{subscriptionId}");

        Console.WriteLine("\nFetching inherited role assignments from current management group...");
        var inheritedRolesCurrent = await GetRoleAssignmentsAsync(credential, $"/providers/Microsoft.Management/managementGroups/{currentManagementGroupId}");

        Console.WriteLine("\nFetching inherited role assignments from target management group...");
        var inheritedRolesTarget = await GetRoleAssignmentsAsync(credential, $"/providers/Microsoft.Management/managementGroups/{targetManagementGroupId}");

        // ✅ Compare Role Assignments
        var rolesLost = inheritedRolesCurrent.Except(inheritedRolesTarget).ToList();
        var rolesGained = inheritedRolesTarget.Except(inheritedRolesCurrent).ToList();

        // ✅ Display Results
        Console.WriteLine("\n=== Role Changes Preview ===");
        
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
    }

    /// <summary>
    /// Retrieves role assignments for a given Azure resource scope (Subscription or Management Group).
    /// </summary>
    static async Task<List<string>> GetRoleAssignmentsAsync(DefaultAzureCredential credential, string scope)
    {
        var roleAssignments = new List<string>();

        try
        {
            // ✅ Use AuthorizationManagementClient
            var authClient = new AuthorizationManagementClient(new Uri("https://management.azure.com/"), credential);

            // Fetch role assignments for the scope
            await foreach (var roleAssignment in authClient.RoleAssignments.ListAsync(scope))
            {
                roleAssignments.Add(roleAssignment.RoleDefinitionId);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching role assignments for {scope}: {ex.Message}");
        }

        return roleAssignments;
    }
}
