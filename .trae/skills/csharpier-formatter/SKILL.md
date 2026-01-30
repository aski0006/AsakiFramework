---
name: "csharpier-formatter"
description: "Runs CSharpier check and format commands on the project. Invoke when user asks for code formatting, code style checking, code beautification, or mentions CSharpier."
---

# CSharpier Code Formatter

This skill automatically executes CSharpier code formatting workflow on the project.

## When to Invoke

- User asks to format code
- User requests code style checking
- User mentions code beautification
- User asks about CSharpier
- User wants to standardize code style
- Before committing code changes

## Execution Workflow

### Step 1: Check Current Directory
```powershell
Get-Location
```

### Step 2: Run CSharpier Check
```powershell
Csharpier check Assets/Asaki
```

**Purpose**: Analyze code style issues without making changes
**Output**: List of files that need formatting

### Step 3: Run CSharpier Format
```powershell
Csharpier format Assets/Asaki
```

**Purpose**: Automatically format all code files according to CSharpier standards
**Output**: Formatted files count

## Alternative Commands

If `Csharpier` command is not found, try:
```powershell
dotnet-csharpier check Assets/Asaki
dotnet-csharpier format Assets/Asaki
```

Or with full path:
```powershell
~/.dotnet/tools/csharpier check Assets/Asaki
```

## Error Handling

1. **CSharpier not installed**:
   ```powershell
   dotnet tool install -g csharpier
   ```

2. **No .cs files found**: Report that no C# files were detected

3. **Permission errors**: Run with appropriate permissions

## Example Usage

User: "Format my code"
Agent: Runs `Csharpier check Assets/Asaki` then `Csharpier format Assets/Asaki`

User: "Check code style"
Agent: Runs `Csharpier check Assets/Asaki` to analyze issues

User: "Make my code look better"
Agent: Runs the full CSharpier workflow

## Notes

- Works on all .cs files in the specified directory and subdirectories
- Respects .csharpierignore files if present
- Uses default CSharpier configuration or local .csharpierrc settings
- Target directory is `Assets/Asaki` for this project
