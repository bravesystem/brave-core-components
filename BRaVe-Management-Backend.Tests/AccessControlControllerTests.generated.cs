using BRaVe_Management_Backend.Controllers;
using BRaVe_Management_Backend.Interfaces;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Xunit;

namespace BRaVe_Management_Backend.Tests;

/// <summary>Generated unit tests for BRaVe_Management_Backend.Controllers.AccessControlController.</summary>
public class AccessControlControllerTests
{
    [Fact]
    public async Task GetUserRolesAndMissions_ShouldSucceed()
    {
        // TODO: Arrange - create instance and mocks

        // TODO: Act - call the method

        // TODO: Assert - verify result
        Assert.True(true);
    }

    [Fact]
    public async Task GetUserRoles_Should_Return_NotFound_When_No_Roles_Found()
    {
        //Arrage
        string userid = Guid.NewGuid().ToString();
        string userDetails = "anyuser@iom.int";
        string userProfile = "Any User";
        CancellationToken ct = default;

        //build a ClaimsPrincipal with the claim your app expects
        var identity = new ClaimsIdentity(new[]
        {
                new Claim("oid", userid),
                new Claim("name", userProfile),
                new Claim("email", userDetails)

            }, "TestAuth");

        var principal = new ClaimsPrincipal(identity);


        var dataStore = A.Fake<IRoleService>();
        A.CallTo(() => dataStore.GetRolesAsync(userid, userDetails, userProfile, "en", ct)).Returns(Task.FromResult(new DTOs.TenantAndRolesDto
        {
        }));

        var controller = new AccessControlController(dataStore);


        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };

        //Act
        var result = await controller.GetUserRoles(userid, "en", ct);

        //Assert

        var nf = Assert.IsType<NotFoundObjectResult>(result.Result);

        Assert.Equal(StatusCodes.Status404NotFound, nf.StatusCode);

    }

    [Fact]
    public async Task GetUserRoles_Should_Return_Badrequest_When_No_Tenant_But_Multiple_Roles_Found_And_Excl_RoleId_1()
    {
        //Arrage
        string userid = Guid.NewGuid().ToString();
        string userDetails = "anyuser@iom.int";
        string userProfile = "Any User";

        CancellationToken ct = default;

        // Arrange: build a ClaimsPrincipal with the claim your app expects
        var identity = new ClaimsIdentity(new[]
        {
                new Claim("oid", userid),
                new Claim("name", userProfile),
                new Claim("email", userDetails)

            }, "TestAuth");

        var principal = new ClaimsPrincipal(identity);


        var dataStore = A.Fake<IRoleService>();
        A.CallTo(() => dataStore.GetRolesAsync(userid, userDetails, userProfile, "en", ct)).Returns(Task.FromResult(new DTOs.TenantAndRolesDto
        {
            TenantId = 0,
            Roles = [2, 3]
        }));

        var controller = new AccessControlController(dataStore);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };

        //Act
        var result = await controller.GetUserRoles(userid, "en", ct);

        //Assert

        var nf = Assert.IsType<BadRequestObjectResult>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, nf.StatusCode);

    }

    [Fact]
    public async Task GetUserRoles_Should_Return_Status200OK_When_No_Tenant_But_Multiple_Roles_Found_Incl_RoleId_1()
    {
        //Arrage
        string userid = Guid.NewGuid().ToString();
        string userDetails = "anyuser@iom.int";
        string userProfile = "Any User";

        CancellationToken ct = default;

        // Arrange: build a ClaimsPrincipal with the claim your app expects
        var identity = new ClaimsIdentity(new[]
        {
                new Claim("oid", userid),
                new Claim("name", userProfile),
                new Claim("email", userDetails)

            }, "TestAuth");

        var principal = new ClaimsPrincipal(identity);


        var dataStore = A.Fake<IRoleService>();
        A.CallTo(() => dataStore.GetRolesAsync(userid, userDetails, userProfile, "en", ct)).Returns(Task.FromResult(new DTOs.TenantAndRolesDto
        {
            TenantId = 0,
            Roles = [1, 2, 3]
        }));

        var controller = new AccessControlController(dataStore);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };

        //Act
        var result = await controller.GetUserRoles(userid, "en", ct);

        //Assert

        var nf = Assert.IsType<OkObjectResult>(result.Result);

        Assert.Equal(StatusCodes.Status200OK, nf.StatusCode);

    }

    [Fact]
    public void GetUserRoles_ShouldSucceed()
    {
        // TODO: Arrange - create instance and mocks
        // TODO: Act - call the method
        // TODO: Assert - verify result
        Assert.True(true);
    }


    [Fact]
    public void AssignUserToTenant_ShouldSucceed()
    {
        // TODO: Arrange - create instance and mocks
        // TODO: Act - call the method
        // TODO: Assert - verify result
        Assert.True(true);
    }


    [Fact]
    public void AssignUserRoles_ShouldSucceed()
    {
        // TODO: Arrange - create instance and mocks
        // TODO: Act - call the method
        // TODO: Assert - verify result
        Assert.True(true);
    }


    [Fact]
    public void RemoveUserRoles_ShouldSucceed()
    {
        // TODO: Arrange - create instance and mocks
        // TODO: Act - call the method
        // TODO: Assert - verify result
        Assert.True(true);
    }


    [Fact]
    public void GetTemporaryAccess_ShouldSucceed()
    {
        // TODO: Arrange - create instance and mocks
        // TODO: Act - call the method
        // TODO: Assert - verify result
        Assert.True(true);
    }


    [Fact]
    public void AddTempAccess_ShouldSucceed()
    {
        // TODO: Arrange - create instance and mocks
        // TODO: Act - call the method
        // TODO: Assert - verify result
        Assert.True(true);
    }


    [Fact]
    public void GetEffectiveUserRoles_ShouldSucceed()
    {
        // TODO: Arrange - create instance and mocks
        // TODO: Act - call the method
        // TODO: Assert - verify result
        Assert.True(true);
    }


    [Fact]
    public void UpdateLogoutTime_ShouldSucceed()
    {
        // TODO: Arrange - create instance and mocks
        // TODO: Act - call the method
        // TODO: Assert - verify result
        Assert.True(true);
    }


    [Fact]
    public void GetSystemUsers_ShouldSucceed()
    {
        // TODO: Arrange - create instance and mocks
        // TODO: Act - call the method
        // TODO: Assert - verify result
        Assert.True(true);
    }


}
