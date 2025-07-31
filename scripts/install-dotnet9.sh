#!/bin/bash

# Install .NET 9 SDK on Debian 12

echo "Installing .NET 9 SDK on Debian 12..."

# Download Microsoft package configuration
wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb

# Install the package
sudo dpkg -i packages-microsoft-prod.deb

# Clean up
rm packages-microsoft-prod.deb

# Update package list
sudo apt-get update

# Install .NET 9 SDK
sudo apt-get install -y dotnet-sdk-9.0

# Verify installation
echo ""
echo "Verifying installation..."
dotnet --list-sdks
echo ""
echo "Current .NET version:"
dotnet --version