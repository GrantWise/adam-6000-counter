using Microsoft.AspNetCore.Mvc;
using Industrial.Adam.Logger.Simulator.Simulation;
using Industrial.Adam.Logger.Simulator.Storage;

namespace Industrial.Adam.Logger.Simulator.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SimulatorController : ControllerBase
{
    private readonly SimulationEngine _simulationEngine;
    private readonly SimulatorDatabase _database;
    private readonly ILogger<SimulatorController> _logger;
    
    public SimulatorController(
        SimulationEngine simulationEngine,
        SimulatorDatabase database,
        ILogger<SimulatorController> logger)
    {
        _simulationEngine = simulationEngine ?? throw new ArgumentNullException(nameof(simulationEngine));
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    /// <summary>
    /// Get current simulator status
    /// </summary>
    [HttpGet("status")]
    public ActionResult<object> GetStatus()
    {
        return Ok(_simulationEngine.GetStatistics());
    }
    
    /// <summary>
    /// Reset a specific channel counter
    /// </summary>
    [HttpPost("channels/{channel}/reset")]
    public ActionResult ResetCounter(int channel)
    {
        if (channel < 0 || channel >= 16)
        {
            return BadRequest("Invalid channel number");
        }
        
        _simulationEngine.ResetChannel(channel);
        _logger.LogInformation("Channel {Channel} counter reset", channel);
        
        return Ok(new { message = $"Channel {channel} counter reset" });
    }
    
    /// <summary>
    /// Force a production stoppage
    /// </summary>
    [HttpPost("production/force-stoppage")]
    public async Task<ActionResult> ForceStoppage([FromBody] StoppageRequest request)
    {
        if (request.Type != "minor" && request.Type != "major")
        {
            return BadRequest("Stoppage type must be 'minor' or 'major'");
        }
        
        var stoppageType = request.Type == "minor" 
            ? ProductionState.MinorStoppage 
            : ProductionState.MajorStoppage;
        
        _simulationEngine.ForceStoppage(stoppageType);
        
        // Record event
        await _database.RecordEventAsync(
            "SIM001", // TODO: Get from config
            null,
            $"Forced {request.Type} stoppage",
            null,
            request.Reason);
        
        _logger.LogInformation("Forced {Type} stoppage: {Reason}", request.Type, request.Reason);
        
        return Ok(new { message = $"Forced {request.Type} stoppage" });
    }
    
    /// <summary>
    /// Start a new production job
    /// </summary>
    [HttpPost("production/start-job")]
    public async Task<ActionResult> StartNewJob([FromBody] JobRequest? request = null)
    {
        _simulationEngine.StartNewJob();
        
        // Record event
        await _database.RecordEventAsync(
            "SIM001",
            null,
            "New job started",
            null,
            request?.JobName);
        
        _logger.LogInformation("Started new job: {JobName}", request?.JobName ?? "unnamed");
        
        return Ok(new { message = "New job started" });
    }
    
    /// <summary>
    /// Get production history
    /// </summary>
    [HttpGet("history")]
    public async Task<ActionResult<List<ProductionEvent>>> GetHistory([FromQuery] int hours = 24)
    {
        var events = await _database.GetRecentEventsAsync("SIM001", hours);
        return Ok(events);
    }
    
    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public ActionResult<object> HealthCheck()
    {
        return Ok(new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            ProductionState = _simulationEngine.GetProductionState().ToString()
        });
    }
}

public class StoppageRequest
{
    public string Type { get; set; } = "minor";
    public string? Reason { get; set; }
}

public class JobRequest
{
    public string? JobName { get; set; }
    public int? TargetQuantity { get; set; }
}