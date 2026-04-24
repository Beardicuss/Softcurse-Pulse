# Contributing to Pulse

Welcome, and thank you for taking the time to contribute to the Pulse System Monitor! Pulse thrives on community-driven plugins, strict performance heuristics, and robust analytics tools.

Please take a moment to review our [Code of Conduct](./CODE_OF_CONDUCT.md) before participating in our spaces.

## Ways to Contribute

You do not need to be a C# expert to contribute! We welcome support in many forms:
- Reporting bugs or identifying edge-cases in our heuristics.
- Proposing new metrics to track (Feature Requests).
- Designing new .ico assets or UI components for the Dashboard.
- Writing code for `Pulse.Plugins` completely isolated from the main Engine.
- Improving our documentation and community health files.

## Reporting Bugs

If you find a bug, please use the provided [Bug Report Template](./ISSUE_TEMPLATE/bug_report.md). Be sure to include:
- A clear, concise title.
- Exact steps to reproduce the issue.
- Your Windows Version and .NET Runtime Version.
- Screenshots, especially if the Dashboard UI glitches.

## Suggesting Features

We love new ideas, particularly around Plugin modules. If you have an idea:
- Use the [Feature Request Template](./ISSUE_TEMPLATE/feature_request.md).
- Clearly define the problem you are solving (e.g. "I cannot track my GPU temperatures").
- Propose exactly how it could hook into the `ActionEngine.cs` interface natively.

## Development Setup

To test and develop Pulse locally:

1. **Fork & Clone**
   Fork the repository to your own GitHub account and clone it to your local machine:
   ```bash
   git clone https://github.com/YOUR_USERNAME/Pulse.git
   cd Pulse
   ```

2. **Install Dependencies & Build**
   Ensure you have the `.NET 8.0 SDK` installed. No external packages are strictly required outside of those natively resolved by Nuget during the build.
   ```bash
   dotnet build
   ```

3. **Running Local Instances**
   ```bash
   dotnet run --project Pulse.App
   ```

## Making Changes

### Branching Convention
Create a cleanly scoped branch before making any changes:
- `feat/discord-embeds`
- `fix/sqlite-locking`
- `docs/readme-update`
- `plugin/gpu-temp-tracker`

### Commit Message Convention
Please adhere to **Conventional Commits**:
- `feat:` for new features.
- `fix:` for bug fixes.
- `docs:` for documentation updates.
- `chore:` for general repository maintenance (e.g., `chore: cleanup pass`).
- `refactor:` for code modifications lacking feature alterations or bug patches.

Make sure to test your code locally and verify your changes do not introduce warnings to the C# compiler.

## Submitting a Pull Request

When your branch is pushed up, open a PR against `main`. 
- Ensure you have thoroughly completed the [Pull Request Template](./PULL_REQUEST_TEMPLATE.md).
- One of the core maintainers will review the PR within roughly 48 hours.
- Be prepared to discuss architectural choices, especially regarding the `Pulse.Core` module, as we enforce extremely strict performance limitations there to ensure Pulse remains a lightweight background agent.

## Getting Help
If you need any guidance building a Plugin or tracking down a namespace, feel free to open a Discussion on GitHub!
