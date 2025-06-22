# GenerateInterface

A C# source generator that automatically creates interfaces from classes annotated with `[GenerateInterface]`.

## Overview

Tired of manually creating interfaces for dependency injection? This source generator automatically generates interfaces for your classes, eliminating boilerplate code and ensuring your interfaces stay in sync with your implementations.

## Features

- 🚀 **Automatic Interface Generation** - Simply annotate your class with `[GenerateInterface]`
- 🔄 **Always In Sync** - Interfaces are generated at compile time, so they're always up to date
- 🎯 **Zero Runtime Dependencies** - The generator runs at compile time only
- 📦 **Easy Integration** - Just install the NuGet package and start using
- 🛡️ **Type Safe** - Full support for generics, inheritance, and complex types

## Installation

```bash
dotnet add package GenerateInterface
```

## Usage

### Basic Example

```csharp
[GenerateInterface]
public class UserService
{
    public Task<User> GetUserAsync(int id) => Task.FromResult(new User());
    public void DeleteUser(int id) { }
    private void InternalMethod() { } // Won't be included in interface
}
```

This automatically generates:

```csharp
public interface IUserService
{
    Task<User> GetUserAsync(int id);
    void DeleteUser(int id);
}
```

### Dependency Injection

```csharp
// Register in your DI container
services.AddScoped<IUserService, UserService>();

// Inject the interface
public class UserController
{
    private readonly IUserService _userService;
    
    public UserController(IUserService userService)
    {
        _userService = userService;
    }
}
```

## How It Works

1. Add the `[GenerateInterface]` attribute to any class
2. The source generator scans for annotated classes during compilation
3. Interfaces are automatically generated with all public members
4. Use the generated interfaces in your dependency injection setup

## Requirements

- .NET 5.0 or later
- C# 9.0 or later

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.