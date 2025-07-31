#!/bin/bash

# Install .NET 9 SDK locally (without sudo)

echo "Installing .NET 9 SDK locally (user installation)..."

# Download the install script
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh

# Install .NET 9 SDK to user directory
./dotnet-install.sh --channel 9.0 --install-dir $HOME/.dotnet

# Clean up
rm dotnet-install.sh

# Add to PATH for current session
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools

echo ""
echo "Installation complete!"
echo ""
echo "To make this permanent, add these lines to your ~/.bashrc or ~/.zshrc:"
echo "export DOTNET_ROOT=\$HOME/.dotnet"
echo "export PATH=\$PATH:\$DOTNET_ROOT:\$DOTNET_ROOT/tools"
echo ""
echo "For now, you can use this command to set the PATH for the current session:"
echo "export PATH=\$HOME/.dotnet:\$PATH"
echo ""
echo "Verifying installation..."
$HOME/.dotnet/dotnet --list-sdks
echo ""
echo "Current .NET version:"
$HOME/.dotnet/dotnet --version