#!/bin/bash
# Industrial ADAM Logger - Developer Environment Setup
# Sets up quality tools and pre-commit hooks for new developers

set -e

echo "🏭 Industrial ADAM Logger - Developer Environment Setup"
echo "======================================================="

echo "📦 Installing development tools..."

# Restore .NET tools
echo "  - Restoring .NET tools..."
dotnet tool restore

# Install ReportGenerator globally for coverage reports (optional)
echo "  - Installing ReportGenerator for coverage visualization..."
dotnet tool install -g dotnet-reportgenerator-globaltool 2>/dev/null || echo "    (Already installed)"

# Setup git hooks
echo "🔧 Setting up git hooks..."
dotnet husky install

# Verify git hooks are working
echo "🧪 Testing pre-commit hooks..."
if [ -f ".husky/pre-commit" ]; then
    echo "  ✅ Pre-commit hooks installed successfully"
else
    echo "  ❌ Pre-commit hooks not found"
    exit 1
fi

# Run initial format to establish baseline
echo "🎨 Running initial code formatting..."
dotnet format

# Test build
echo "🔨 Testing build..."
dotnet build --configuration Release --verbosity quiet

echo ""
echo "✅ Development environment setup complete!"
echo ""
echo "📋 Quality Tools Available:"
echo "  - Code formatting: dotnet format"
echo "  - Coverage reports: ./scripts/run-coverage.sh"
echo "  - Pre-commit hooks: Automatic on git commit"
echo ""
echo "🔍 EditorConfig Standards:"
echo "  - 4-space indentation"
echo "  - CRLF line endings (Windows)"
echo "  - Consistent C# formatting"
echo "  - Industrial naming conventions"
echo ""
echo "🚨 Pre-commit Quality Gates:"
echo "  - Code formatting verification"
echo "  - Build success verification"
echo ""
echo "💡 Tips:"
echo "  - Use 'dotnet format' before committing"
echo "  - Run coverage with './scripts/run-coverage-simple.sh'"
echo "  - Check .editorconfig is applied in your IDE"
echo ""