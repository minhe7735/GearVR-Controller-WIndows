# Changelog

## Unreleased

- Add configurable idle disconnect and pause automatic reconnect until the user connects again.
- Show connection status and connect/disconnect actions in the notification-area menu.

- Rework BLE initialization and cleanup; add bounded connection attempts and optional automatic reconnection.
- Detect known and nearby Gear VR Controllers and allow selection when more than one is found.
- Add a gyroscope motion pointer with sensitivity, smoothing, recentering and virtual-desktop support.
- Correct sensor byte order and keep simultaneous button mappings independent.
- Redesign the interface with controller diagrams, startup options and notification-area controls.
- Document setup, limitations and the use of OpenAI Codex for this change set.

Validation: Windows build succeeded; controller discovery was checked against a controller visible to Windows; pointer coordinate mapping was checked using simulated multi-monitor layouts. Physical reconnection, aiming, screen transitions and all buttons require further testing on target hardware.
