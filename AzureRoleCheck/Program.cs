using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Authorization;
using Azure.ResourceManager.Authorization.Mocking;
using Azure.ResourceManager.ManagementGroups;
using Azure.ResourceManager.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        
        // Get user input for Subscription ID and Management Groups
        //Console.Write("Enter the Subscription ID: ");
        //string subscriptionId = Console.ReadLine();
      
        //Console.Write("Enter Current Management Group ID: ");
        //string currentManagementGroupId = Console.ReadLine();
        string currentManagementGroupId = "eportales";

        //Console.Write("Enter Target Management Group ID: ");
        //string targetManagementGroupId = Console.ReadLine();
        string targetManagementGroupId = "ContosoRootManagementgroup";
        

        string subscriptionId = "e6b1f24d-85ce-4fe2-8a32-3e9d38ad9a05";
        string scope = $"/subscriptions/{subscriptionId}";

        // Authenticate using DefaultAzureCredential
        var credential = new DefaultAzureCredential();
        var client = new ArmClient(credential);

        // Get Subscription Resource
        SubscriptionResource subscription = await client.GetDefaultSubscriptionAsync();

        // Get Management Group Resources
        ManagementGroupResource currentManagementGroup = client.GetManagementGroupResource(ManagementGroupResource.CreateResourceIdentifier(currentManagementGroupId));
        ManagementGroupResource targetManagementGroup = client.GetManagementGroupResource(ManagementGroupResource.CreateResourceIdentifier(targetManagementGroupId));

        // Fetch Role Assignments
        Console.WriteLine("\nFetching current role assignments at the subscription level...");
        var currentRoles = await GetRoleAssignmentsAsync(client, scope);

        Console.WriteLine("\nFetching inherited role assignments from current management group...");
        var inheritedRolesCurrent = await GetRoleAssignmentsAsync(client, $"/providers/Microsoft.Management/managementGroups/{currentManagementGroupId}");

        Console.WriteLine("\nFetching inherited role assignments from target management group...");
        var inheritedRolesTarget = await GetRoleAssignmentsAsync(client, $"/providers/Microsoft.Management/managementGroups/{targetManagementGroupId}");

        // Compare Role Assignments
        var rolesLost = inheritedRolesCurrent.Except(inheritedRolesTarget).ToList();
        var rolesGained = inheritedRolesTarget.Except(inheritedRolesCurrent).ToList();

        // Display Results
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
static async Task<List<string>> GetRoleAssignmentsAsync(ArmClient client, string scope)
{
    var roleAssignments = new List<string>();

    try
    {
        RoleAssignmentCollection roleAssignmentsCollection;

        // ✅ Determine whether the scope is a Subscription or Management Group
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

        // ✅ Loop through role assignments and store RoleDefinitionId
        await foreach (RoleAssignmentResource roleAssignment in roleAssignmentsCollection.GetAllAsync())
        {
            roleAssignments.Add(roleAssignment.Data.RoleDefinitionId);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error fetching role assignments for {scope}: {ex.Message}");
    }

    return roleAssignments;
}


}
