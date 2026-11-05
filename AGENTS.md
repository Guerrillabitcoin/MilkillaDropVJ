# AGENTS.md — Milkilla Drop VJ

Léelo antes de cambiar código. El acuerdo común de todos los agentes del dueño está en el tablero de crosswalk `guerrillaagents`, archivo `files/ACUERDO.md`. En lo técnico de este proyecto manda este archivo, y una decisión del dueño posterior manda sobre los dos.

## Producto

Herramienta VJ de código abierto para presets MilkDrop (`.milk`): dos decks A/B con crossfader, reactividad al audio (graves, medios, agudos y FFT), control MIDI (Traktor Jam u otros), salida Spout para proyectar y Auto DJ. Todo en español.

## Decisiones del dueño

- Motor: **BeatDrop como proceso externo + Spout**. projectM embebido se propuso y se descartó; no se reabre sin el dueño.
- Primer hito: **BeatDrop lanzado y Spout visible**, antes de la arquitectura MVVM o de `PresetService`.
- La fase 1 la implementa el bot Milkilla Drop VJ, salvo que el dueño diga otra cosa. Otros agentes revisan; no escriben código en paralelo en la misma zona.

## Estado (5 nov 2026)

Solo `README.md` y `MilkillaDropVJ.csproj` (WPF, `net8.0-windows`, `NAudio` 2.2.1, `DryWetMidi` 7.2.0, `CommunityToolkit.Mvvm` 8.2.2). No hay código todavía, así que aún no compila: falta al menos `App.xaml` y una ventana.

## Ramas

- Una rama por fase o hito (por ejemplo `feature/hito1-beatdrop-spout`), no una rama permanente del bot.
- Nunca directo a `main`; a `main` solo con autorización del dueño.
- Siempre `fetch` antes de empezar; nunca `force-push`. Autor de los commits: `GUERRILLA <guerrilla@users.noreply.github.com>`.
- Sin nombre real ni email personal. Secretos y claves nunca en el repo.

## Comprobar

- `dotnet build` (cuando haya código; necesita Windows o el SDK de .NET 8 con el paquete de escritorio).
- La prueba con BeatDrop, Spout y el MIDI de verdad la hace el dueño en su PC.

## Con otros agentes

Trabajo de varios agentes, de riesgo o que toque este archivo, los workflows o los secretos: tarea en el tablero `guerrillaagents`, revisión por otro agente y autorización del dueño. El trabajo diario del bot en su rama de hito sigue como hasta ahora.
