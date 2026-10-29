# Design

## Source of truth
Status: Active. Updated: 2026-10-29. Surface: the Windows desktop main window and hotkey dialog. Evidence: `Forms/Form1.cs`, `Forms/HotkeySettingsForm.cs`, `Services/Localization.cs`, README, and existing GUI render probes under `.omc/artifacts/usability-probe/`. User direction: improve the existing interface, keep it simple, and move hotkeys into a separate Settings menu.

## Brand
A practical, calm desktop utility. Familiar Windows controls and clear feedback build trust. Avoid decorative dashboards, excessive icons, gradients, and extra navigation.

## Product goals
Make the first successful click task obvious: add a region, adjust timing/order, start. Keep region conditions discoverable and preserve all existing execution behavior. Success means users can identify the next action without reading the tutorial.

## Personas and jobs
People automating repetitive desktop clicks, from a single target to several conditional targets. They need to select screen regions, inspect the configuration, and stop reliably.

## Information architecture
Top menus: File for configuration import/export, Settings for hotkeys, Language, Help, About. Main content follows region management, click parameters, optional display controls, and start/stop. Context menus and double-click editing remain available alongside visible condition editing.

## Design principles
Use grouping and spacing to explain the workflow. Give the start/stop action the strongest emphasis. Keep optional settings visually secondary. Prefer explicit text to unexplained symbols. Preserve established workflows and configuration compatibility.

## Visual language
Light neutral window background, white content groups, dark readable headings, muted secondary text, and one blue accent for the main action. Reserve red for stopping/errors. Use the Windows UI font, consistent spacing, restrained borders, and no animation. Overlay colors retain their existing meanings.

## Components
Reuse native buttons, menus, numeric inputs, checkboxes, combo boxes, and the toggle-selection list. Main-window styling belongs in the main form or its UI helper. Section headings, empty-list guidance, selection actions, and the primary action must share consistent spacing and typography.

## Accessibility
Keep keyboard navigation, visible focus, readable contrast, accessible names, and existing global stop behavior. Never convey state by color alone. Disabled actions must reflect unavailable operations. Respect DPI scaling and avoid clipped translated text.

## Responsive behavior
This remains a compact desktop utility. Size content from actual font/text measurements and scale spacing with DPI. Long translations and jitter warnings must expand or wrap cleanly. Avoid introducing a navigation sidebar or demanding a large monitor.

## Interaction states
Empty region list explains how to add a region. Selection enables only applicable edit/remove/reorder actions. Running clearly changes Start to Stop and locks configuration mutations as before. Update checking remains unobtrusive. Hotkey failures retain existing explanatory feedback.

## Content voice
Short, concrete action labels in all ten supported languages. Use consistent terms for click region, detection region, template, and condition. Put detailed explanations in Help rather than repeating them on the main screen.

## Implementation constraints
.NET 10 Windows Forms; no additional UI framework or visual asset dependency. Preserve localization, configuration schema, overlay behavior, session cancellation, modal hotkey guards, and update handling. Build and run the existing tests; render representative languages and inspect the resulting main-window images before completion.

## Open questions
None blocking. Scope assumes the existing light desktop appearance; additional themes and a redesign of the condition editor are outside this iteration.
