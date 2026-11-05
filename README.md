# Milkilla Drop VJ

Herramienta VJ open source compatible con presets MilkDrop (.milk), orientada a decks estilo Traktor, con MIDI, reactividad al audio, Spout y Auto DJ.

## Características previstas (Fase 1+)

- Carga de presets .milk
- 2 Decks estilo DJ (A/B) + Crossfader
- Reactividad al sonido (bass/mid/treb + FFT)
- Control MIDI (Traktor Jam u otros)
- Salida Spout para proyección
- Auto DJ por volumen / BPM
- Soporte futuro de PNG/sprites dentro de presets
- Compatible con motor MilkDrop / BeatDrop

## Cómo abrir el proyecto

1. Instala [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
2. Instala Visual Studio 2022 (Community) con carga de trabajo ".NET desktop development"
3. Abre `MilkillaDropVJ.csproj`
4. Restaura paquetes NuGet y compila (F5)

## Arquitectura

UI (WPF) ←→ ViewModel ←→ Services
                      ├── PresetService
                      ├── AudioService (NAudio + FFT)
                      ├── MidiService
                      └── MilkEngineService (controla motor externo)

El motor de render (MilkDrop/BeatDrop) se mantiene separado para máximo rendimiento y compatibilidad con .milk.
