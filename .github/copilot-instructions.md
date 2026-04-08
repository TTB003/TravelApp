# Copilot Instructions

## Architecture Principles
- Always follow Clean Architecture.
- All business logic must be implemented in Services, not in ViewModels.
- Services must be interface-based, testable, and registered via Dependency Injection.
- Prefer event-driven communication between services and ViewModels.
- When restructuring this repo, preserve all existing logic, only move/reorganize into proper monorepo positions, and add backend as new components without impacting MAUI frontend behavior.

## ViewModel Rules
- ViewModels must NOT contain business logic.
- ViewModels should only:
  - Bind data to UI
  - Expose commands
  - Subscribe to service events

## Service Responsibilities
- GPS logic → LocationPollingService
- Geofencing logic → PoiGeofenceService
- Audio handling → AudioService (with fallback strategy)
- API communication → PoiApiService
- Local storage → LocalDatabaseService

## Offline-First Requirement
- The app must work without internet connection.
- Always load from local cache first (SQLite, file system).
- Sync with API in background when online.
- Audio files should be cached locally after first load.

## Event-Driven Design
- Services should emit events (e.g., OnLocationUpdated, OnPoiEntered).
- ViewModels and other services subscribe to events instead of calling logic directly.

## Anti-Patterns (MUST AVOID)
- Do NOT call Geolocation API inside ViewModel.
- Do NOT call HttpClient directly inside ViewModel.
- Do NOT implement geofencing logic in ViewModel.
- Do NOT play audio inside ViewModel.
- Do NOT access SQLite directly from ViewModel.

## Performance & Stability
- Avoid unnecessary API calls (use debounce and distance threshold).
- Prevent duplicate event subscriptions.
- Ensure proper unsubscribe to avoid memory leaks.

## Goal
- Code must be clean, modular, testable, and easy to extend for:
  - GPS tracking
  - Geofencing
  - Auto audio guide

## MAUI UI Guidelines
- Provide UI-focused responses in Vietnamese with a brief design concept first.
- Use modern Material 3 + Fluent styling for UI components.
- Ensure responsive and accessibility considerations in all designs.
- Maintain a friendly tone with suitable emojis.
- When requested, provide complete copy-pasteable XAML/C# code.

## Debugging Guidelines
- When explaining fixes, always include the root cause of the issue clearly along with the solution.