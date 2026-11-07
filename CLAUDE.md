# CLAUDE.md — Milkilla Drop VJ

@AGENTS.md

Lo técnico de Milkilla Drop VJ está en `AGENTS.md` (importado arriba; si aún no está en tu rama, mandan el README y las decisiones del dueño: motor BeatDrop externo + Spout y primer hito «BeatDrop lanzado y Spout visible», antes de MVVM o de `PresetService`). Lo común a todos los agentes está en el ACUERDO: tablero crosswalk `guerrillaagents`, archivo `files/ACUERDO.md`, en la versión que el dueño haya aprobado (la nota fijada del tablero dice cuál; un borrador no está en vigor). Si este archivo y `AGENTS.md` discrepan en lo técnico, manda `AGENTS.md`. Una decisión posterior del dueño manda sobre todo. Este archivo solo añade lo que Claude (Claude Code o claude.ai) necesita para trabajar aquí sin que se lo repitan en cada sesión.

## Antes de empezar

- `git fetch` y mira el tablero `guerrillaagents` (sección `milkilla`). Si otro agente tiene una tarea abierta en la misma zona, no la toques.
- Una tarea, un objetivo, una rama.

## Tu papel

- En cada tarea, o implementas o revisas, y la tarea dice cuál. Nunca revisas tu propio cambio, aunque lo hicieras desde otra interfaz (Claude Code o claude.ai).
- Un cambio de código tiene un único revisor, el bot del proyecto, más el ok del dueño. Las rondas entre todos los bots quedan para decisiones transversales.
- El dueño técnico es el bot Milkilla Drop VJ, que implementa la fase 1. Motor decidido por el dueño: BeatDrop externo + Spout. No se reabre.

## Ramas y commits

- Una rama por fase o hito: `claude/<hito>`. Nunca push a `main`, nunca `force-push`, no borres ramas ni etiquetas.
- Solo el dueño decide qué se fusiona a `main`: lo fusiona él o, con su ok, el bot del proyecto. Claude nunca. Prohibir el push no es lo mismo que esta regla.
- Autor, antes del primer commit:

  ```
  git config user.name "GUERRILLA"
  git config user.email "115675632+Guerrillabitcoin@users.noreply.github.com"
  ```

- No subas `.github/workflows`: déjalo en `docs/ci/` y avisa al dueño.
- Nunca secretos ni claves. Sin nombre real ni email personal. Todo en español.

## Pruebas

- `dotnet build` cuando haya código (necesita Windows o el SDK de .NET 8 con el paquete de escritorio). Si no puedes compilar, dilo.
- BeatDrop, Spout y el MIDI de verdad los prueba el dueño en su PC: déjale los pasos exactos.

## Al terminar

Como dice `AGENTS.md` («Con otros agentes»), el trabajo diario del bot en su rama de hito sigue como hasta ahora; el ciclo completo es para trabajo de varios agentes, de riesgo o que toque `AGENTS.md`, los workflows o los secretos, y el trabajo de Claude siempre entra ahí.

Traspaso en el tablero con el formato del ACUERDO (§3): Qué, Rama @ commit, Probado, Archivos, Riesgos / pendiente, Prueba manual para el dueño, Revisa.

`.claude/settings.json` es una red parcial: frena las formas habituales de push a `main`, force-push, borrar ramas o etiquetas, editar `.github/workflows` y leer claves, pero no cualquier forma de escribir un comando (por ejemplo `git -C . push` o un script propio). No la pongas a prueba con acciones destructivas. La regla de verdad es la de arriba: solo el dueño fusiona.
