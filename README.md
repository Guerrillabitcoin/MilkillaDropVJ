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

## Hito 1 (v0.1.0): BeatDrop lanzado y Spout visible

Qué hace esta versión:

- Elige el ejecutable de BeatDrop (se recuerda en `%AppData%\MilkillaDropVJ\settings.json`).
- Lanza y cierra BeatDrop como proceso externo; si ya estaba abierto, lo detecta.
- Lista los emisores Spout de la máquina y elige solo el de BeatDrop (o el que elijas).
- Muestra dentro de la ventana la imagen que BeatDrop envía por Spout, con tamaño, formato y fps.
- Botón «Alternar Spout»: pone BeatDrop delante y le manda Ctrl+Z, el atajo con el que BeatDrop activa o desactiva su salida Spout.

Aún no hay decks, crossfader, audio propio, MIDI ni Auto DJ.

### Cómo probarlo en Windows

1. Instala BeatDrop con salida Spout (versión 1.3 o posterior de [OfficialIncubo/BeatDrop-Music-Visualizer](https://github.com/OfficialIncubo/BeatDrop-Music-Visualizer/releases)) y ábrelo una vez a mano para comprobar que funciona.
2. En la carpeta del repo: `dotnet build` y después `dotnet run` (o abre `MilkillaDropVJ.csproj` en Visual Studio y pulsa F5).
3. Pulsa «Buscar…» y elige `BeatDrop.exe`.
4. Pulsa «▶ Lanzar BeatDrop» y pon música en el PC.
5. En el apartado Spout debe aparecer el emisor de BeatDrop y la imagen en el centro de la ventana. Si pone «No hay emisores Spout», pulsa «Alternar Spout en BeatDrop (Ctrl+Z)» una vez.
6. Pulsa «■ Cerrar BeatDrop»: la vista debe pasar a «Sin señal Spout».

Si algo falla, copia el texto del recuadro «Registro» y pásalo al tablón.

UI (WPF) ←→ ViewModel ←→ Services
                      ├── PresetService
                      ├── AudioService (NAudio + FFT)
                      ├── MidiService
                      └── MilkEngineService (controla motor externo)

El motor de render (MilkDrop/BeatDrop) se mantiene separado para máximo rendimiento y compatibilidad con .milk.
