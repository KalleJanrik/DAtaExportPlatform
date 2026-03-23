using DataExportPlatform.Core.Interfaces;
using DataExportPlatform.Core.Models;
using DataExportPlatform.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DataExportPlatform.Tests.Controllers;

public class PipelineControllerTests
{
    // Minimal stub — only AppId matters for validation tests
    private sealed class StubJob(string appId) : IExportJob
    {
        public string AppId => appId;
        public Task<IEnumerable<ExportResult>> RunAsync(DataContext ctx, CancellationToken ct)
            => Task.FromResult(Enumerable.Empty<ExportResult>());
    }

    private static PipelineController BuildController(params string[] appIds)
    {
        var jobs = appIds.Select(id => (IExportJob)new StubJob(id));
        // null! for orchestrator: validation 400-paths return before calling it
        return new PipelineController(null!, NullLogger<PipelineController>.Instance, jobs);
    }

    [Fact]
    public async Task Trigger_NullJobs_Returns400()
    {
        var controller = BuildController("AppA", "AppB");
        var result = await controller.Trigger(new TriggerRequest(null), CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Trigger_EmptyJobs_Returns400()
    {
        var controller = BuildController("AppA", "AppB");
        var result = await controller.Trigger(new TriggerRequest([]), CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Trigger_UnknownJobId_Returns400WithMessage()
    {
        var controller = BuildController("AppA", "AppB");
        var result = await controller.Trigger(new TriggerRequest(["AppA", "NonExistent"]), CancellationToken.None);
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("NonExistent", bad.Value!.ToString());
    }

    [Fact]
    public void GetJobs_ReturnsAllRegisteredJobIds()
    {
        var controller = BuildController("AppA", "AppB", "AppC");
        var result = Assert.IsType<OkObjectResult>(controller.GetJobs());
        var json = System.Text.Json.JsonSerializer.Serialize(result.Value);
        Assert.Contains("AppA", json);
        Assert.Contains("AppB", json);
        Assert.Contains("AppC", json);
    }
}
