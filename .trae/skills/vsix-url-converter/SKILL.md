---
name: "vsix-url-converter"
description: "Converts VS Code marketplace URLs to VSIX download URLs. Invoke when user provides a marketplace URL and asks to convert it to a downloadable VSIX package URL."
---

# VSIX URL Converter

This skill converts Visual Studio Code extension marketplace URLs to direct VSIX download URLs.

## When to Invoke

- User provides a marketplace URL like `https://marketplace.visualstudio.com/items?itemName=publisher.extension`
- User asks to convert the URL to a VSIX download link
- User wants to download an extension as a .vsix file for offline installation
- User says the exact trigger phrase: "将商店地址和插件版本转为为VSIX下载地址"

## Conversion Process

### Step 1: Extract Information from URL

Given a marketplace URL:
```
https://marketplace.visualstudio.com/items?itemName=PUBLISHER.EXTENSION_NAME
```

Extract:
- **fieldA** (Publisher): The part before the dot (`.`) in itemName
- **fieldB** (Extension): The part after the dot (`.`) in itemName
- **version**: The version number provided by the user

### Step 2: Validate Parameters

Verify:
1. URL contains `itemName` parameter
2. itemName format is `publisher.extension` (contains exactly one dot)
3. Version number is in valid format (e.g., `1.2.3`)

### Step 3: Build Download URL

Use this template:
```
https://marketplace.visualstudio.com/_apis/public/gallery/publishers/${fieldA}/vsextensions/${fieldB}/${version}/vspackage
```

### Step 4: Return Result

Provide:
1. The generated download URL
2. Installation instructions
3. Example usage

## Examples

### Example 1: Unity Debugger
- **Store URL**: `https://marketplace.visualstudio.com/items?itemName=Unity.unity-debug`
- **Version**: `3.0.2`
- **Extracted fieldA**: `Unity`
- **Extracted fieldB**: `unity-debug`
- **Generated URL**: `https://marketplace.visualstudio.com/_apis/public/gallery/publishers/Unity/vsextensions/unity-debug/3.0.2/vspackage`

### Example 2: C# Dev Kit
- **Store URL**: `https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit`
- **Version**: `1.14.14`
- **Extracted fieldA**: `ms-dotnettools`
- **Extracted fieldB**: `csdevkit`
- **Generated URL**: `https://marketplace.visualstudio.com/_apis/public/gallery/publishers/ms-dotnettools/vsextensions/csdevkit/1.14.14/vspackage`

### Example 3: VSTUC
- **Store URL**: `https://marketplace.visualstudio.com/items?itemName=VisualStudioToolsForUnity.vstuc`
- **Version**: `1.2.0`
- **Extracted fieldA**: `VisualStudioToolsForUnity`
- **Extracted fieldB**: `vstuc`
- **Generated URL**: `https://marketplace.visualstudio.com/_apis/public/gallery/publishers/VisualStudioToolsForUnity/vsextensions/vstuc/1.2.0/vspackage`

## Installation Instructions

After generating the download URL:

1. **Copy the URL** and paste it into a web browser
2. **Press Enter** to start downloading the `.vsix` file
3. **In VS Code/Trae IDE**:
   - Press `Ctrl+Shift+X` to open the Extensions panel
   - Drag and drop the downloaded `.vsix` file into the panel
   - Wait for the installation to complete
4. **Verify** the extension appears in the "Installed" list

## Error Handling

### Common Issues

1. **Invalid URL format**:
   - URL must contain `itemName` parameter
   - Example: `https://marketplace.visualstudio.com/items?itemName=publisher.extension`

2. **Invalid itemName format**:
   - Must contain exactly one dot (`.`) separating publisher and extension
   - Example: `publisher.extension` (correct)
   - Example: `publisher.extension.name` (incorrect)

3. **Invalid version format**:
   - Must be in semantic versioning format (e.g., `1.2.3`)
   - Check the extension's Version History page for valid versions

### Troubleshooting

- If the download fails, check if the version number is correct
- For private or paid extensions, ensure you have the necessary permissions
- Some extensions may require specific VS Code versions

## Usage Notes

- This skill works for all VS Code extensions on the marketplace
- The generated URL provides a direct download of the `.vsix` file
- For offline installation, download the `.vsix` file and transfer it to the target machine
- Always use the exact version number from the extension's Version History page