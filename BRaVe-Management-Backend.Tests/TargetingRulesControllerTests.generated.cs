using BRaVe_Management_Backend.Controller;
using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Interfaces;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;
using System.Text.Json;
using Xunit;

namespace BRaVe_Management_Backend.Tests;

/// <summary>Generated unit tests for BRaVe_Management_Backend.Controller.TargetingRulesController.</summary>
public class TargetingRulesControllerTests
{
    private static TargetingRulesController CreateController(
       ITargetingRulesService targetingService,
       int tenantId,
       string userId = "test-user")
    {
        var controller = new TargetingRulesController(
            targetingService,
            NullLogger<TargetingRulesController>.Instance);

        var claims = new List<Claim>
        {
            new("TenantId", tenantId.ToString()),
            new(ClaimTypes.NameIdentifier, userId),
        };

        var identity = new ClaimsIdentity(claims, authenticationType: "Test");

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) },
        };

        return controller;
    }


    [Fact]
    public async Task Create_ShouldSucceed()
    {
        var dto = new TargetingRuleDto
        {
            RuleName = "Test rule",
            TenantId = 7,
            WeightBound = false,
            Code = "CODE-1",
            CreatedBy = "tester",
        };
        const int expectedRuleId = 42;

        var targetingService = A.Fake<ITargetingRulesService>();
        A.CallTo(() => targetingService.CreateAsync(A<TargetingRuleDto>._))
            .Returns(Task.FromResult(expectedRuleId));

        var controller = new TargetingRulesController(
            targetingService,
            NullLogger<TargetingRulesController>.Instance);

        var result = await controller.Create(dto);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedRuleId, ok.Value);
        A.CallTo(() => targetingService.CreateAsync(dto)).MustHaveHappenedOnceExactly();
    }


    [Fact]
    public async Task Update_ShouldSucceed()
    {
        const int ruleId = 99;
        var dto = new TargetingRuleDto
        {
            Id = ruleId,
            RuleName = "Updated rule",
            TenantId = 3,
            WeightBound = true,
            Code = "CODE-U",
            CreatedBy = "tester",
        };

        var targetingService = A.Fake<ITargetingRulesService>();
        A.CallTo(() => targetingService.UpdateAsync(ruleId, A<TargetingRuleDto>._))
            .Returns(Task.CompletedTask);

        var controller = new TargetingRulesController(
            targetingService,
            NullLogger<TargetingRulesController>.Instance);

        var result = await controller.Update(ruleId, dto);

        Assert.IsType<NoContentResult>(result);
        A.CallTo(() => targetingService.UpdateAsync(ruleId, dto)).MustHaveHappenedOnceExactly();
    }


    [Fact]
    public async Task GetAll_ShouldSucceed()
    {
        const int tenantId = 5;
        var rules = new List<TargetingRuleDto>
        {
            new()
            {
                Id = 1,
                RuleName = "Rule A",
                TenantId = tenantId,
                Code = "A",
                CreatedBy = "u1",
            },
        };

        var targetingService = A.Fake<ITargetingRulesService>();
        A.CallTo(() => targetingService.GetAllAsync(tenantId))
            .Returns(Task.FromResult<IEnumerable<TargetingRuleDto>>(rules));

        var controller = CreateController(targetingService, tenantId);

        var result = await controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(rules, ok.Value);
        A.CallTo(() => targetingService.GetAllAsync(tenantId)).MustHaveHappenedOnceExactly();
    }


    [Fact]
    public async Task GetById_ShouldSucceed()
    {
        const int ruleId = 7;
        var rule = new TargetingRuleDto
        {
            Id = ruleId,
            RuleName = "Found",
            TenantId = 1,
            Code = "X",
            CreatedBy = "u",
        };

        var targetingService = A.Fake<ITargetingRulesService>();
        A.CallTo(() => targetingService.GetByIdAsync(ruleId))
            .Returns(Task.FromResult<TargetingRuleDto?>(rule));

        var controller = new TargetingRulesController(
            targetingService,
            NullLogger<TargetingRulesController>.Instance);

        var result = await controller.GetById(ruleId);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(rule, ok.Value);
        A.CallTo(() => targetingService.GetByIdAsync(ruleId)).MustHaveHappenedOnceExactly();
    }


    [Fact]
    public async Task Delete_ShouldSucceed()
    {
        const int ruleId = 11;

        var targetingService = A.Fake<ITargetingRulesService>();
        A.CallTo(() => targetingService.DeleteAsync(ruleId))
            .Returns(Task.CompletedTask);

        var controller = new TargetingRulesController(
            targetingService,
            NullLogger<TargetingRulesController>.Instance);

        var result = await controller.Delete(ruleId);

        Assert.IsType<NoContentResult>(result);
        A.CallTo(() => targetingService.DeleteAsync(ruleId)).MustHaveHappenedOnceExactly();
    }


    [Fact]
    public async Task GetTargetingFields_ShouldSucceed()
    {
        const int tenantId = 8;
        var fields = new List<TargetingFieldsDto>
        {
            new()
            {
                FieldCode = "REG",
                DisplayName = "Region",
                DataType = 1,
                TenantId = tenantId,
            },
            new()
            {
                FieldCode = "AGE",
                DisplayName = "Age",
                DataType = 2,
                TenantId = tenantId,
            },
        };

        var targetingService = A.Fake<ITargetingRulesService>();
        A.CallTo(() => targetingService.GetAllTargetingFieldsAsync(tenantId))
            .Returns(Task.FromResult(fields));

        var controller = CreateController(targetingService, tenantId);

        var result = await controller.GetTargetingFields();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(fields, ok.Value);
        A.CallTo(() => targetingService.GetAllTargetingFieldsAsync(tenantId)).MustHaveHappenedOnceExactly();
    }


    [Fact]
    public async Task GetFieldData_ShouldSucceed()
    {
        const string displayName = "Region";
        var values = new List<string> { "North", "South" };

        var targetingService = A.Fake<ITargetingRulesService>();
        A.CallTo(() => targetingService.GetDistinctValuesForFieldAsync(displayName))
            .Returns(Task.FromResult<IEnumerable<string>>(values));

        var controller = new TargetingRulesController(
            targetingService,
            NullLogger<TargetingRulesController>.Instance);

        var result = await controller.GetFieldData(displayName);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(values, ok.Value);
        A.CallTo(() => targetingService.GetDistinctValuesForFieldAsync(displayName)).MustHaveHappenedOnceExactly();
    }


    [Fact]
    public async Task Preview_ShouldSucceed()
    {
        using var doc = JsonDocument.Parse("""{"criteria":{"op":"and"}}""");
        var ruleJson = doc.RootElement;
        var previewRows = new List<TargetingPreviewDto>
        {
            new()
            {
                FirstName = "Ada",
                LastName = "Lovelace",
                AgeInYears = 36,
                Relationship = "Head",
                Gender = "F",
            },
        };

        var targetingService = A.Fake<ITargetingRulesService>();
        A.CallTo(() => targetingService.PreviewAsync(A<JsonElement>._))
            .Returns(Task.FromResult<IEnumerable<TargetingPreviewDto>>(previewRows));

        var controller = new TargetingRulesController(
            targetingService,
            NullLogger<TargetingRulesController>.Instance);

        var result = await controller.Preview(ruleJson);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(previewRows, ok.Value);
        A.CallTo(() => targetingService.PreviewAsync(A<JsonElement>._)).MustHaveHappenedOnceExactly();
    }



    [Fact]
    public async Task EnrollBeneficiaries_ShouldSucceed()
    {
        const int tenantId = 4;
        const string userId = "user-42";
        var dto = new EnrollBeneficiariesDto
        {
            DistributionId = 100,
            TargetingId = 200,
            HouseholdIds = new List<string> { "hh-1", "hh-2" },
        };

        var targetingService = A.Fake<ITargetingRulesService>();
        A.CallTo(() => targetingService.EnrollBeneficiariesAsync(A<EnrollBeneficiariesDto>._))
            .Returns(Task.CompletedTask);

        var controller = CreateController(targetingService, tenantId, userId);

        var result = await controller.EnrollBeneficiaries(dto);

        Assert.IsType<OkResult>(result);
        Assert.Equal(tenantId, dto.TenantId);
        Assert.Equal(userId, dto.CreatedBy);
        Assert.Equal(2, dto.HouseholdIds.Count);
        A.CallTo(() => targetingService.EnrollBeneficiariesAsync(dto)).MustHaveHappenedOnceExactly();
    }


}
