# GitHub Copilot Instructions for Mate Engine (PC-Partner)

## Project Overview

**Mate Engine** is a free, open-source desktop companion application built with Unity. It serves as an alternative to Desktop Mate, featuring custom VRM avatar support, modding capabilities, and desktop interaction features. The application allows users to have interactive 3D avatars on their desktop with features like dancing, window sitting, AI chat, and extensive customization.

## Technology Stack

- **Engine**: Unity 6000.2.6f2
- **Primary Language**: C#
- **Key Dependencies**:
  - VRM 1.0 (VRM avatar format support)
  - Steamworks.NET (Steam integration)
  - QWEN 2.5 1.5b LLM (AI chat features)
  - System Tray integration
  - Custom shaders and post-processing effects

## Project Structure

- `Assets/Scripts/` - Main C# scripts for application logic
- `Assets/MATE ENGINE - System Tray/` - System tray integration code
- `Assets/MATE ENGINE - Packages/` - Custom packages including VRM support
- `ProjectSettings/` - Unity project configuration
- `Scenes - USED FOR MATE ENGINE > Mate Engine Main` - Main development scene

## Coding Standards

### General Guidelines

1. **Code Style**:
   - Follow existing C# naming conventions (PascalCase for classes/methods, camelCase for private fields)
   - Keep code clean, consistent, and well-commented where necessary
   - Follow Unity C# coding conventions

2. **Comments**:
   - Add comments for complex logic and non-obvious code
   - Document public APIs and methods
   - Use XML documentation comments for public classes and methods

3. **File Organization**:
   - Keep related functionality in appropriate folders within `Assets/`
   - Follow the existing folder structure for new features

### Unity-Specific Conventions

1. **MonoBehaviour Scripts**:
   - Use proper Unity lifecycle methods (Awake, Start, Update, OnDestroy, etc.)
   - Cache component references in Awake/Start when possible
   - Clean up resources in OnDestroy

2. **Performance**:
   - Avoid expensive operations in Update()
   - Use object pooling for frequently instantiated objects
   - Cache GetComponent calls
   - Be mindful of garbage collection

3. **Serialization**:
   - Use `[SerializeField]` for private fields that need Inspector access
   - Use `[HideInInspector]` appropriately

## Development Workflow

### Setting Up Development Environment

1. Clone the repository and extract the folder
2. Open Unity Hub → Add Project From Disk
3. Select the `PC-Partner` folder
4. Load the project and open the main scene:
   `Scenes - USED FOR MATE ENGINE > Mate Engine Main`

### Making Changes

1. **Fork and Branch**: Fork the repository and create a new branch for your changes
2. **Test Changes**: Test your changes in the Unity Editor before committing
3. **Follow Conventions**: Ensure code follows existing formatting and naming conventions
4. **Document**: Add comments and update documentation as needed
5. **Pull Request**: Open a PR with a clear description of changes

## Testing

- Test changes within the Unity Editor before creating builds
- Verify features work in standalone builds when appropriate
- Test VRM avatar loading with different models
- Check performance impact of changes (FPS, RAM usage)
- Test system tray integration on Windows

## Architecture Notes

### Key Components

1. **VRM Integration**: Custom VRM 1.0 support for loading and displaying 3D avatars
2. **System Tray**: Native Windows system tray integration
3. **AI Chat**: Integration with QWEN 2.5 LLM for conversational features
4. **Animation System**: Custom animation blending and transitions
5. **Desktop Interaction**: Window sitting, taskbar sitting, dragging mechanics
6. **Steam Integration**: Workshop support and Steam features

### Important Considerations

1. **VRM Models**: Ensure compatibility with VRM 1.0 specification
2. **Windows-Specific**: Many features are Windows-specific (system tray, window detection)
3. **Performance**: Application should remain lightweight (target ~200MB RAM)
4. **Modding Support**: Changes should maintain mod compatibility where possible

## Licensing

- **Application**: Mixed - GNU AGPL v3 & MateProv2 License
- **Default Avatar**: All Rights Reserved by Yorshka Shop (do not redistribute)
- **QWEN 2.5 LLM**: Apache License Version 2.0

**Important**: Do not include proprietary assets in distributions. Users must provide their own VRM models.

## Common Tasks

### Adding New Features

1. Create scripts in appropriate `Assets/Scripts/` subdirectory
2. Follow Unity component-based architecture
3. Update UI/menus if adding user-facing features
4. Test with multiple VRM models for compatibility

### Working with VRM Models

- VRM models must be properly formatted and exported
- Check bone structure and shader compatibility
- Reference: [VRM Specification](https://vrm.dev/)

### Modding Support

- Maintain extensibility for custom mods
- Document mod interfaces and hooks
- Keep mod loading systems accessible

## Security Considerations

1. **User Data**: Handle user preferences and settings securely
2. **File Loading**: Validate VRM and other file formats before loading
3. **AI Integration**: Ensure AI API calls are properly secured
4. **Steam Integration**: Follow Steamworks best practices

## Multilingual Support

The project supports multiple languages (English, Japanese, Chinese). When adding user-facing text:
- Add translations for all supported languages
- Use localization keys/systems where available
- Test UI with different language lengths

## Build and Deployment

- Builds are created through Unity's build system
- Target platform: Windows Desktop
- Include necessary DLLs (Steamworks, etc.)
- Executable name: `MateEngineX.exe`

## Questions and Support

- For questions, open an issue in the repository
- Check existing issues before creating new ones
- Provide clear reproduction steps for bugs
- Include Unity version and system specs for technical issues

## Additional Notes

- Avoid scenes like "Mate Engine InDev" unless working on dev branch
- The application is designed to be anti-cheat safe and game-compatible
- Performance is a priority - maintain lightweight resource usage
- The Steam version includes additional exclusive content
