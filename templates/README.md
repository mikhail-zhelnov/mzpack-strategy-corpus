# templates/ — scaffolds for new strategies

Each template is a self-contained standalone project (host + nested algo + signal + csproj);
references/build/deploy are inherited from `..\Directory.Build.props`. Copy the one you need into
your own solution folder, then rename the project/namespace/classes.

| Template               | What it demonstrates | Reference source |
|----------------------|-------------------|-----------------|
| `StrategyTemplate`   | Basic scaffold: Entry[] + Pattern + signals tree + Strategy.Initialize | — |
| `ControlPanelTemplate` | Custom MZpack Control Panel: buttons (Trading on/off, Close, Break Even), handler attachment, updates from the UI thread, `[ControlPanel]` properties | AdvancedTemplate.cs |
| `DashboardTemplate`  | MZpack Pattern Dashboard: enabling via flags (ShowPatternsDashboard, legend, position), named signals for the legend, control from the panel | FootprintAction.cs |

## Control Panel — key points (ControlPanelTemplate)
- `ControlPanelShow / ControlPanelPropertiesShow / ControlPanelWidth` — configured in `SetDefaults`.
- `override UIElement[] CreateControlPanelElements()` — return an array of buttons/controls.
- `override ControlPanel_AttachEventHandlers() / DetachEventHandlers()` — attach/detach Click.
- Handlers: `Strategy.IsOpeningPositionEnabled` (enable/disable trading),
  `Strategy.Positions.CancelClose(...)`, `Strategy.Positions.BreakEven(...)`.
- Updating buttons from the UI thread: `ChartControl.Dispatcher.InvokeAsync(...)` (the `UpdateButtons` method,
  called from `OnPositionUpdate` of the algo class).
- `[ControlPanel]` on an overridden inherited property surfaces it in the panel.

## Dashboard — key points (DashboardTemplate)
- Built-in pattern dashboard; custom rendering is NOT needed — the grid is built from the signals tree.
- Enable in `SetDefaults`: `ShowPatternsDashboard=true`, `DashboardGridShowLegend=true`,
  `DashboardGridViewPosition=DashboardViewPosition.Top`, `DashboardGridViewOffset=-30`.
- For the legend, set a `Name` on EVERY signal (in `CreateEntryPattern`).
- Dashboard toggles are surfaced on the Control Panel via `[ControlPanel]`.
