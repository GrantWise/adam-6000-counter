#!/bin/bash

# Update Microsoft.Extensions packages to .NET 9
find src -name "*.csproj" -exec sed -i 's/Include="Microsoft\.Extensions\.[^"]*" Version="8\.0\.0"/Include="\0" Version="9.0.0"/g' {} \; -exec sed -i 's/Version="8\.0\.0"/Version="9.0.0"/g' {} \;

# Update Microsoft.Data packages
find src -name "*.csproj" -exec sed -i 's/Include="Microsoft\.Data\.[^"]*" Version="8\.0\.0"/Include="\0" Version="9.0.0"/g' {} \; -exec sed -i 's/"Microsoft\.Data\.Sqlite" Version="8\.0\.0"/"Microsoft.Data.Sqlite" Version="9.0.0"/g' {} \;

# Update Microsoft.AspNetCore packages
find src -name "*.csproj" -exec sed -i 's/Include="Microsoft\.AspNetCore\.[^"]*" Version="8\.0\.0"/Include="\0" Version="9.0.0"/g' {} \; -exec sed -i 's/"Microsoft\.AspNetCore\.Mvc\.Testing" Version="8\.0\.0"/"Microsoft.AspNetCore.Mvc.Testing" Version="9.0.0"/g' {} \;

# Update test SDK
find src -name "*.csproj" -exec sed -i 's/"Microsoft\.NET\.Test\.Sdk" Version="17\.8\.0"/"Microsoft.NET.Test.Sdk" Version="17.12.0"/g' {} \;

# Update xunit to latest
find src -name "*.csproj" -exec sed -i 's/"xunit" Version="2\.6\.[0-9]*"/"xunit" Version="2.9.2"/g' {} \;
find src -name "*.csproj" -exec sed -i 's/"xunit\.runner\.visualstudio" Version="2\.5\.[0-9]*"/"xunit.runner.visualstudio" Version="2.8.2"/g' {} \;

# Update other packages to latest compatible versions
find src -name "*.csproj" -exec sed -i 's/"FluentAssertions" Version="6\.12\.0"/"FluentAssertions" Version="7.0.0"/g' {} \;
find src -name "*.csproj" -exec sed -i 's/"Moq" Version="4\.20\.[0-9]*"/"Moq" Version="4.20.72"/g' {} \;
find src -name "*.csproj" -exec sed -i 's/"Swashbuckle\.AspNetCore" Version="6\.6\.2"/"Swashbuckle.AspNetCore" Version="7.2.0"/g' {} \;
find src -name "*.csproj" -exec sed -i 's/"Polly" Version="8\.4\.2"/"Polly" Version="8.5.0"/g' {} \;
find src -name "*.csproj" -exec sed -i 's/"InfluxDB\.Client" Version="4\.15\.0"/"InfluxDB.Client" Version="4.19.0"/g' {} \;
find src -name "*.csproj" -exec sed -i 's/"Testcontainers" Version="3\.6\.0"/"Testcontainers" Version="4.1.0"/g' {} \;
find src -name "*.csproj" -exec sed -i 's/"WireMock\.Net" Version="1\.5\.34"/"WireMock.Net" Version="1.6.8"/g' {} \;
find src -name "*.csproj" -exec sed -i 's/"coverlet\.collector" Version="6\.0\.0"/"coverlet.collector" Version="6.0.2"/g' {} \;
find src -name "*.csproj" -exec sed -i 's/"coverlet\.msbuild" Version="6\.0\.0"/"coverlet.msbuild" Version="6.0.2"/g' {} \;
find src -name "*.csproj" -exec sed -i 's/"BenchmarkDotNet" Version="0\.13\.12"/"BenchmarkDotNet" Version="0.14.0"/g' {} \;
find src -name "*.csproj" -exec sed -i 's/"BenchmarkDotNet\.Diagnostics\.Windows" Version="0\.13\.12"/"BenchmarkDotNet.Diagnostics.Windows" Version="0.14.0"/g' {} \;

echo "Package updates complete"