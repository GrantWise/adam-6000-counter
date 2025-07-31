using Industrial.Adam.Logger.Simulator.Modbus;

namespace Industrial.Adam.Logger.Simulator.Simulation;

/// <summary>
/// Main simulation engine that coordinates all channels and updates Modbus registers
/// </summary>
public class SimulationEngine : IHostedService, IDisposable
{
    private readonly Adam6051RegisterMap _registerMap;
    private readonly ProductionSimulator _productionSimulator;
    private readonly List<ChannelSimulator> _channels;
    private readonly ILogger<SimulationEngine> _logger;
    private readonly IConfiguration _configuration;

    private Timer? _updateTimer;
    private Timer? _scheduleTimer;
    private readonly object _lock = new();

    public SimulationEngine(
        Adam6051RegisterMap registerMap,
        ILogger<SimulationEngine> logger,
        ILoggerFactory loggerFactory,
        IConfiguration configuration)
    {
        _registerMap = registerMap ?? throw new ArgumentNullException(nameof(registerMap));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        // Get device ID from configuration
        var deviceId = _configuration["SimulatorSettings:DeviceId"] ?? "SIM001";

        // Create production simulator
        _productionSimulator = new ProductionSimulator(
            deviceId,
            loggerFactory.CreateLogger<ProductionSimulator>());

        // Configure production parameters from settings
        ConfigureProduction();

        // Create channel simulators
        _channels = new List<ChannelSimulator>();
        ConfigureChannels(loggerFactory);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting simulation engine");

        // Start update timer (100ms interval for smooth counter updates)
        _updateTimer = new Timer(
            UpdateSimulation,
            null,
            TimeSpan.Zero,
            TimeSpan.FromMilliseconds(100));

        // Start schedule timer (check every minute)
        _scheduleTimer = new Timer(
            CheckSchedule,
            null,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(1));

        // Start with a new job
        _productionSimulator.StartNewJob();

        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping simulation engine");

        _updateTimer?.Change(Timeout.Infinite, 0);
        _scheduleTimer?.Change(Timeout.Infinite, 0);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Get current production state
    /// </summary>
    public ProductionState GetProductionState() => _productionSimulator.CurrentState;

    /// <summary>
    /// Get production statistics
    /// </summary>
    public object GetStatistics()
    {
        lock (_lock)
        {
            return new
            {
                DeviceId = _productionSimulator.DeviceId,
                State = _productionSimulator.CurrentState.ToString(),
                TimeInState = _productionSimulator.TimeInCurrentState,
                CurrentJobSize = _productionSimulator.CurrentJobSize,
                UnitsProduced = _productionSimulator.UnitsProducedInJob,
                TotalUnits = _productionSimulator.TotalUnitsProduced,
                Channels = _channels.Select(c => new
                {
                    Channel = c.ChannelNumber,
                    Name = c.Name,
                    Type = c.Type.ToString(),
                    Counter = c.GetCounterValue(),
                    DigitalInput = c.GetDigitalInputState()
                })
            };
        }
    }

    /// <summary>
    /// Force a production stoppage
    /// </summary>
    public void ForceStoppage(ProductionState stoppageType)
    {
        if (stoppageType == ProductionState.MinorStoppage ||
            stoppageType == ProductionState.MajorStoppage)
        {
            _productionSimulator.ForceTransition(stoppageType);
        }
    }

    /// <summary>
    /// Start a new job
    /// </summary>
    public void StartNewJob()
    {
        _productionSimulator.StartNewJob();
    }

    /// <summary>
    /// Reset a specific channel counter
    /// </summary>
    public void ResetChannel(int channelNumber)
    {
        lock (_lock)
        {
            var channel = _channels.FirstOrDefault(c => c.ChannelNumber == channelNumber);
            channel?.ResetCounter();
            _registerMap.ResetCounter(channelNumber);
        }
    }

    private void UpdateSimulation(object? state)
    {
        try
        {
            lock (_lock)
            {
                // Update production simulator
                _productionSimulator.Update();

                // Update all channels
                foreach (var channel in _channels)
                {
                    channel.Update();

                    // Update Modbus registers
                    _registerMap.UpdateCounter(channel.ChannelNumber, channel.GetCounterValue());
                    _registerMap.UpdateDigitalInput(channel.ChannelNumber, channel.GetDigitalInputState());
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating simulation");
        }
    }

    private void CheckSchedule(object? state)
    {
        try
        {
            var schedule = _configuration.GetSection("Schedule");
            var now = DateTime.Now.TimeOfDay;

            // Check for scheduled breaks
            var breaks = schedule.GetSection("Breaks").GetChildren();
            foreach (var breakConfig in breaks)
            {
                var breakTime = TimeSpan.Parse(breakConfig["Time"] ?? "12:00");
                var duration = int.Parse(breakConfig["Duration"] ?? "30");

                // If we're within a minute of break time and running
                if (Math.Abs((now - breakTime).TotalMinutes) < 1 &&
                    _productionSimulator.CurrentState == ProductionState.Running)
                {
                    _logger.LogInformation("Taking scheduled break at {Time} for {Duration} minutes",
                        breakTime, duration);
                    _productionSimulator.TakeScheduledBreak(TimeSpan.FromMinutes(duration));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking schedule");
        }
    }

    private void ConfigureProduction()
    {
        var settings = _configuration.GetSection("ProductionSettings");

        _productionSimulator.BaseRate = double.Parse(settings["BaseRate"] ?? "120");
        _productionSimulator.RateVariation = double.Parse(settings["RateVariation"] ?? "0.1");
        _productionSimulator.JobSizeMin = int.Parse(settings["JobSizeMin"] ?? "1000");
        _productionSimulator.JobSizeMax = int.Parse(settings["JobSizeMax"] ?? "5000");
        _productionSimulator.SetupDuration = TimeSpan.FromMinutes(
            double.Parse(settings["SetupDurationMinutes"] ?? "15"));
        _productionSimulator.MinorStoppageProbability = double.Parse(
            settings["MinorStoppageProbability"] ?? "0.02");
        _productionSimulator.MajorStoppageProbability = double.Parse(
            settings["MajorStoppageProbability"] ?? "0.005");
    }

    private void ConfigureChannels(ILoggerFactory loggerFactory)
    {
        var channelsConfig = _configuration.GetSection("Channels").GetChildren();

        foreach (var channelConfig in channelsConfig)
        {
            var number = int.Parse(channelConfig["Number"] ?? "0");
            var name = channelConfig["Name"] ?? $"Channel {number}";
            var typeStr = channelConfig["Type"] ?? "Disabled";
            var enabled = bool.Parse(channelConfig["Enabled"] ?? "false");

            if (!Enum.TryParse<ChannelType>(typeStr, out var type))
            {
                type = ChannelType.Disabled;
            }

            var channel = new ChannelSimulator(
                number,
                name,
                type,
                loggerFactory.CreateLogger<ChannelSimulator>(),
                type == ChannelType.ProductionCounter ? _productionSimulator : null)
            {
                Enabled = enabled
            };

            // Configure reject rate if it's a reject counter
            if (type == ChannelType.RejectCounter)
            {
                channel.RejectRate = double.Parse(channelConfig["RejectRate"] ?? "0.05");
            }

            _channels.Add(channel);
        }

        _logger.LogInformation("Configured {Count} channels", _channels.Count);
    }

    public void Dispose()
    {
        _updateTimer?.Dispose();
        _scheduleTimer?.Dispose();
    }
}
